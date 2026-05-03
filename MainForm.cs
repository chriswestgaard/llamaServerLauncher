using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace LlamaServerLauncher
{
    public partial class MainForm : Form
    {
        private readonly string _configPath = "config.json";
        private string _modelFolder;
        private Process _proc;

        // Hardware monitor
        private Timer _monitorTimer;
        private PerformanceCounter _cpuCounter;
        private PerformanceCounter _ramAvailCounter;
        private long _totalRamMb;
        private readonly object _gpuLock = new();
        private List<PerformanceCounter> _gpuEngineCounters = [];
        private List<PerformanceCounter> _gpuVramCounters = [];
        private long _gpuTotalVramBytes;
        private int _monitorTick;

        // Last-known metric snapshot (updated each monitor tick)
        private float _lastCpu = -1, _lastGpuPct = -1, _lastVramGb = -1;

        // Context usage — populated by parsing log output
        private volatile int _ctxMax, _ctxCurrent;

        // Server log file (written in real-time; rtbLog is populated on-demand when tab is shown)
        private readonly string _logFilePath = "server_log.txt";
        private StreamWriter _logWriter;
        private readonly object _logLock = new();
        private readonly object _metricsLock = new();
        private long _logViewRenderedBytes = -1;

        // Performance log
        private readonly string _perfLogPath = "performance_log.json";
        private static readonly JsonSerializerOptions _jsonOpts         = new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private static readonly JsonSerializerOptions _jsonOptsIndented = new() { WriteIndented = true, PropertyNamingPolicy = JsonNamingPolicy.CamelCase, PropertyNameCaseInsensitive = true };
        private DateTime _sessionStart;
        private string _sessionModel;
        private string _sessionArgs;
        private readonly List<float> _metricsCpu = new();
        private readonly List<float> _metricsRamGb = new();
        private readonly List<float> _metricsGpuPct = new();
        private readonly List<float> _metricsVramGb = new();
        private readonly List<float> _metricsGenTokPerSec = new();
        private readonly List<float> _metricsPrefillTokPerSec = new();
        private float _pendingPrefillTps = -1f;
        private readonly string _perfRequestsPath = "performance_requests.json";

        public MainForm()
        {
            InitializeComponent();
            FormClosed += (_, _) => { lock (_logLock) { _logWriter?.Dispose(); _logWriter = null; } };
            LoadConfig();
            if (string.IsNullOrEmpty(txtExePath.Text))
                txtExePath.Text = FindExe("llama-server.exe");
            _ = LoadHardwareInfoAsync();
            _ = InitMonitorAsync();
            if (!string.IsNullOrEmpty(_modelFolder))
            {
                LoadModels();
            }
            else
            {
                using var dialog = new FolderBrowserDialog { Description = "Select folder containing .gguf model files" };
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    _modelFolder = dialog.SelectedPath;
                    SaveConfig();
                    LoadModels();
                }
            }
        }

        private void LoadModels()
        {
            if (string.IsNullOrEmpty(_modelFolder)) return;
            if (!Directory.Exists(_modelFolder))
            {
                MessageBox.Show($"Folder not found: {_modelFolder}");
                return;
            }
            var files = Directory.GetFiles(_modelFolder, "*.gguf");
            cbModel.Items.Clear();
            foreach (var f in files)
                cbModel.Items.Add(Path.GetFileNameWithoutExtension(f));
            if (cbModel.Items.Count > 0)
                cbModel.SelectedIndex = 0;
            UpdateCommandPreview();
        }

        internal void UpdateCommandPreview()
        {
            if (cbModel.SelectedItem == null)
            {
                txtCmdPreview.Text = "(select a model to see the command)";
                return;
            }
            string modelFile = cbModel.SelectedItem.ToString() + ".gguf";
            string args = BuildCommand(modelFile);
            txtCmdPreview.Text = $"{Path.GetFileName(txtExePath.Text)} {args}";

            bool isRunning = false;
            try { isRunning = _proc != null && !_proc.HasExited; } catch { }
            if (isRunning && !string.IsNullOrEmpty(_sessionArgs) && args != _sessionArgs)
            {
                lblStatus.Text      = "Running  ⚠  restart to apply changes";
                lblStatus.ForeColor = System.Drawing.Color.FromArgb(255, 0, 0);
            }
            else if (isRunning)
            {
                lblStatus.Text      = "Running…";
                lblStatus.ForeColor = System.Drawing.SystemColors.ControlText;
            }
        }

        private void btnLaunch_Click(object sender, EventArgs e)
        {
            bool isRunning = false;
            try { isRunning = _proc != null && !_proc.HasExited; } catch { }
            if (isRunning)
            {
                StopServer();
                return;
            }

            if (cbModel.SelectedItem == null)
            {
                MessageBox.Show("Choose a model first.");
                return;
            }

            string modelFile = cbModel.SelectedItem.ToString() + ".gguf";
            string args = BuildCommand(modelFile);
            RunCommand(args);
        }

        private string BuildCommand(string modelFile)
        {
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;

            string modelPath = Path.Combine(_modelFolder ?? "", modelFile);
            sb.Append($"-m \"{modelPath}\"");

            // Server — only emit when value differs from llama-server's built-in default
            if (txtHost.Text != "127.0.0.1")
                sb.Append($" --host {txtHost.Text}");
            if (nudPort.Value != 8080)
                sb.Append($" --port {nudPort.Value}");
            if (nudThreads.Value != -1)
                sb.Append($" --threads {nudThreads.Value}");
            if (nudBatchSize.Value != 2048)
                sb.Append($" -b {nudBatchSize.Value}");
            if (nudUBatchSize.Value != 512)
                sb.Append($" -ub {nudUBatchSize.Value}");
            if (nudCtxSize.Value != 0)
                sb.Append($" -c {nudCtxSize.Value}");
            if (nudGpuLayers.Value != -1)
                sb.Append($" -ngl {nudGpuLayers.Value}");
            if (nudParallel.Value > 1)
                sb.Append($" -np {nudParallel.Value}");
            if (chkFlashAttn.Checked)
                sb.Append(" --flash-attn on");
            if (chkContBatching.Checked)
                sb.Append(" -cb");

            // Cache
            if (cbCacheK.Text != "f16")
                sb.Append($" --cache-type-k {cbCacheK.Text}");
            if (cbCacheV.Text != "f16")
                sb.Append($" --cache-type-v {cbCacheV.Text}");
            if (!chkMmap.Checked)
                sb.Append(" --no-mmap");
            if (chkMlock.Checked)
                sb.Append(" --mlock");

            // Reasoning
            if (cbReasoning.Text != "auto")
                sb.Append($" --reasoning {cbReasoning.Text}");

            // Sampling
            if (nudTemperature.Value != 0.80M)
                sb.Append($" --temperature {nudTemperature.Value.ToString("F2", ci)}");
            if (nudTopK.Value != 40)
                sb.Append($" --top-k {nudTopK.Value}");
            if (nudTopP.Value != 0.95M)
                sb.Append($" --top-p {nudTopP.Value.ToString("F2", ci)}");
            if (nudMinP.Value != 0.05M)
                sb.Append($" --min-p {nudMinP.Value.ToString("F2", ci)}");
            if (nudRepeatPenalty.Value != 1.00M)
                sb.Append($" --repeat-penalty {nudRepeatPenalty.Value.ToString("F2", ci)}");
            if (nudSeed.Value != -1)
                sb.Append($" --seed {nudSeed.Value}");

            // Tools
            if (!string.IsNullOrWhiteSpace(txtTools.Text))
                sb.Append($" --tools {txtTools.Text}");

            // Advanced
            if (!string.IsNullOrWhiteSpace(txtApiKey.Text))
                sb.Append($" --api-key \"{txtApiKey.Text}\"");
            if (chkEmbedding.Checked)
                sb.Append(" --embedding");
            if (chkRerank.Checked)
                sb.Append(" --rerank");
            if (chkMetrics.Checked)
                sb.Append(" --metrics");

            string extra = txtExtraArgs.Text.Trim();
            if (!string.IsNullOrEmpty(extra))
                sb.Append($" {extra}");

            return sb.ToString();
        }

        private void RunCommand(string args)
        {
            var startInfo = new ProcessStartInfo
            {
                FileName = txtExePath.Text,
                Arguments = args,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            try
            {
                _proc = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                _proc.OutputDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data, isError: false); };
                _proc.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data, isError: true); };
                _proc.Exited += (_, _) =>
                {
                    int code = _proc.ExitCode;
                    this.BeginInvoke(new Action(() =>
                    {
                        AppendLog($"--- process exited (code {code}) ---", isError: false);
                        lblStatus.Text      = $"Stopped (exit code {code})";
                        lblStatus.ForeColor = System.Drawing.SystemColors.ControlText;
                        btnLaunch.Text = "Launch llama-server";
                        btnOpenChat.Enabled = false;
                        SavePerformanceSession(code);
                        UpdatePerformanceTips();
                    }));
                };

                _proc.Start();
                _proc.BeginOutputReadLine();
                _proc.BeginErrorReadLine();

                _sessionStart = DateTime.Now;
                _sessionModel = cbModel.SelectedItem?.ToString() ?? "";
                _sessionArgs = args;
                _metricsCpu.Clear(); _metricsRamGb.Clear(); _metricsGpuPct.Clear(); _metricsVramGb.Clear();
                _metricsGenTokPerSec.Clear(); _metricsPrefillTokPerSec.Clear();
                _pendingPrefillTps = -1f;

                lock (_logLock)
                {
                    _logWriter?.Dispose();
                    try { _logWriter = new StreamWriter(_logFilePath, append: false) { AutoFlush = true }; } catch { }
                }
                _logViewRenderedBytes = -1;
                _ctxMax = 0;
                _ctxCurrent = 0;

                AppendLog($"--- started: {txtExePath.Text} ---", isError: false);
                UpdatePerformanceTips();
                lblStatus.Text = "Running…";
                btnLaunch.Text = "Stop Server";
                btnOpenChat.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error launching: {ex.Message}");
            }
        }

        private void btnOpenChat_Click(object sender, EventArgs e)
        {
            var url = $"http://localhost:{nudPort.Value}/";
            Process.Start(new ProcessStartInfo(url) { UseShellExecute = true });
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog { Description = "Select model folder" };
            if (!string.IsNullOrEmpty(_modelFolder))
                dialog.SelectedPath = _modelFolder;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _modelFolder = dialog.SelectedPath;
                SaveConfig();
                LoadModels();
            }
        }

        private async void StopServer()
        {
            var proc = _proc;
            if (proc == null) return;

            btnLaunch.Enabled = false;
            btnOpenChat.Enabled = false;
            btnLaunch.Text = "Stopping…";
            AppendLog("--- sending stop signal ---", isError: false);

            try
            {
                // Kill the entire process tree so CUDA/Vulkan helper processes are
                // also terminated, giving the GPU driver a chance to reclaim VRAM.
                proc.Kill(entireProcessTree: true);
                // Wait for the OS to fully confirm the process has exited.
                await Task.Run(() => proc.WaitForExit(8000));
            }
            catch { }

            btnLaunch.Enabled = true;
        }

        private void btnBrowseExe_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "Executable files (*.exe)|*.exe|All files (*.*)|*.*",
                Title = "Select llama-server executable"
            };
            if (!string.IsNullOrEmpty(txtExePath.Text))
                try { dialog.InitialDirectory = Path.GetDirectoryName(txtExePath.Text); } catch { }
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                txtExePath.Text = dialog.FileName;
                SaveConfig();
            }
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
            _cpuCounter?.Dispose();
            _ramAvailCounter?.Dispose();
            lock (_gpuLock) { _gpuEngineCounters.ForEach(c => c.Dispose()); _gpuVramCounters.ForEach(c => c.Dispose()); }
            if (_proc != null)
                try { if (!_proc.HasExited) _proc.Kill(entireProcessTree: true); _proc.WaitForExit(5000); } catch { }
            base.OnFormClosing(e);
        }

        // ── Hardware monitor ─────────────────────────────────────────────

        private async Task InitMonitorAsync()
        {
            await Task.Run(() =>
            {
                try { _cpuCounter = new PerformanceCounter("Processor", "% Processor Time", "_Total"); _cpuCounter.NextValue(); } catch { }
                try { _ramAvailCounter = new PerformanceCounter("Memory", "Available MBytes"); } catch { }
                try
                {
                    using var cs = new ManagementObjectSearcher("SELECT TotalVisibleMemorySize FROM Win32_OperatingSystem");
                    foreach (ManagementBaseObject b in cs.Get())
                        using (var o = (ManagementObject)b)
                        { _totalRamMb = Convert.ToInt64(o["TotalVisibleMemorySize"]) / 1024; break; }
                }
                catch { }
                RefreshGpuCounters();
                InitGpuVramCounters();
            });

            _monitorTimer = new Timer { Interval = 1500 };
            _monitorTimer.Tick += MonitorTick;
            _monitorTimer.Start();
        }

        private void RefreshGpuCounters()
        {
            var fresh = new List<PerformanceCounter>();
            try
            {
                var cat = new PerformanceCounterCategory("GPU Engine");
                foreach (var inst in cat.GetInstanceNames())
                {
                    if (!inst.Contains("engtype_3D", StringComparison.OrdinalIgnoreCase)) continue;
                    try { var c = new PerformanceCounter("GPU Engine", "Utilization Percentage", inst, true); c.NextValue(); fresh.Add(c); } catch { }
                }
            }
            catch { }
            lock (_gpuLock) { _gpuEngineCounters.ForEach(c => c.Dispose()); _gpuEngineCounters = fresh; }
        }

        private void InitGpuVramCounters()
        {
            var fresh = new List<PerformanceCounter>();
            try
            {
                var cat = new PerformanceCounterCategory("GPU Adapter Memory");
                foreach (var inst in cat.GetInstanceNames())
                    try { var c = new PerformanceCounter("GPU Adapter Memory", "Dedicated Usage", inst, true); c.NextValue(); fresh.Add(c); } catch { }
            }
            catch { }
            lock (_gpuLock) { _gpuVramCounters.ForEach(c => c.Dispose()); _gpuVramCounters = fresh; }
        }

        private void MonitorTick(object sender, EventArgs e)
        {
            if (tabMain.SelectedTab == tabLog)
            {
                long currentBytes = File.Exists(_logFilePath) ? new FileInfo(_logFilePath).Length : 0;
                if (currentBytes != _logViewRenderedBytes)
                    RefreshLogView();
            }

            if (++_monitorTick % 20 == 0)
                Task.Run(RefreshGpuCounters);

            float sampleCpu = -1, sampleRamGb = -1, sampleGpuPct = -1, sampleVramGb = -1;

            // ── CPU ──────────────────────────────────────────────────────
            float cpu = -1;
            try { cpu = _cpuCounter?.NextValue() ?? -1; } catch { }
            if (cpu >= 0) sampleCpu = cpu;
            graphCpu.AddSample(
                cpu >= 0 ? cpu : 0,
                cpu >= 0 ? $"{cpu:F0}%" : "N/A");

            // ── RAM ───────────────────────────────────────────────────────
            try
            {
                float availMb = _ramAvailCounter?.NextValue() ?? 0;
                if (_totalRamMb > 0)
                {
                    float usedMb = _totalRamMb - availMb;
                    float pct = usedMb / _totalRamMb * 100f;
                    float totalGb = _totalRamMb / 1024f;
                    sampleRamGb = usedMb / 1024f;
                    graphRam.AddSample(pct, $"{usedMb / 1024f:F1} / {totalGb:F0} GB");
                }
                else graphRam.AddSample(0, "N/A");
            }
            catch { graphRam.AddSample(0, "N/A"); }

            // ── GPU ───────────────────────────────────────────────────────
            float gpuTotal = 0;
            lock (_gpuLock)
            {
                List<PerformanceCounter> bad = null;
                foreach (var c in _gpuEngineCounters)
                {
                    try { gpuTotal += c.NextValue(); }
                    catch { (bad ??= new()).Add(c); }
                }
                if (bad != null) { bad.ForEach(c => { _gpuEngineCounters.Remove(c); c.Dispose(); }); }
            }
            float gpuPct = Math.Min(gpuTotal, 100f);
            if (_gpuEngineCounters.Count > 0) sampleGpuPct = gpuPct;
            graphGpu.AddSample(gpuPct, _gpuEngineCounters.Count > 0 ? $"{gpuPct:F0}%" : "N/A");

            // ── VRAM ──────────────────────────────────────────────────────
            try
            {
                long usedBytes;
                lock (_gpuLock)
                {
                    List<PerformanceCounter> bad = null;
                    usedBytes = 0;
                    foreach (var c in _gpuVramCounters)
                    {
                        try { usedBytes += (long)c.NextValue(); }
                        catch { (bad ??= new()).Add(c); }
                    }
                    if (bad != null) { bad.ForEach(c => { _gpuVramCounters.Remove(c); c.Dispose(); }); }
                }

                if (_gpuTotalVramBytes > 0)
                {
                    float pct = (float)(usedBytes / (double)_gpuTotalVramBytes * 100.0);
                    double usedG = usedBytes / (1024.0 * 1024 * 1024);
                    double totG = _gpuTotalVramBytes / (1024.0 * 1024 * 1024);
                    sampleVramGb = (float)usedG;
                    graphVram.AddSample(pct, $"{usedG:F1} / {totG:F0} GB");
                }
                else graphVram.AddSample(0, usedBytes > 0 ? $"{usedBytes / (1024.0 * 1024 * 1024):F1} GB" : "N/A");
            }
            catch { graphVram.AddSample(0, "N/A"); }

            // ── Context ───────────────────────────────────────────────────
            int ctxMax = _ctxMax, ctxCur = _ctxCurrent;
            graphCtx.AddSample(
                ctxMax > 0 ? ctxCur * 100f / ctxMax : 0,
                ctxMax > 0 ? $"{ctxCur:N0} / {ctxMax:N0}" : "—");

            // ── Store last-known values and accumulate session samples ────
            if (sampleCpu >= 0) _lastCpu = sampleCpu;
            if (sampleGpuPct >= 0) _lastGpuPct = sampleGpuPct;
            if (sampleVramGb >= 0) _lastVramGb = sampleVramGb;

            bool isRunning = false;
            try { isRunning = _proc != null && !_proc.HasExited; } catch { }
            if (isRunning)
            {
                if (sampleCpu >= 0) _metricsCpu.Add(sampleCpu);
                if (sampleRamGb >= 0) _metricsRamGb.Add(sampleRamGb);
                if (sampleGpuPct >= 0) _metricsGpuPct.Add(sampleGpuPct);
                if (sampleVramGb >= 0) _metricsVramGb.Add(sampleVramGb);
            }

            if (_monitorTick % 3 == 0) UpdatePerformanceTips();
        }

        private static readonly Regex _rxCtxMax  = new(@"\bn_ctx\b\s*=\s*(\d+)",  RegexOptions.Compiled);
        private static readonly Regex _rxNPast   = new(@"\bn_past\b\s*=\s*(\d+)", RegexOptions.Compiled);

        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) return;
            var json = File.ReadAllText(_configPath);
            var data = JsonSerializer.Deserialize<Dictionary<string, string>>(json);
            if (data == null) return;
            if (data.TryGetValue("folder", out var folder) && !string.IsNullOrEmpty(folder))
                _modelFolder = folder;
            if (data.TryGetValue("exePath", out var exe) && !string.IsNullOrEmpty(exe))
                txtExePath.Text = exe;
        }

        private void SaveConfig()
        {
            var data = new Dictionary<string, string>
            {
                ["folder"] = _modelFolder ?? "",
                ["exePath"] = txtExePath.Text
            };
            File.WriteAllText(_configPath, JsonSerializer.Serialize(data));
        }

        private static readonly Regex _tokPerSecRx = new(@"([\d.]+)\s+tokens per second", RegexOptions.Compiled);

        private void AppendLog(string text, bool isError)
        {
            // Parse context size from startup line, e.g. "n_ctx = 4096"
            if (text.Contains("n_ctx") && !text.Contains("n_ctx_train"))
            {
                var m = _rxCtxMax.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int v) && v > 0)
                    _ctxMax = v;
            }

            // Parse current position from slot lines, e.g. "n_past = 512"
            if (text.Contains("n_past"))
            {
                var m = _rxNPast.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int v))
                    _ctxCurrent = v;
            }

            // Parse token metrics — may be called from background threads
            if (text.Contains("tokens per second") && text.Contains("eval time"))
            {
                var m = _tokPerSecRx.Match(text);
                if (m.Success && float.TryParse(m.Groups[1].Value, NumberStyles.Float, CultureInfo.InvariantCulture, out float tps))
                {
                    bool running = false;
                    try { running = _proc != null && !_proc.HasExited; } catch { }
                    if (running)
                    {
                        lock (_metricsLock)
                        {
                            if (text.Contains("prompt eval"))
                            {
                                _metricsPrefillTokPerSec.Add(tps);
                                _pendingPrefillTps = tps;
                            }
                            else
                            {
                                _metricsGenTokPerSec.Add(tps);
                                float prefill = _pendingPrefillTps;
                                _pendingPrefillTps = -1f;
                                this.BeginInvoke(new Action(() => SavePerfRequest(tps, prefill)));
                            }
                        }
                    }
                }
            }

            // Write to log file; rtbLog is refreshed on-demand when the Log tab is opened
            lock (_logLock)
            {
                try { _logWriter?.WriteLine((isError ? "[E] " : "") + text.Replace("\a", "")); } catch { }
            }
        }

        private void RefreshLogView()
        {
            rtbLog.Clear();
            if (!File.Exists(_logFilePath)) return;
            string[] lines;
            try
            {
                // FileShare.ReadWrite lets us read while _logWriter may still have the file open
                using var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                lines = sr.ReadToEnd().Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
            }
            catch { return; }

            rtbLog.SuspendLayout();
            foreach (var line in lines)
            {
                bool err = line.StartsWith("[E] ");
                rtbLog.SelectionColor = err ? System.Drawing.Color.Salmon : System.Drawing.Color.LightGray;
                rtbLog.AppendText((err ? line.Substring(4) : line) + Environment.NewLine);
            }
            rtbLog.ResumeLayout();
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
            _logViewRenderedBytes = new FileInfo(_logFilePath).Length;
        }

        private async Task LoadHardwareInfoAsync()
        {
            var (cpu, gpu) = await Task.Run(() =>
            {
                string cpuText, gpuText;
                try
                {
                    var cpus = new List<string>();
                    using var cs = new ManagementObjectSearcher("SELECT Name, NumberOfCores, NumberOfLogicalProcessors FROM Win32_Processor");
                    foreach (ManagementBaseObject b in cs.Get())
                        using (var o = (ManagementObject)b)
                        {
                            string name = o["Name"]?.ToString()?.Trim() ?? "Unknown";
                            string cores = o["NumberOfCores"]?.ToString() ?? "?";
                            string threads = o["NumberOfLogicalProcessors"]?.ToString() ?? "?";
                            cpus.Add($"{name}  ({cores}C / {threads}T)");
                        }
                    cpuText = cpus.Count > 0 ? string.Join("  |  ", cpus) : "Unknown";
                }
                catch { cpuText = "Unable to query"; }

                // AdapterRAM in WMI is uint32 — caps at 4 GB for large cards.
                // The registry stores the real 64-bit value in qwMemorySize.
                try
                {
                    var gpus = new List<string>();
                    using var displayClass = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(
                        @"SYSTEM\CurrentControlSet\Control\Class\{4d36e968-e325-11ce-bfc1-08002be10318}");
                    if (displayClass != null)
                    {
                        foreach (string sub in displayClass.GetSubKeyNames())
                        {
                            if (!int.TryParse(sub, out _)) continue;
                            using var k = displayClass.OpenSubKey(sub);
                            if (k == null) continue;

                            string name = ((k.GetValue("HardwareInformation.AdapterString")
                                         ?? k.GetValue("DriverDesc")) as string ?? "").Trim();
                            if (string.IsNullOrEmpty(name)) continue;
                            if (IsVirtualAdapter(name)) continue;

                            long vram = 0;
                            try { vram = Convert.ToInt64(k.GetValue("HardwareInformation.qwMemorySize")); } catch { }
                            if (vram <= 0)
                                try { vram = Convert.ToInt64(k.GetValue("HardwareInformation.MemorySize")); } catch { }

                            if (vram > _gpuTotalVramBytes) _gpuTotalVramBytes = vram;

                            gpus.Add(vram > 0
                                ? $"{name}  ({vram / (1024.0 * 1024 * 1024):F0} GB VRAM)"
                                : name);
                        }
                    }
                    gpuText = gpus.Count > 0 ? string.Join("  |  ", gpus) : "None detected";
                }
                catch { gpuText = "Unable to query"; }

                return (cpuText, gpuText);
            });

            txtCpuInfo.Text = cpu;
            txtGpuInfo.Text = gpu;
        }

        private static bool IsVirtualAdapter(string name)
        {
            // Filter software/remote display adapters that are not useful for LLM inference
            string[] virtualKeywords =
            [
                "Microsoft Basic Display",
                "Virtual Display",
                "Indirect Display",
                "Remote Display",
                "Parsec",
                "Citrix",
                "VMware SVGA",
                "VirtualBox",
                "Moonlight",
            ];
            return virtualKeywords.Any(kw => name.Contains(kw, StringComparison.OrdinalIgnoreCase));
        }

        private static string FindExe(string exeName)
        {
            var pathVar = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in pathVar.Split(Path.PathSeparator))
            {
                try
                {
                    var full = Path.Combine(dir.Trim(), exeName);
                    if (File.Exists(full)) return full;
                }
                catch { }
            }
            return exeName; // let the OS resolve it at launch time
        }

        private void UpdatePerformanceTips()
        {
            rtbTips.Clear();

            var green = System.Drawing.Color.FromArgb(19, 194, 56);
            var amber = System.Drawing.Color.FromArgb(255, 183, 0);
            var red = System.Drawing.Color.Salmon;
            var dim = System.Drawing.Color.DimGray;

            void Line(string text, System.Drawing.Color color)
            {
                rtbTips.SelectionStart = rtbTips.TextLength;
                rtbTips.SelectionLength = 0;
                rtbTips.SelectionColor = color;
                rtbTips.AppendText(text + "\n");
            }

            bool serverRunning = false;
            try { serverRunning = _proc != null && !_proc.HasExited; } catch { }

            bool hasGpu = _gpuEngineCounters.Count > 0 || _gpuTotalVramBytes > 0;
            double totalVramGb = _gpuTotalVramBytes / (1024.0 * 1024 * 1024);
            double vramPct = totalVramGb > 0 && _lastVramGb >= 0 ? _lastVramGb / totalVramGb * 100.0 : -1;

            // Rolling average of the last 5 completed-request tok/s values
            float recentTps = -1;
            if (_metricsGenTokPerSec.Count > 0)
            {
                int take = Math.Min(5, _metricsGenTokPerSec.Count);
                recentTps = (float)_metricsGenTokPerSec.Skip(_metricsGenTokPerSec.Count - take).Average();
            }

            if (!serverRunning)
            {
                // Static pre-flight advice based on current settings
                if (hasGpu && nudGpuLayers.Value == 0)
                    Line("▶ GPU layers is 0 — the model will run entirely on CPU. Set -ngl to 999 to fully use the GPU.", amber);
                else if (hasGpu && nudGpuLayers.Value == -1)
                    Line("▶ GPU layers is -1 (auto-detect). Try setting an explicit value if GPU usage seems low.", dim);

                if (nudCtxSize.Value >= 8192 && !chkFlashAttn.Checked)
                    Line("▶ Large context planned. Enable Flash Attention (-fa) to cut KV VRAM usage for long contexts.", amber);

                if (cbCacheK.Text == "f16" && cbCacheV.Text == "f16" && hasGpu)
                    Line("▶ KV cache is f16 (default). Switching to q8_0 halves KV VRAM with minimal quality impact.", dim);

                if (rtbTips.TextLength == 0)
                    Line("Start the server and send some requests to see live performance tips.", dim);
                return;
            }

            // ── Live tips ────────────────────────────────────────────────

            // 1. Generation speed
            if (recentTps >= 40)
                Line($"▶ Generation: {recentTps:F1} t/s — excellent.", green);
            else if (recentTps >= 20)
                Line($"▶ Generation: {recentTps:F1} t/s — good.", green);
            else if (recentTps >= 8)
                Line($"▶ Generation: {recentTps:F1} t/s — moderate. Consider increasing GPU layers (-ngl) if VRAM has headroom.", amber);
            else if (recentTps >= 0)
                Line($"▶ Generation: {recentTps:F1} t/s — slow. Most work is likely on CPU; increase -ngl to offload more layers to GPU.", red);
            else
                Line("▶ No requests completed yet — send a prompt to measure generation speed.", dim);

            // 2. GPU vs CPU balance (only meaningful when we have actual tok/s evidence the model is working)
            if (hasGpu && _lastGpuPct >= 0 && _lastCpu >= 0 && recentTps >= 0)
            {
                if (_lastGpuPct < 15 && _lastCpu > 50)
                    Line($"▶ GPU nearly idle ({_lastGpuPct:F0}%) while CPU is busy ({_lastCpu:F0}%). Increase -ngl to move layers to GPU.", red);
                else if (_lastGpuPct < 40 && _lastCpu > 40)
                    Line($"▶ GPU at {_lastGpuPct:F0}% — some layers are still on CPU ({_lastCpu:F0}%). Try a higher -ngl value.", amber);
                else if (_lastGpuPct > 65 && _lastCpu < 35)
                    Line($"▶ GPU is handling the load ({_lastGpuPct:F0}% GPU, {_lastCpu:F0}% CPU) — well balanced.", green);
            }

            // 3. VRAM pressure
            if (vramPct > 95)
                Line($"▶ VRAM critically full ({vramPct:F0}%). Reduce context size (-c) or switch KV cache to q8_0/q4_0 to avoid OOM.", red);
            else if (vramPct > 85)
            {
                if (cbCacheK.Text == "f16" || cbCacheV.Text == "f16")
                    Line($"▶ VRAM at {vramPct:F0}%. Switch KV cache from f16 → q8_0 to recover ~50% of KV memory with minimal quality loss.", amber);
                else
                    Line($"▶ VRAM at {vramPct:F0}% — reduce context size (-c) if you hit out-of-memory errors.", amber);
            }
            else if (vramPct >= 0 && vramPct < 60 && nudGpuLayers.Value >= 0 && nudGpuLayers.Value < 999)
                Line($"▶ VRAM has headroom ({vramPct:F0}% used). You could increase GPU layers (-ngl) or context size (-c).", green);

            // 4. Flash attention for large contexts
            if (nudCtxSize.Value >= 8192 && !chkFlashAttn.Checked)
                Line($"▶ Context is {nudCtxSize.Value:N0} tokens — enable Flash Attention (-fa) to reduce VRAM usage and improve long-context speed.", amber);
        }

        private void SavePerformanceSession(int exitCode)
        {
            if (_metricsCpu.Count == 0) return;

            var end = DateTime.Now;
            int duration = (int)(end - _sessionStart).TotalSeconds;

            static PerfMetricStats Stat(List<float> lst) => lst.Count == 0
                ? new(0, 0, 0)
                : new(Math.Round(lst.Average(), 1), Math.Round((double)lst.Min(), 1), Math.Round((double)lst.Max(), 1));

            var session = new PerfSession(
                Start: _sessionStart.ToString("o"),
                End: end.ToString("o"),
                DurationSeconds: duration,
                Model: _sessionModel,
                Args: _sessionArgs,
                Samples: _metricsCpu.Count,
                Cpu: Stat(_metricsCpu),
                RamGb: Stat(_metricsRamGb),
                GpuPct: Stat(_metricsGpuPct),
                VramGb: Stat(_metricsVramGb),
                GenTokPerSec: _metricsGenTokPerSec.Count > 0 ? Stat(_metricsGenTokPerSec) : null,
                PrefillTokPerSec: _metricsPrefillTokPerSec.Count > 0 ? Stat(_metricsPrefillTokPerSec) : null);

            try
            {
                var opts = _jsonOptsIndented;
                var sessions = new List<PerfSession>();
                if (File.Exists(_perfLogPath))
                    try { sessions = JsonSerializer.Deserialize<List<PerfSession>>(File.ReadAllText(_perfLogPath), opts) ?? sessions; } catch { }
                sessions.Add(session);
                File.WriteAllText(_perfLogPath, JsonSerializer.Serialize(sessions, opts));

                var genSummary = session.GenTokPerSec != null ? $" | gen {session.GenTokPerSec.Avg:F1} t/s" : "";
                AppendLog($"--- perf: {duration}s | cpu avg {session.Cpu.Avg:F0}% | ram {session.RamGb.Avg:F1} GB | gpu {session.GpuPct.Avg:F0}% | vram {session.VramGb.Avg:F1} GB{genSummary} ---", isError: false);
                RefreshPerfLog();
            }
            catch (Exception ex) { AppendLog($"--- perf log write failed: {ex.Message} ---", isError: true); }
        }

        private void SavePerfRequest(float genTps, float prefillTps)
        {
            try
            {
                var opts = _jsonOpts;
                var req = new PerfRequest(DateTime.UtcNow.ToString("o"), _sessionModel, genTps, prefillTps, _sessionArgs, ParsePerfParams(_sessionArgs));
                var list = new List<PerfRequest>();
                if (File.Exists(_perfRequestsPath))
                    try { list = JsonSerializer.Deserialize<List<PerfRequest>>(File.ReadAllText(_perfRequestsPath), opts) ?? list; } catch { }
                list.Add(req);
                File.WriteAllText(_perfRequestsPath, JsonSerializer.Serialize(list, opts));
            }
            catch { }
            RefreshPerfLog();
        }

        private void btnClearPerf_Click(object sender, EventArgs e)
        {
            try { if (File.Exists(_perfLogPath)) File.Delete(_perfLogPath); } catch { }
            try { if (File.Exists(_perfRequestsPath)) File.Delete(_perfRequestsPath); } catch { }
            RefreshPerfLog();
        }

        private void RefreshPerfLog()
        {
            static string FmtTps(double v) => v >= 0 ? $"{v:F1}" : "—";

            var opts = _jsonOpts;

            var requests = new List<PerfRequest>();
            if (File.Exists(_perfRequestsPath))
                try { requests = JsonSerializer.Deserialize<List<PerfRequest>>(File.ReadAllText(_perfRequestsPath), opts) ?? requests; } catch { }

            var green    = System.Drawing.Color.FromArgb(19, 194, 56);
            var boldFont = new System.Drawing.Font(treePerf.Font, System.Drawing.FontStyle.Bold);

            treePerf.BeginUpdate();
            treePerf.Nodes.Clear();

            if (requests.Count == 0)
            {
                treePerf.Nodes.Add(new TreeNode("No requests recorded yet — run the server and send some prompts.")
                    { ForeColor = System.Drawing.Color.DimGray });
                treePerf.EndUpdate();
                return;
            }

            var resolved = requests.Select(r => (r, p: r.Params ?? ParsePerfParams(r.Args ?? ""), argsLabel: StripModelArg(r.Args ?? ""))).ToList();

            foreach (var modelGroup in resolved.GroupBy(x => x.r.Model).OrderBy(g => g.Key))
            {
                var modelNode = new TreeNode(modelGroup.Key) { ForeColor = green, NodeFont = boldFont };
                treePerf.Nodes.Add(modelNode);

                var byConfig = modelGroup
                    .GroupBy(x => x.argsLabel)
                    .Select(g =>
                    {
                        var genList     = g.Select(x => x.r.GenTps).Where(v => v >= 0).ToList();
                        var prefillList = g.Select(x => x.r.PrefillTps).Where(v => v >= 0).ToList();
                        double genAvg   = genList.Count > 0 ? genList.Average() : -1;
                        double preAvg   = prefillList.Count > 0 ? prefillList.Average() : -1;
                        var runs        = g.OrderByDescending(x => x.r.Timestamp).ToList();
                        return (ArgsLabel: g.Key, Params: g.First().p, GenAvg: genAvg, PrefillAvg: preAvg, Runs: runs);
                    })
                    .OrderByDescending(x => x.GenAvg)
                    .ToList();

                for (int i = 0; i < byConfig.Count; i++)
                {
                    var (ArgsLabel, Params, GenAvg, PrefillAvg, Runs) = byConfig[i];
                    bool isBest = i == 0 && byConfig.Count > 1;

                    string star      = isBest ? "★ " : "  ";
                    string tpsPart   = GenAvg >= 0 ? $"{GenAvg:F1} t/s avg" : "no data";
                    string countPart = Runs.Count == 1 ? "1 run" : $"{Runs.Count} runs";
                    string configText = $"{star}{tpsPart}  ({countPart})   {ArgsLabel}";

                    var configNode = new TreeNode(configText)
                    {
                        ForeColor = isBest ? green : System.Drawing.Color.LightGray,
                        NodeFont  = isBest ? boldFont : null,
                        Tag       = new PerfConfigItem { Model = modelGroup.Key, Params = Params, ArgsLabel = ArgsLabel, GenAvg = GenAvg }
                    };
                    modelNode.Nodes.Add(configNode);

                    foreach (var (r, _, _) in Runs)
                    {
                        var ts      = DateTime.Parse(r.Timestamp, null, DateTimeStyles.RoundtripKind).ToLocalTime();
                        string pre  = r.PrefillTps >= 0 ? $"   prefill {FmtTps(r.PrefillTps)} t/s" : "";
                        var runNode = new TreeNode($"{ts:yyyy-MM-dd HH:mm}   gen {FmtTps(r.GenTps)} t/s{pre}")
                        {
                            ForeColor = System.Drawing.Color.DimGray,
                            Tag       = new PerfConfigItem { Model = modelGroup.Key, Params = Params, ArgsLabel = ArgsLabel, GenAvg = GenAvg }
                        };
                        configNode.Nodes.Add(runNode);
                    }
                }

                modelNode.Expand();
            }

            treePerf.EndUpdate();
        }

#pragma warning disable CS8632
        private record PerfParams(
            int CtxSize, int GpuLayers, int Threads, int BatchSize, int UbatchSize, bool FlashAttn,
            string CacheTypeK = "f16", string CacheTypeV = "f16")
        {
            public string ToLabel()
            {
                var parts = new List<string>();
                if (CtxSize    > 0)      parts.Add($"ctx={CtxSize:N0}");
                if (GpuLayers  >= 0)     parts.Add($"ngl={GpuLayers}");
                if (Threads    >= 0)     parts.Add($"t={Threads}");
                if (BatchSize  > 0)      parts.Add($"batch={BatchSize}");
                if (UbatchSize > 0)      parts.Add($"ubatch={UbatchSize}");
                if (FlashAttn)           parts.Add("flash=yes");
                if (CacheTypeK != "f16") parts.Add($"ctk={CacheTypeK}");
                if (CacheTypeV != "f16") parts.Add($"ctv={CacheTypeV}");
                return parts.Count > 0 ? string.Join("  ", parts) : "(all defaults)";
            }

            public string ToKey() => $"{CtxSize}|{GpuLayers}|{Threads}|{BatchSize}|{UbatchSize}|{FlashAttn}|{CacheTypeK}|{CacheTypeV}";
        }

        private sealed class PerfConfigItem
        {
            public string Model      { get; init; }
            public PerfParams Params { get; init; }
            public string ArgsLabel  { get; init; }
            public double GenAvg     { get; init; }
            public override string ToString() =>
                $"{Model}  |  {ArgsLabel}" + (GenAvg >= 0 ? $"  ({GenAvg:F1} t/s)" : "");
        }

        private record PerfRequest(string Timestamp, string Model, float GenTps, float PrefillTps, string Args, PerfParams? Params = null);
        private record PerfMetricStats(double Avg, double Min, double Max);
        private record PerfSession(
            string Start, string End, int DurationSeconds,
            string Model, string Args, int Samples,
            PerfMetricStats Cpu, PerfMetricStats RamGb, PerfMetricStats GpuPct, PerfMetricStats VramGb,
            PerfMetricStats? GenTokPerSec = null, PerfMetricStats? PrefillTokPerSec = null);
#pragma warning restore CS8632

        private void LoadSelectedPerfConfig()
        {
            if (treePerf.SelectedNode?.Tag is not PerfConfigItem item) return;
            ApplyPerfConfig(item.Model, item.Params);
        }

        private void ApplyPerfConfig(string model, PerfParams p)
        {
            for (int i = 0; i < cbModel.Items.Count; i++)
                if (cbModel.Items[i]?.ToString() == model) { cbModel.SelectedIndex = i; break; }

            static decimal Clamp(decimal val, NumericUpDown n) =>
                Math.Max(n.Minimum, Math.Min(n.Maximum, val));

            nudCtxSize.Value     = Clamp(p.CtxSize,                                  nudCtxSize);
            nudGpuLayers.Value   = Clamp(p.GpuLayers,                                nudGpuLayers);
            nudThreads.Value     = Clamp(p.Threads,                                   nudThreads);
            nudBatchSize.Value   = Clamp(p.BatchSize  > 0 ? p.BatchSize  : 2048,     nudBatchSize);
            nudUBatchSize.Value  = Clamp(p.UbatchSize > 0 ? p.UbatchSize : 512,      nudUBatchSize);
            chkFlashAttn.Checked = p.FlashAttn;

            static void SelectCombo(ComboBox cb, string value)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                    if (cb.Items[i]?.ToString() == value) { cb.SelectedIndex = i; return; }
            }
            SelectCombo(cbCacheK, p.CacheTypeK);
            SelectCombo(cbCacheV, p.CacheTypeV);

            tabMain.SelectedTab = tabModel;
        }

        private static readonly Regex _rxCtx      = new(@"(?:-c|--ctx-size)\s+(\d+)",       RegexOptions.Compiled);
        private static readonly Regex _rxNgl      = new(@"(?:-ngl|--n-gpu-layers)\s+(\d+)", RegexOptions.Compiled);
        private static readonly Regex _rxT        = new(@"(?:-t|--threads)\s+(\d+)",        RegexOptions.Compiled);
        private static readonly Regex _rxBatch    = new(@"(?:-b|--batch-size)\s+(\d+)",     RegexOptions.Compiled);
        private static readonly Regex _rxUbatch   = new(@"(?:-ub|--ubatch-size)\s+(\d+)",   RegexOptions.Compiled);
        private static readonly Regex _rxCacheK   = new(@"--cache-type-k\s+(\S+)",          RegexOptions.Compiled);
        private static readonly Regex _rxCacheV   = new(@"--cache-type-v\s+(\S+)",          RegexOptions.Compiled);
        private static readonly Regex _rxModelArg = new(@"-m\s+""[^""]*""\s*",              RegexOptions.Compiled);

        private static string StripModelArg(string args)
        {
            var stripped = _rxModelArg.Replace(args ?? "", "").Trim();
            return stripped.Length > 0 ? stripped : "(all defaults)";
        }

        private static PerfParams ParsePerfParams(string args)
        {
            int    GetInt(Regex rx, int absent = 0) => rx.Match(args) is { Success: true } m
                ? int.TryParse(m.Groups[1].Value, out int v) ? v : absent : absent;
            string GetStr(Regex rx, string absent) => rx.Match(args) is { Success: true } m
                ? m.Groups[1].Value : absent;
            bool HasFlag(string s) => args.Contains(s);

            return new PerfParams(
                CtxSize:    GetInt(_rxCtx),
                GpuLayers:  GetInt(_rxNgl, absent: -1),
                Threads:    GetInt(_rxT,   absent: -1),
                BatchSize:  GetInt(_rxBatch),
                UbatchSize: GetInt(_rxUbatch),
                FlashAttn:  HasFlag("--flash-attn") || HasFlag("-fa"),
                CacheTypeK: GetStr(_rxCacheK, "f16"),
                CacheTypeV: GetStr(_rxCacheV, "f16"));
        }
    }
}
