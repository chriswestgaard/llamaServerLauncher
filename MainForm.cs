using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Text.Json;

namespace LlamaServerLauncher
{
    public partial class MainForm : Form
    {
        private readonly string _configPath = "config.json";
        private string _modelFolder;
        private string _savedModel = "";
        private string _mmprojPath = "";
        private int _modelLayerCount = 0;
        private int _modelDefaultCtx = 0;
        private Process _proc;

        // Hardware monitor
        private System.Windows.Forms.Timer _monitorTimer;
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
        private bool _monitorBusy;

        // Context usage — populated by /slots API polling (log parsing used as fallback on startup)
            private int _ctxMax, _ctxCurrent;
        private volatile bool _pollingSlotsInProgress;
        private volatile bool _serverReady;
        private volatile bool _fitAbortedNgl999;
        private volatile bool _fitSuccessNotified;
        private int _fitReducedCtxSize;
        private int _fitAdjustedNgl = -1;
        private static readonly System.Net.Http.HttpClient _http = new() { Timeout = TimeSpan.FromSeconds(3) };

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
            cbModel.SelectedIndexChanged += (_, _) => _ = UpdateModelMetaAsync();
            chkNglAuto.CheckedChanged += (_, _) =>
            {
                trkGpuLayers.Enabled = !chkNglAuto.Checked;
                UpdateNglDisplay();
            };
            trkGpuLayers.Scroll += (_, _) => UpdateNglDisplay();
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
            {
                int idx = 0;
                if (!string.IsNullOrEmpty(_savedModel))
                    for (int i = 0; i < cbModel.Items.Count; i++)
                        if (cbModel.Items[i]?.ToString() == _savedModel) { idx = i; break; }
                _savedModel = "";
                cbModel.SelectedIndex = idx;
            }
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
                btnLaunch.Text      = "Restart Server";
                btnLaunch.BackColor = System.Drawing.Color.FromArgb(204, 80, 0);
                btnLaunch.ForeColor = System.Drawing.Color.White;
            }
            else if (isRunning)
            {
                lblStatus.Text      = "Running…";
                lblStatus.ForeColor = System.Drawing.SystemColors.ControlText;
                btnLaunch.Text      = "Stop Server";
                btnLaunch.BackColor = System.Drawing.SystemColors.Control;
                btnLaunch.ForeColor = System.Drawing.SystemColors.ControlText;
            }
        }

        private async void btnLaunch_Click(object sender, EventArgs e)
        {
            bool isRunning = false;
            try { isRunning = _proc != null && !_proc.HasExited; } catch { }
            if (isRunning)
            {
                bool needsRestart = cbModel.SelectedItem != null &&
                                    !string.IsNullOrEmpty(_sessionArgs) &&
                                    BuildCommand(cbModel.SelectedItem.ToString() + ".gguf") != _sessionArgs;
                await StopServer();
                if (needsRestart)
                {
                    string restartModelFile = cbModel.SelectedItem.ToString() + ".gguf";
                    RunCommand(BuildCommand(restartModelFile));
                }
                return;
            }

            if (cbModel.SelectedItem == null)
            {
                MessageBox.Show("Choose a model first.");
                return;
            }

            if (!File.Exists(txtExePath.Text))
            {
                MessageBox.Show($"llama-server executable not found:\n{txtExePath.Text}\n\nUse the Advanced tab to set the correct path.", "Executable not found", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            string modelFile = cbModel.SelectedItem.ToString() + ".gguf";
            string args = BuildCommand(modelFile);
            RunCommand(args);
        }

        private string HostValue =>
            rdoHostLocal.Checked ? "127.0.0.1" :
            rdoHostAll.Checked   ? "0.0.0.0"   :
            txtHostCustom.Text.Trim();

        private int GpuLayersValue =>
            chkNglAuto.Checked                          ? -1  :
            trkGpuLayers.Value == 0                     ?  0  :
            trkGpuLayers.Value >= trkGpuLayers.Maximum  ? 999 :
            trkGpuLayers.Value;

        private void UpdateNglDisplay()
        {
            if (chkNglAuto.Checked)        lblNglDisplay.Text = "auto";
            else if (trkGpuLayers.Value == 0) lblNglDisplay.Text = "CPU only";
            else if (trkGpuLayers.Value >= trkGpuLayers.Maximum) lblNglDisplay.Text = "GPU only";
            else lblNglDisplay.Text = $"{trkGpuLayers.Value} layers";
        }

        private void SetNglSlider(int ngl)
        {
            // ngl: -1=auto, 0=CPU only, 999=GPU only, N=custom
            if (ngl < 0) { chkNglAuto.Checked = true; return; }
            chkNglAuto.Checked = false;
            trkGpuLayers.Value = ngl == 0   ? 0 :
                                  ngl >= 999 ? trkGpuLayers.Maximum :
                                  Math.Clamp(ngl, 0, trkGpuLayers.Maximum);
            UpdateNglDisplay();
        }

        private string BuildCommand(string modelFile)
        {
            var sb = new StringBuilder();
            var ci = CultureInfo.InvariantCulture;

            string modelPath = Path.Combine(_modelFolder ?? "", modelFile);
            sb.Append($"-m \"{modelPath}\"");
            if (chkMmproj.Checked && !string.IsNullOrEmpty(_mmprojPath))
                sb.Append($" --mmproj \"{_mmprojPath}\"");

            // Server — only emit when value differs from llama-server's built-in default
            string host = HostValue;
            if (host != "127.0.0.1")
                sb.Append($" --host {host}");
            if (nudPort.Value != 8080)
                sb.Append($" --port {nudPort.Value}");
            if (!chkThreadsAuto.Checked)
                sb.Append($" --threads {nudThreads.Value}");
            if (!chkThreadsBatchAuto.Checked)
                sb.Append($" -tb {nudThreadsBatch.Value}");
            if (nudBatchSize.Value != 2048)
                sb.Append($" -b {nudBatchSize.Value}");
            if (nudUBatchSize.Value != 512)
                sb.Append($" -ub {nudUBatchSize.Value}");
            if (!chkCtxDefault.Checked)
                sb.Append($" -c {nudCtxSize.Value}");
            int ngl = GpuLayersValue;
            if (ngl != -1)
                sb.Append($" -ngl {ngl}");
            if (nudParallel.Value > 1)
                sb.Append($" -np {nudParallel.Value}");
            if (chkFlashAttn.Checked)
                sb.Append(" --flash-attn on");
            if (chkContBatching.Checked)
                sb.Append(" -cb");
            if (!chkContextShift.Checked)
                sb.Append(" --no-context-shift");

            // Cache
            if (cbCacheK.Text != "f16")
                sb.Append($" --cache-type-k {cbCacheK.Text}");
            if (cbCacheV.Text != "f16")
                sb.Append($" --cache-type-v {cbCacheV.Text}");
            if (!chkMmap.Checked)
                sb.Append(" --no-mmap");
            if (chkMlock.Checked)
                sb.Append(" --mlock");
            if (!chkKvOffload.Checked)
                sb.Append(" --no-kv-offload");
            if (cbSplitMode.Text != "layer")
                sb.Append($" --split-mode {cbSplitMode.Text}");
            if (nudDefragThold.Value >= 0)
                sb.Append($" -dt {nudDefragThold.Value.ToString("F2", ci)}");

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
            if (!chkSeedRandom.Checked)
                sb.Append($" --seed {nudSeed.Value}");

            if (nudCacheReuse.Value > 0)
                sb.Append($" --cache-reuse {nudCacheReuse.Value}");

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
                var thisProc = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
                _proc = thisProc;
                thisProc.OutputDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data, isError: false); };
                thisProc.ErrorDataReceived += (_, e) => { if (e.Data != null) AppendLog(e.Data, isError: true); };
                thisProc.Exited += (_, _) =>
                {
                    int code = thisProc.ExitCode;
                    this.BeginInvoke(new Action(() =>
                    {
                        if (_proc != thisProc) return; // a restart started a new process — skip cleanup
                        AppendLog($"--- process exited (code {code}) ---", isError: false);
                        lblStatus.Text      = $"Stopped (exit code {code})";
                        lblStatus.ForeColor = System.Drawing.SystemColors.ControlText;
                        btnLaunch.Text      = "Launch llama-server";
                        btnLaunch.BackColor = System.Drawing.SystemColors.Control;
                        btnLaunch.ForeColor = System.Drawing.SystemColors.ControlText;
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
                _serverReady = false;
                _fitAbortedNgl999   = false;
                _fitSuccessNotified = false;
                _fitReducedCtxSize  = 0;
                _fitAdjustedNgl     = -1;

                AppendLog($"--- started: {txtExePath.Text} ---", isError: false);
                UpdatePerformanceTips();
                lblStatus.Text      = "Starting…";
                btnLaunch.Text      = "Stop Server";
                btnLaunch.BackColor = System.Drawing.SystemColors.Control;
                btnLaunch.ForeColor = System.Drawing.SystemColors.ControlText;
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

        private async Task StopServer()
        {
            var proc = _proc;
            if (proc == null) return;

            btnLaunch.Enabled   = false;
            btnOpenChat.Enabled = false;
            btnLaunch.Text      = "Stopping…";
            btnLaunch.BackColor = System.Drawing.SystemColors.Control;
            btnLaunch.ForeColor = System.Drawing.SystemColors.ControlText;
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

        private void btnBrowseMmproj_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Filter = "GGUF files (*.gguf)|*.gguf|All files (*.*)|*.*",
                Title = "Select multimodal projector file"
            };
            if (!string.IsNullOrEmpty(_mmprojPath))
                try { dialog.InitialDirectory = Path.GetDirectoryName(_mmprojPath); } catch { }
            else if (!string.IsNullOrEmpty(_modelFolder))
                dialog.InitialDirectory = _modelFolder;
            if (dialog.ShowDialog() == DialogResult.OK)
            {
                _mmprojPath = dialog.FileName;
                txtMmprojPath.Text = _mmprojPath;
                SaveConfig();
                UpdateCommandPreview();
            }
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
            SaveConfig();
            _monitorTimer?.Stop();
            _monitorTimer?.Dispose();
            _cpuCounter?.Dispose();
            _ramAvailCounter?.Dispose();
            lock (_gpuLock) { _gpuEngineCounters.ForEach(c => c.Dispose()); _gpuVramCounters.ForEach(c => c.Dispose()); }

            var proc = _proc;
            if (proc != null)
            {
                // Detach the Exited handler so the BeginInvoke callback doesn't fire
                // against a form that is already being torn down.
                proc.EnableRaisingEvents = false;
                try
                {
                    if (!proc.HasExited)
                    {
                        proc.Kill(entireProcessTree: true);
                        proc.WaitForExit(5000);
                    }
                }
                catch { }
            }

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

            _monitorTimer = new System.Windows.Forms.Timer { Interval = 1500 };
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
                    if (!inst.Contains("engtype_3D",       StringComparison.OrdinalIgnoreCase) &&
                        !inst.Contains("engtype_Compute", StringComparison.OrdinalIgnoreCase)) continue;
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

        private async void MonitorTick(object sender, EventArgs e)
        {
            if (_monitorBusy) return;
            _monitorBusy = true;
            try
            {
                // ── Log tab refresh — file read on background thread ──────
                if (tabMain.SelectedTab == tabLog)
                {
                    var lines = await Task.Run(() =>
                    {
                        long sz = File.Exists(_logFilePath) ? new FileInfo(_logFilePath).Length : 0;
                        return sz == _logViewRenderedBytes ? null : ReadLogLines();
                    });
                    if (lines != null) RenderLogLines(lines);
                }

                if (++_monitorTick % 20 == 0)
                    _ = Task.Run(RefreshGpuCounters);

                // ── All counter reads on background thread ────────────────
                var s = await Task.Run(SampleMetrics);

                // ── Graph updates on UI thread ────────────────────────────
                graphCpu.AddSample(s.CpuPct,  s.CpuLbl);
                graphRam.AddSample(s.RamPct,  s.RamLbl);
                graphGpu.AddSample(s.GpuPct,  s.GpuLbl);
                graphVram.AddSample(s.VramPct, s.VramLbl);

                // ── Context ───────────────────────────────────────────────
                bool serverRunning = false;
                try { serverRunning = _proc != null && !_proc.HasExited; } catch { }
                if (serverRunning && !_pollingSlotsInProgress)
                    _ = PollSlotsAsync((int)nudPort.Value);

                int ctxMax = _ctxMax, ctxCur = _ctxCurrent;
                graphCtx.AddSample(
                    ctxMax > 0 ? ctxCur * 100f / ctxMax : 0,
                    ctxMax > 0 ? $"{ctxCur:N0} / {ctxMax:N0}" : "—");

                // ── Store last-known values and accumulate session samples ─
                if (s.StoreCpu    >= 0) _lastCpu    = s.StoreCpu;
                if (s.StoreGpuPct >= 0) _lastGpuPct = s.StoreGpuPct;
                if (s.StoreVramGb >= 0) _lastVramGb = s.StoreVramGb;

                bool isRunning = false;
                try { isRunning = _proc != null && !_proc.HasExited; } catch { }
                if (isRunning)
                {
                    if (s.StoreCpu    >= 0) _metricsCpu.Add(s.StoreCpu);
                    if (s.StoreRamGb  >= 0) _metricsRamGb.Add(s.StoreRamGb);
                    if (s.StoreGpuPct >= 0) _metricsGpuPct.Add(s.StoreGpuPct);
                    if (s.StoreVramGb >= 0) _metricsVramGb.Add(s.StoreVramGb);
                }

                if (_monitorTick % 3 == 0) UpdatePerformanceTips();
            }
            finally { _monitorBusy = false; }
        }

        private readonly struct HwSample
        {
            public readonly float CpuPct, RamPct, GpuPct, VramPct;
            public readonly float StoreCpu, StoreRamGb, StoreGpuPct, StoreVramGb;
            public readonly string CpuLbl, RamLbl, GpuLbl, VramLbl;

            public HwSample(
                float cpuPct,  string cpuLbl,  float storeCpu,
                float ramPct,  string ramLbl,  float storeRamGb,
                float gpuPct,  string gpuLbl,  float storeGpuPct,
                float vramPct, string vramLbl, float storeVramGb)
            {
                CpuPct = cpuPct;   CpuLbl = cpuLbl;   StoreCpu    = storeCpu;
                RamPct = ramPct;   RamLbl = ramLbl;   StoreRamGb  = storeRamGb;
                GpuPct = gpuPct;   GpuLbl = gpuLbl;   StoreGpuPct = storeGpuPct;
                VramPct = vramPct; VramLbl = vramLbl; StoreVramGb = storeVramGb;
            }
        }

        private HwSample SampleMetrics()
        {
            // ── CPU ──────────────────────────────────────────────────────
            float cpu = -1;
            try { cpu = _cpuCounter?.NextValue() ?? -1; } catch { }
            float cpuPct  = cpu >= 0 ? cpu : 0;
            string cpuLbl = cpu >= 0 ? $"{cpu:F0}%" : "N/A";

            // ── RAM ───────────────────────────────────────────────────────
            float ramPct = 0, storeRamGb = -1;
            string ramLbl = "N/A";
            try
            {
                float availMb = _ramAvailCounter?.NextValue() ?? 0;
                if (_totalRamMb > 0)
                {
                    float usedMb  = _totalRamMb - availMb;
                    float totalGb = _totalRamMb / 1024f;
                    storeRamGb = usedMb / 1024f;
                    ramPct = usedMb / _totalRamMb * 100f;
                    ramLbl = $"{storeRamGb:F1} / {totalGb:F0} GB";
                }
            }
            catch { }

            // ── GPU ───────────────────────────────────────────────────────
            float gpuTotal = 0, storeGpuPct = -1;
            bool hasGpuCounters;
            lock (_gpuLock)
            {
                List<PerformanceCounter> bad = null;
                foreach (var c in _gpuEngineCounters)
                {
                    try { gpuTotal += c.NextValue(); }
                    catch { (bad ??= new()).Add(c); }
                }
                if (bad != null) { bad.ForEach(c => { _gpuEngineCounters.Remove(c); c.Dispose(); }); }
                hasGpuCounters = _gpuEngineCounters.Count > 0;
            }
            float gpuPct = Math.Min(gpuTotal, 100f);
            if (hasGpuCounters) storeGpuPct = gpuPct;
            string gpuLbl = hasGpuCounters ? $"{gpuPct:F0}%" : "N/A";

            // ── VRAM ──────────────────────────────────────────────────────
            float vramPct = 0, storeVramGb = -1;
            string vramLbl = "N/A";
            try
            {
                long usedBytes = 0;
                lock (_gpuLock)
                {
                    List<PerformanceCounter> bad = null;
                    foreach (var c in _gpuVramCounters)
                    {
                        try { usedBytes += (long)c.NextValue(); }
                        catch { (bad ??= new()).Add(c); }
                    }
                    if (bad != null) { bad.ForEach(c => { _gpuVramCounters.Remove(c); c.Dispose(); }); }
                }
                if (_gpuTotalVramBytes > 0)
                {
                    vramPct    = (float)(usedBytes / (double)_gpuTotalVramBytes * 100.0);
                    double usedG = usedBytes / (1024.0 * 1024 * 1024);
                    double totG  = _gpuTotalVramBytes / (1024.0 * 1024 * 1024);
                    storeVramGb = (float)usedG;
                    vramLbl = $"{usedG:F1} / {totG:F0} GB";
                }
                else if (usedBytes > 0)
                    vramLbl = $"{usedBytes / (1024.0 * 1024 * 1024):F1} GB";
            }
            catch { }

            return new HwSample(
                cpuPct,  cpuLbl,  cpu >= 0 ? cpu : -1f,
                ramPct,  ramLbl,  storeRamGb,
                gpuPct,  gpuLbl,  storeGpuPct,
                vramPct, vramLbl, storeVramGb);
        }

        private static readonly Regex _rxCtxMax          = new(@"\bn_ctx\b\s*=\s*(\d+)",                        RegexOptions.Compiled);
        private static readonly Regex _rxFitCtxReduced  = new(@"context size reduced from \d+ to (\d+)",        RegexOptions.Compiled);
        private static readonly Regex _rxFitNgl          = new(@"set ngl_per_device\[0\]\.n_layer=(\d+)",        RegexOptions.Compiled);
        private static readonly Regex _rxOffloadedLayers = new(@"offloaded\s+(\d+)/\d+\s+layers\s+to\s+GPU",    RegexOptions.Compiled);
        // Matches "n_tokens = N" but not "task.n_tokens" or "batch.n_tokens"
        private static readonly Regex _rxNTokens  = new(@"(?<![.\w])n_tokens\s*=\s*(\d+)", RegexOptions.Compiled);

        private async Task PollSlotsAsync(int port)
        {
            _pollingSlotsInProgress = true;
            try
            {
                var json = await _http.GetStringAsync($"http://localhost:{port}/slots");
                using var doc = JsonDocument.Parse(json);
                int nPast = 0, nCtx = 0;
                foreach (var slot in doc.RootElement.EnumerateArray())
                {
                    if (slot.TryGetProperty("n_past", out var p)) nPast = Math.Max(nPast, p.GetInt32());
                    if (slot.TryGetProperty("n_ctx",  out var c)) nCtx  = Math.Max(nCtx,  c.GetInt32());
                }
                if (nCtx  > 0) _ctxMax = nCtx;
                // Only advance — keeps last value when slots are idle between requests
                if (nPast > _ctxCurrent) _ctxCurrent = nPast;
            }
            catch { }
            finally { _pollingSlotsInProgress = false; }
        }

        private void LoadConfig()
        {
            if (!File.Exists(_configPath)) return;
            AppSettings s;
            try { s = JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(_configPath), _jsonOpts); }
            catch { return; }
            if (s == null) return;

            if (!string.IsNullOrEmpty(s.Folder))  _modelFolder    = s.Folder;
            if (!string.IsNullOrEmpty(s.ExePath)) txtExePath.Text = s.ExePath;
            _savedModel = s.Model ?? "";

            static decimal Clamp(decimal v, NumericUpDown n) => Math.Max(n.Minimum, Math.Min(n.Maximum, v));

            // NglMode: 0=auto, 1=CPU only, 2=GPU only, 3=custom
            int savedNgl = s.NglMode == 0 ? -1 :
                           s.NglMode == 1 ?  0 :
                           s.NglMode == 2 ? 999 :
                           (int)s.GpuLayers;
            SetNglSlider(savedNgl);
            UpdateNglDisplay();
            chkCtxDefault.Checked       = s.CtxDefault;
            nudCtxSize.Value            = Clamp(s.CtxSize, nudCtxSize);
            nudBatchSize.Value          = Clamp(s.BatchSize, nudBatchSize);
            nudUBatchSize.Value         = Clamp(s.UBatchSize, nudUBatchSize);
            if (s.CacheKIndex    >= 0 && s.CacheKIndex    < cbCacheK.Items.Count)    cbCacheK.SelectedIndex    = s.CacheKIndex;
            if (s.CacheVIndex    >= 0 && s.CacheVIndex    < cbCacheV.Items.Count)    cbCacheV.SelectedIndex    = s.CacheVIndex;
            if (s.ReasoningIndex >= 0 && s.ReasoningIndex < cbReasoning.Items.Count) cbReasoning.SelectedIndex = s.ReasoningIndex;
            chkThreadsAuto.Checked       = s.ThreadsAuto;
            nudThreads.Value             = Clamp(s.Threads, nudThreads);
            chkThreadsBatchAuto.Checked  = s.ThreadsBatchAuto;
            nudThreadsBatch.Value        = Clamp(s.ThreadsBatch, nudThreadsBatch);
            nudParallel.Value            = Clamp(s.Parallel, nudParallel);
            chkFlashAttn.Checked         = s.FlashAttn;
            chkContBatching.Checked      = s.ContBatching;
            chkContextShift.Checked      = s.ContextShift;
            chkMmap.Checked              = s.Mmap;
            chkMlock.Checked             = s.Mlock;
            chkKvOffload.Checked         = s.KvOffload;
            if (s.SplitModeIndex >= 0 && s.SplitModeIndex < cbSplitMode.Items.Count) cbSplitMode.SelectedIndex = s.SplitModeIndex;
            nudDefragThold.Value         = Clamp(s.DefragThold, nudDefragThold);

            string host = s.Host ?? "127.0.0.1";
            if (host == "0.0.0.0")      rdoHostAll.Checked    = true;
            else if (host == "127.0.0.1") rdoHostLocal.Checked = true;
            else { rdoHostCustom.Checked = true; txtHostCustom.Text = host; }
            nudPort.Value                = Clamp(s.Port, nudPort);
            txtTools.Text                = s.Tools ?? "";
            nudCacheReuse.Value          = Clamp(s.CacheReuse, nudCacheReuse);

            nudTemperature.Value        = Clamp(s.Temperature, nudTemperature);
            nudTopK.Value               = Clamp(s.TopK, nudTopK);
            nudTopP.Value               = Clamp(s.TopP, nudTopP);
            nudMinP.Value               = Clamp(s.MinP, nudMinP);
            chkSeedRandom.Checked       = s.SeedRandom;
            nudSeed.Value               = Clamp(s.Seed, nudSeed);
            nudRepeatPenalty.Value      = Clamp(s.RepeatPenalty, nudRepeatPenalty);

            txtApiKey.Text              = s.ApiKey ?? "";
            chkEmbedding.Checked        = s.Embedding;
            chkRerank.Checked           = s.Rerank;
            chkMetrics.Checked          = s.Metrics;
            txtExtraArgs.Text           = s.ExtraArgs ?? "";
            chkMmproj.Checked           = s.MmprojEnabled;
            _mmprojPath                 = s.MmprojPath ?? "";
            txtMmprojPath.Text          = _mmprojPath;
        }

        private void SaveConfig()
        {
            var s = new AppSettings
            {
                Folder         = _modelFolder ?? "",
                ExePath        = txtExePath.Text,
                Model          = cbModel.SelectedItem?.ToString() ?? "",
                NglMode        = chkNglAuto.Checked ? 0 : trkGpuLayers.Value == 0 ? 1 : trkGpuLayers.Value >= trkGpuLayers.Maximum ? 2 : 3,
                GpuLayers      = trkGpuLayers.Value,
                CtxDefault     = chkCtxDefault.Checked,
                CtxSize        = (int)nudCtxSize.Value,
                BatchSize      = (int)nudBatchSize.Value,
                UBatchSize     = (int)nudUBatchSize.Value,
                CacheKIndex    = cbCacheK.SelectedIndex,
                CacheVIndex    = cbCacheV.SelectedIndex,
                ReasoningIndex = cbReasoning.SelectedIndex,
                ThreadsAuto      = chkThreadsAuto.Checked,
                Threads          = (int)nudThreads.Value,
                ThreadsBatchAuto = chkThreadsBatchAuto.Checked,
                ThreadsBatch     = (int)nudThreadsBatch.Value,
                Parallel         = (int)nudParallel.Value,
                FlashAttn        = chkFlashAttn.Checked,
                ContBatching     = chkContBatching.Checked,
                ContextShift     = chkContextShift.Checked,
                Mmap             = chkMmap.Checked,
                Mlock            = chkMlock.Checked,
                KvOffload        = chkKvOffload.Checked,
                SplitModeIndex   = cbSplitMode.SelectedIndex,
                DefragThold      = nudDefragThold.Value,
                Host             = HostValue,
                Port             = (int)nudPort.Value,
                Tools            = txtTools.Text,
                CacheReuse       = (int)nudCacheReuse.Value,
                Temperature    = nudTemperature.Value,
                TopK           = (int)nudTopK.Value,
                TopP           = nudTopP.Value,
                MinP           = nudMinP.Value,
                SeedRandom     = chkSeedRandom.Checked,
                Seed           = nudSeed.Value,
                RepeatPenalty  = nudRepeatPenalty.Value,
                ApiKey         = txtApiKey.Text,
                Embedding         = chkEmbedding.Checked,
                Rerank            = chkRerank.Checked,
                Metrics           = chkMetrics.Checked,
                ExtraArgs         = txtExtraArgs.Text,
                MmprojEnabled     = chkMmproj.Checked,
                MmprojPath        = _mmprojPath,
            };
            try { File.WriteAllText(_configPath, JsonSerializer.Serialize(s, _jsonOptsIndented)); } catch { }
        }

        private static readonly Regex _tokPerSecRx = new(@"([\d.]+)\s+tokens per second", RegexOptions.Compiled);

        private void AppendLog(string text, bool isError)
        {
            // Suppress noisy internal server status lines
            if (text.Contains("GET /slots") || text.Contains("all slots are idle")) return;

            // Detect when the server finishes loading and is ready to serve
            if (!_serverReady && (text.Contains("server is listening") || text.Contains("all slots are ready")))
            {
                _serverReady = true;
                this.BeginInvoke(new Action(() =>
                {
                    lblStatus.Text      = "Running…";
                    lblStatus.ForeColor = System.Drawing.SystemColors.ControlText;
                }));
            }

            // Parse context size from startup line, e.g. "n_ctx = 4096"
            if (text.Contains("n_ctx") && !text.Contains("n_ctx_train"))
            {
                var m = _rxCtxMax.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int v) && v > 0)
                    _ctxMax = v;
            }

            // Parse current token count from slot progress/release lines
            // e.g. "prompt processing progress, n_tokens = 2048" or "stop processing: n_tokens = 16484"
            if (text.Contains("n_tokens") && (text.Contains("prompt processing") || text.Contains("stop processing:")))
            {
                var m = _rxNTokens.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int v) && v > 0)
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

            // Reflect actual offloaded layer count on the slider, e.g.:
            // "llm_load_tensors: offloaded 30/30 layers to GPU"
            if (text.Contains("layers to GPU"))
            {
                var m = _rxOffloadedLayers.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int offloaded))
                    BeginInvoke(new Action(() =>
                    {
                        int clamped = Math.Clamp(offloaded, 0, trkGpuLayers.Maximum);
                        if (offloaded >= trkGpuLayers.Maximum) clamped = trkGpuLayers.Maximum;
                        trkGpuLayers.Value = clamped;
                        UpdateNglDisplay();
                    }));
            }

            // Capture the reduced context size from the -fit log line, e.g.:
            // "context size reduced from 262144 to 4096 -> need 885 MiB less memory in total"
            if (text.Contains("context size reduced from"))
            {
                var m = _rxFitCtxReduced.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int reduced) && reduced > 0)
                    _fitReducedCtxSize = reduced;
            }

            // Capture the committed GPU layer count chosen by -fit, e.g.:
            // "common_params_fit_impl: set ngl_per_device[0].n_layer=62"
            // Ignore the "ngl_per_device_high" lines — those are the binary-search upper bound.
            if (text.Contains("set ngl_per_device[0].n_layer="))
            {
                var m = _rxFitNgl.Match(text);
                if (m.Success && int.TryParse(m.Groups[1].Value, out int ngl) && ngl > 0)
                    _fitAdjustedNgl = ngl;
            }

            // Notify when -fit successfully altered params (context and/or GPU layers changed)
            if (!_fitSuccessNotified && text.Contains("successfully fit params to free device memory"))
            {
                _fitSuccessNotified = true;
                int newCtx = _fitReducedCtxSize;
                int newNgl = _fitAdjustedNgl;
                BeginInvoke(new Action(() =>
                {
                    var parts = new List<string>();
                    if (newCtx > 0) parts.Add($"• Context window reduced to {newCtx:N0} tokens");
                    // Only report ngl if it was actually reduced below the model maximum
                    bool nglReduced = newNgl > 0 && newNgl < trkGpuLayers.Maximum;
                    if (nglReduced) parts.Add($"• GPU layers reduced to {newNgl} (of {trkGpuLayers.Maximum})");

                    if (parts.Count == 0) return; // nothing significant changed — no need to interrupt

                    string detail = string.Join("\n", parts);
                    MessageBox.Show(
                        "The auto-fit algorithm adjusted parameters to fit within available VRAM:\n\n" +
                        detail + "\n\n" +
                        "These values have been updated in the UI and saved so they take effect on next launch.",
                        "Parameters auto-adjusted by -fit",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);

                    if (newCtx > 0)
                    {
                        chkCtxDefault.Checked = false;
                        nudCtxSize.Value = Math.Max(nudCtxSize.Minimum, Math.Min(nudCtxSize.Maximum, newCtx));
                    }
                    if (nglReduced)
                        SetNglSlider(newNgl);
                    // Sync _sessionArgs so refreshPreview doesn't flag a restart —
                    // the server is already running with these adjusted values.
                    if (cbModel.SelectedItem != null)
                        _sessionArgs = BuildCommand(cbModel.SelectedItem.ToString() + ".gguf");
                    SaveConfig();
                    UpdateCommandPreview();
                }));
            }

            // Detect -fit abort when GPU layers are locked to 999 (GPU only mode)
            if (!_fitAbortedNgl999 && text.Contains("failed to fit params to free device memory") && text.Contains("n_gpu_layers already set by user to 999"))
            {
                _fitAbortedNgl999 = true;
                int newCtx = _fitReducedCtxSize;
                BeginInvoke(new Action(() =>
                {
                    string ctxDetail = newCtx > 0 ? $" Context was reduced to {newCtx:N0} tokens." : "";
                    MessageBox.Show(
                        "The server could not reserve VRAM headroom because GPU Layers is set to \"GPU only\" (-ngl 999).\n\n" +
                        $"The auto-fit algorithm reduced your context size instead.{ctxDetail}\n\n" +
                        "To let the fit algorithm also adjust layer count: uncheck \"Auto\" on the GPU Layers slider and move it away from the GPU-only end.",
                        "Context auto-reduced — GPU layers locked",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);

                    if (newCtx > 0)
                    {
                        chkCtxDefault.Checked = false;
                        nudCtxSize.Value = Math.Max(nudCtxSize.Minimum, Math.Min(nudCtxSize.Maximum, newCtx));
                    }
                }));
            }

            // Write to log file; rtbLog is refreshed on-demand when the Log tab is opened
            lock (_logLock)
            {
                try { _logWriter?.WriteLine((isError ? "[E] " : "") + text.Replace("\a", "")); } catch { }
            }
        }

        private void ClearLog()
        {
            lock (_logLock)
            {
                _logWriter?.Dispose();
                _logWriter = null;
                try { File.Delete(_logFilePath); } catch { }

                bool running = false;
                try { running = _proc != null && !_proc.HasExited; } catch { }
                if (running)
                    try { _logWriter = new StreamWriter(_logFilePath, append: false) { AutoFlush = true }; } catch { }
            }
            rtbLog.Clear();
            _logViewRenderedBytes = -1;
        }

        private static readonly System.Drawing.Color _logYellow = System.Drawing.Color.FromArgb(255, 210, 60);
        private static readonly System.Drawing.Color _logAmber  = System.Drawing.Color.FromArgb(255, 160, 30);
        private static readonly System.Drawing.Color _logRed    = System.Drawing.Color.FromArgb(255, 80,  80);
        private static readonly System.Drawing.Color _logNormal = System.Drawing.Color.FromArgb(190, 190, 190);
        private static readonly System.Drawing.Color _logDim    = System.Drawing.Color.FromArgb(100, 100, 100);

        private static System.Drawing.Color ClassifyLogLine(string raw)
        {
            // Strip the stderr prefix before matching — llama-server writes everything to stderr
            // so [E] does not mean the line is actually an error.
            var line = raw.StartsWith("[E] ") ? raw.Substring(4) : raw;

            if (line.StartsWith("---"))                                                          return _logYellow;
            if (line.Contains("server is listening") || line.Contains("all slots are ready"))   return _logYellow;
            if (line.Contains("tokens per second")   || line.Contains("eval time"))             return _logYellow;
            if (line.Contains("llm_load_tensors")    || line.Contains("llama_model_load"))      return _logYellow;
            if (line.Contains("WARN")                || line.Contains("warning"))               return _logAmber;
            if (line.Contains("ERROR")               || line.Contains("error:") ||
                line.Contains("failed to")           || line.Contains("FAILED"))                return _logRed;
            return _logNormal;
        }

        // Sync — safe to call from Task.Run
        private string[] ReadLogLines()
        {
            if (!File.Exists(_logFilePath)) return null;
            try
            {
                // FileShare.ReadWrite lets us read while _logWriter may still have the file open
                using var fs = new FileStream(_logFilePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
                using var sr = new StreamReader(fs);
                var lines = sr.ReadToEnd().Split(["\r\n", "\r", "\n"], StringSplitOptions.None);
                _logViewRenderedBytes = fs.Length;
                return lines;
            }
            catch { return null; }
        }

        // UI thread only
        private void RenderLogLines(string[] lines)
        {
            rtbLog.SuspendLayout();
            rtbLog.Clear();
            foreach (var line in lines)
            {
                bool isErr = line.StartsWith("[E] ");
                rtbLog.SelectionColor = ClassifyLogLine(line);
                rtbLog.AppendText((isErr ? line.Substring(4) : line) + Environment.NewLine);
            }
            rtbLog.ResumeLayout();
            rtbLog.SelectionStart = rtbLog.TextLength;
            rtbLog.ScrollToCaret();
        }

        private void RefreshLogView()
        {
            var lines = ReadLogLines();
            if (lines != null) RenderLogLines(lines);
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

        private async Task UpdateModelMetaAsync()
        {
            if (cbModel.SelectedItem == null || string.IsNullOrEmpty(_modelFolder))
            {
                lblCtxSize.Text    = "";
                lblLayerCount.Text = "";
                return;
            }
            string path = Path.Combine(_modelFolder, cbModel.SelectedItem.ToString() + ".gguf");
            var (ctx, layers) = await Task.Run(() => ReadGgufMeta(path));
            _modelLayerCount  = layers > 0 ? (int)layers : 0;
            _modelDefaultCtx  = ctx    > 0 ? (int)ctx    : 0;
            lblCtxSize.Text    = ctx    > 0 ? $"({ctx:N0})"       : "";
            lblLayerCount.Text = layers > 0 ? $"({layers + 1} layers)" : "";
            UpdateCtxPerSlot();
            if (_modelLayerCount > 0)
            {
                // llama.cpp counts block_count + 1 output layer, so ngl max = block_count + 1
                int nglMax = _modelLayerCount + 1;
                bool wasAtMax = trkGpuLayers.Value >= trkGpuLayers.Maximum;
                trkGpuLayers.Maximum = nglMax;
                trkGpuLayers.Value = wasAtMax ? nglMax : Math.Clamp(trkGpuLayers.Value, 0, nglMax);
                UpdateNglDisplay();
            }
        }

        private void UpdateCtxPerSlot()
        {
            int slots = (int)nudParallel.Value;
            int ctx = chkCtxDefault.Checked ? _modelDefaultCtx : (int)nudCtxSize.Value;
            lblCtxPerSlot.Text = ctx > 0 ? $"({ctx / slots:N0} tok/slot)" : "";
        }

        private static (long Ctx, long Layers) ReadGgufMeta(string path)
        {
            long ctx = -1, layers = -1;
            try
            {
                using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536);
                using var br = new BinaryReader(fs);

                if (br.ReadUInt32() != 0x46554747u) return (-1, -1);  // 'GGUF' magic
                uint version = br.ReadUInt32();
                if (version < 1 || version > 3) return (-1, -1);

                bool v1 = version == 1;
                _ = v1 ? br.ReadUInt32() : br.ReadUInt64();  // n_tensors (unused)
                ulong nKv = v1 ? br.ReadUInt32() : br.ReadUInt64();

                for (ulong i = 0; i < nKv; i++)
                {
                    string key = ReadGgufString(br, v1);
                    uint   vt  = br.ReadUInt32();

                    bool isCtx    = key.EndsWith(".context_length", StringComparison.Ordinal);
                    bool isLayers = key.EndsWith(".block_count",    StringComparison.Ordinal);

                    if (isCtx || isLayers)
                    {
                        long val = vt switch
                        {
                            4  => br.ReadUInt32(),
                            5  => br.ReadInt32(),
                            10 => (long)br.ReadUInt64(),
                            11 => br.ReadInt64(),
                            _  => -1L
                        };
                        if (isCtx)    ctx    = val;
                        if (isLayers) layers = val;
                        if (ctx > 0 && layers > 0) break;
                        continue;
                    }

                    SkipGgufValue(br, vt, v1);
                }
            }
            catch { }
            return (ctx, layers);
        }

        private static string ReadGgufString(BinaryReader br, bool v1)
        {
            int len = (int)Math.Min(v1 ? (ulong)br.ReadUInt32() : br.ReadUInt64(), 4096UL);
            return System.Text.Encoding.UTF8.GetString(br.ReadBytes(len));
        }

        private static void SkipGgufValue(BinaryReader br, uint vt, bool v1)
        {
            switch (vt)
            {
                case 0: case 1: case 7: br.ReadByte(); break;
                case 2: case 3: br.BaseStream.Seek(2, SeekOrigin.Current); break;
                case 4: case 5: case 6: br.BaseStream.Seek(4, SeekOrigin.Current); break;
                case 8:
                    br.BaseStream.Seek((long)(v1 ? br.ReadUInt32() : br.ReadUInt64()), SeekOrigin.Current);
                    break;
                case 9:
                    uint et  = br.ReadUInt32();
                    long cnt = (long)(v1 ? br.ReadUInt32() : br.ReadUInt64());
                    int esz  = et switch { 0 or 1 or 7 => 1, 2 or 3 => 2, 4 or 5 or 6 => 4, 10 or 11 or 12 => 8, _ => 0 };
                    if (esz > 0) br.BaseStream.Seek(cnt * esz, SeekOrigin.Current);
                    else for (long j = 0; j < cnt; j++) SkipGgufValue(br, et, v1);
                    break;
                case 10: case 11: case 12: br.BaseStream.Seek(8, SeekOrigin.Current); break;
            }
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

            float recentPpTps = -1;
            if (_metricsPrefillTokPerSec.Count > 0)
            {
                int take = Math.Min(5, _metricsPrefillTokPerSec.Count);
                recentPpTps = (float)_metricsPrefillTokPerSec.Skip(_metricsPrefillTokPerSec.Count - take).Average();
            }

            if (!serverRunning)
            {
                // Static pre-flight advice based on current settings
                int nglTip = GpuLayersValue;
                if (hasGpu && nglTip == 0)
                    Line("▶ GPU layers set to CPU only — model will run entirely on CPU. Switch to \"GPU only\" or \"Custom\" to use the GPU.", amber);
                else if (hasGpu && nglTip == -1)
                    Line("▶ GPU layers is Auto. Try a Custom value if GPU usage seems low after starting.", dim);

                int ctxTip = chkCtxDefault.Checked ? 0 : (int)nudCtxSize.Value;
                if (ctxTip >= 8192 && !chkFlashAttn.Checked)
                    Line("▶ Large context planned. Enable Flash Attention (-fa) to cut KV VRAM usage for long contexts.", amber);

                if (cbCacheK.Text == "f16" && cbCacheV.Text == "f16" && hasGpu)
                    Line("▶ KV cache is f16 (default). Switching to q8_0 halves KV VRAM with minimal quality impact.", dim);

                if (vramPct > 70)
                    Line($"▶ VRAM is {vramPct:F0}% full before the server starts. Another process may be using the GPU — close other apps or reduce GPU layers.", amber);

                if (rtbTips.TextLength == 0)
                    Line("Start the server and send some requests to see live performance tips.", dim);
                return;
            }

            // ── Live tips ────────────────────────────────────────────────

            // 1. Generation speed (tg)
            if (recentTps >= 40)
                Line($"▶ Generation (tg): {recentTps:F1} t/s — excellent.", green);
            else if (recentTps >= 20)
                Line($"▶ Generation (tg): {recentTps:F1} t/s — good.", green);
            else if (recentTps >= 8)
                Line(GpuLayersValue == 999
                    ? $"▶ Generation (tg): {recentTps:F1} t/s — moderate. All layers are already on GPU; try reducing context size (-c) or enabling Flash Attention to improve speed."
                    : $"▶ Generation (tg): {recentTps:F1} t/s — moderate. Consider increasing GPU layers (-ngl) if VRAM has headroom.", amber);
            else if (recentTps >= 0)
                Line($"▶ Generation (tg): {recentTps:F1} t/s — slow. Most work is likely on CPU; increase -ngl to offload more layers to GPU.", red);
            else
                Line("▶ No requests completed yet — send a prompt to measure generation speed.", dim);

            // 2. Prompt processing speed (pp)
            if (recentPpTps >= 500)
                Line($"▶ Prompt processing (pp): {recentPpTps:F0} t/s — excellent.", green);
            else if (recentPpTps >= 200)
                Line($"▶ Prompt processing (pp): {recentPpTps:F0} t/s — good.", green);
            else if (recentPpTps >= 50)
                Line($"▶ Prompt processing (pp): {recentPpTps:F0} t/s — moderate.", amber);
            else if (recentPpTps >= 0)
                Line($"▶ Prompt processing (pp): {recentPpTps:F0} t/s — slow. Increasing batch size (-b) or GPU layers may help.", red);

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
            bool kvInVram = chkKvOffload.Checked;
            if (vramPct > 95)
            {
                if (kvInVram)
                    Line($"▶ VRAM critically full ({vramPct:F0}%). Reduce context size (-c) or switch KV cache to q8_0/q4_0 to avoid OOM.", red);
                else
                    Line($"▶ VRAM critically full ({vramPct:F0}%). Reduce GPU layers (-ngl) to move model weight layers to CPU/RAM.", red);
            }
            else if (vramPct > 85)
            {
                if (kvInVram && (cbCacheK.Text == "f16" || cbCacheV.Text == "f16"))
                    Line($"▶ VRAM at {vramPct:F0}%. Switch KV cache from f16 → q8_0 to recover ~50% of KV memory with minimal quality loss.", amber);
                else if (kvInVram)
                    Line($"▶ VRAM at {vramPct:F0}% — reduce context size (-c) if you hit out-of-memory errors.", amber);
                else
                    Line($"▶ VRAM at {vramPct:F0}% (KV cache is in RAM). Reduce -ngl if VRAM runs out.", amber);
            }
            else if (vramPct >= 0 && vramPct < 60 && GpuLayersValue >= 0 && GpuLayersValue < 999)
                Line($"▶ VRAM has headroom ({vramPct:F0}% used). You could increase GPU layers or context size.", green);

            // 4. Flash attention for large contexts
            int ctxLive = chkCtxDefault.Checked ? 0 : (int)nudCtxSize.Value;
            if (ctxLive >= 8192 && !chkFlashAttn.Checked)
                Line($"▶ Context is {ctxLive:N0} tokens — enable Flash Attention (-fa) to reduce VRAM usage and improve long-context speed.", amber);
        }

        private void SavePerformanceSession(int _)
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

            bool serverRunningForHighlight = false;
            try { serverRunningForHighlight = _proc != null && !_proc.HasExited; } catch { }

            foreach (var modelGroup in resolved.GroupBy(x => x.r.Model).OrderBy(g => g.Key))
            {
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

                double bestTps    = byConfig.Count > 0 ? byConfig.Max(c => c.GenAvg) : -1;
                double bestPpTps  = byConfig.Count > 0 ? byConfig.Max(c => c.PrefillAvg) : -1;
                bool isActive     = serverRunningForHighlight && _sessionModel == modelGroup.Key;
                string tpsSuffix  = bestTps >= 0 ? $"  (tg {bestTps:F1} t/s" + (bestPpTps >= 0 ? $"  pp {bestPpTps:F0} t/s)" : ")") : "";
                string suffix     = tpsSuffix + (isActive ? "  <- currently loaded model" : "");

                var modelNode = new TreeNode($"{modelGroup.Key}{suffix}")
                {
                    ForeColor = green,
                    NodeFont  = boldFont
                };
                treePerf.Nodes.Add(modelNode);

                for (int i = 0; i < byConfig.Count; i++)
                {
                    var (ArgsLabel, Params, GenAvg, PrefillAvg, Runs) = byConfig[i];
                    bool isBest = i == 0 && byConfig.Count > 1;

                    string star       = isBest ? "★ " : "  ";
                    string tpsPart    = GenAvg >= 0
                        ? $"tg {GenAvg:F1} t/s" + (PrefillAvg >= 0 ? $"  pp {PrefillAvg:F0} t/s" : "") + " avg"
                        : "no data";
                    string countPart  = Runs.Count == 1 ? "1 run" : $"{Runs.Count} runs";
                    bool isCurrentConfig = isActive && ArgsLabel == StripModelArg(_sessionArgs ?? "");
                    string configText = $"{star}{tpsPart}  ({countPart})   {ArgsLabel}"
                                      + (isCurrentConfig ? "  <- current settings" : "");

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
                // collapsed by default — user expands what they need
            }

            treePerf.EndUpdate();
        }

#pragma warning disable CS8632
        private record PerfParams(
            int CtxSize, int GpuLayers, int Threads, int BatchSize, int UbatchSize, bool FlashAttn,
            string CacheTypeK = "f16", string CacheTypeV = "f16", string MmprojPath = "",
            int ThreadsBatch = -1, string SplitMode = "layer", bool KvOffload = true, bool ContextShift = true)
        {
            public string ToLabel()
            {
                var parts = new List<string>();
                if (CtxSize      > 0)                      parts.Add($"ctx={CtxSize:N0}");
                if (GpuLayers    >= 0)                     parts.Add($"ngl={GpuLayers}");
                if (Threads      >= 0)                     parts.Add($"t={Threads}");
                if (ThreadsBatch >= 0)                     parts.Add($"tb={ThreadsBatch}");
                if (BatchSize    > 0)                      parts.Add($"batch={BatchSize}");
                if (UbatchSize   > 0)                      parts.Add($"ubatch={UbatchSize}");
                if (FlashAttn)                             parts.Add("flash=yes");
                if (CacheTypeK  != "f16")                  parts.Add($"ctk={CacheTypeK}");
                if (CacheTypeV  != "f16")                  parts.Add($"ctv={CacheTypeV}");
                if (SplitMode   != "layer")                parts.Add($"sm={SplitMode}");
                if (!KvOffload)                            parts.Add("no-kvo");
                if (!ContextShift)                         parts.Add("no-ctx-shift");
                if (!string.IsNullOrEmpty(MmprojPath))     parts.Add($"mmproj={Path.GetFileName(MmprojPath)}");
                return parts.Count > 0 ? string.Join("  ", parts) : "(all defaults)";
            }

            public string ToKey() => $"{CtxSize}|{GpuLayers}|{Threads}|{ThreadsBatch}|{BatchSize}|{UbatchSize}|{FlashAttn}|{CacheTypeK}|{CacheTypeV}|{SplitMode}|{KvOffload}|{ContextShift}|{MmprojPath}";
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

            static void SelectCombo(ComboBox cb, string value)
            {
                for (int i = 0; i < cb.Items.Count; i++)
                    if (cb.Items[i]?.ToString() == value) { cb.SelectedIndex = i; return; }
            }

            // ── Settings captured in PerfParams ───────────────────────────
            SetNglSlider(p.GpuLayers);

            if (p.Threads == -1) chkThreadsAuto.Checked = true;
            else { chkThreadsAuto.Checked = false; nudThreads.Value = Clamp(p.Threads, nudThreads); }

            if (p.ThreadsBatch == -1) chkThreadsBatchAuto.Checked = true;
            else { chkThreadsBatchAuto.Checked = false; nudThreadsBatch.Value = Clamp(p.ThreadsBatch, nudThreadsBatch); }

            if (p.CtxSize == 0) chkCtxDefault.Checked = true;
            else { chkCtxDefault.Checked = false; nudCtxSize.Value = Clamp(p.CtxSize, nudCtxSize); }

            nudBatchSize.Value    = Clamp(p.BatchSize  > 0 ? p.BatchSize  : 2048, nudBatchSize);
            nudUBatchSize.Value   = Clamp(p.UbatchSize > 0 ? p.UbatchSize : 512,  nudUBatchSize);
            chkFlashAttn.Checked  = p.FlashAttn;
            chkKvOffload.Checked  = p.KvOffload;
            chkContextShift.Checked = p.ContextShift;
            SelectCombo(cbCacheK, p.CacheTypeK);
            SelectCombo(cbCacheV, p.CacheTypeV);
            SelectCombo(cbSplitMode, p.SplitMode);

            _mmprojPath             = p.MmprojPath ?? "";
            txtMmprojPath.Text      = _mmprojPath;
            chkMmproj.Checked       = !string.IsNullOrEmpty(_mmprojPath);
            txtMmprojPath.Enabled   = chkMmproj.Checked;
            btnBrowseMmproj.Enabled = chkMmproj.Checked;

            // ── Reset all other settings to their defaults ─────────────────
            nudParallel.Value          = 1;
            chkContBatching.Checked    = false;
            chkMmap.Checked            = true;
            chkMlock.Checked           = false;
            nudDefragThold.Value       = Clamp(-1M, nudDefragThold);
            nudCacheReuse.Value        = 0;
            cbReasoning.SelectedIndex  = 0;   // auto
            nudTemperature.Value       = Clamp(0.80M, nudTemperature);
            nudTopK.Value              = Clamp(40,    nudTopK);
            nudTopP.Value              = Clamp(0.95M, nudTopP);
            nudMinP.Value              = Clamp(0.05M, nudMinP);
            chkSeedRandom.Checked      = true;
            nudSeed.Value              = 0;
            nudRepeatPenalty.Value     = Clamp(1.00M, nudRepeatPenalty);
            chkEmbedding.Checked       = false;
            chkRerank.Checked          = false;
            chkMetrics.Checked         = false;

            tabMain.SelectedTab = tabModel;
        }

        private sealed class AppSettings
        {
            public string  Folder         { get; set; } = "";
            public string  ExePath        { get; set; } = "";
            public string  Model          { get; set; } = "";
            public int     NglMode        { get; set; } = 0;
            public decimal GpuLayers      { get; set; } = 32;
            public bool    CtxDefault     { get; set; } = true;
            public decimal CtxSize        { get; set; } = 4096;
            public decimal BatchSize      { get; set; } = 2048;
            public decimal UBatchSize     { get; set; } = 512;
            public int     CacheKIndex    { get; set; } = 0;
            public int     CacheVIndex    { get; set; } = 0;
            public int     ReasoningIndex { get; set; } = 0;
            public bool    ThreadsAuto    { get; set; } = true;
            public decimal Threads           { get; set; } = 4;
            public bool    ThreadsBatchAuto  { get; set; } = true;
            public decimal ThreadsBatch      { get; set; } = 4;
            public decimal Parallel          { get; set; } = 1;
            public bool    FlashAttn         { get; set; } = false;
            public bool    ContBatching      { get; set; } = false;
            public bool    ContextShift      { get; set; } = true;
            public bool    Mmap              { get; set; } = true;
            public bool    Mlock             { get; set; } = false;
            public bool    KvOffload         { get; set; } = true;
            public int     SplitModeIndex    { get; set; } = 0;
            public decimal DefragThold       { get; set; } = -1M;
            public string  Host              { get; set; } = "127.0.0.1";
            public decimal Port              { get; set; } = 8080;
            public string  Tools             { get; set; } = "";
            public decimal CacheReuse        { get; set; } = 0;
            public decimal Temperature    { get; set; } = 0.80M;
            public decimal TopK           { get; set; } = 40;
            public decimal TopP           { get; set; } = 0.95M;
            public decimal MinP           { get; set; } = 0.05M;
            public bool    SeedRandom     { get; set; } = true;
            public decimal Seed           { get; set; } = 0;
            public decimal RepeatPenalty  { get; set; } = 1.00M;
            public string  ApiKey         { get; set; } = "";
            public bool    Embedding      { get; set; } = false;
            public bool    Rerank         { get; set; } = false;
            public bool    Metrics           { get; set; } = false;
            public string  ExtraArgs         { get; set; } = "";
            public bool    MmprojEnabled     { get; set; } = false;
            public string  MmprojPath        { get; set; } = "";
        }

        private static readonly Regex _rxCtx        = new(@"(?:-c|--ctx-size)\s+(\d+)",         RegexOptions.Compiled);
        private static readonly Regex _rxNgl        = new(@"(?:-ngl|--n-gpu-layers)\s+(\d+)",   RegexOptions.Compiled);
        private static readonly Regex _rxT          = new(@"(?:-t|--threads)\s+(\d+)",          RegexOptions.Compiled);
        private static readonly Regex _rxTb         = new(@"-tb\s+(\d+)",                       RegexOptions.Compiled);
        private static readonly Regex _rxBatch      = new(@"(?:-b|--batch-size)\s+(\d+)",       RegexOptions.Compiled);
        private static readonly Regex _rxUbatch     = new(@"(?:-ub|--ubatch-size)\s+(\d+)",     RegexOptions.Compiled);
        private static readonly Regex _rxCacheK     = new(@"--cache-type-k\s+(\S+)",            RegexOptions.Compiled);
        private static readonly Regex _rxCacheV     = new(@"--cache-type-v\s+(\S+)",            RegexOptions.Compiled);
        private static readonly Regex _rxSplitMode  = new(@"--split-mode\s+(\S+)",              RegexOptions.Compiled);
        private static readonly Regex _rxMmproj     = new(@"--mmproj\s+""([^""]+)""",           RegexOptions.Compiled);
        private static readonly Regex _rxModelArg   = new(@"-m\s+""[^""]*""\s*",               RegexOptions.Compiled);

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
                CtxSize:      GetInt(_rxCtx),
                GpuLayers:    GetInt(_rxNgl,  absent: -1),
                Threads:      GetInt(_rxT,    absent: -1),
                BatchSize:    GetInt(_rxBatch),
                UbatchSize:   GetInt(_rxUbatch),
                FlashAttn:    HasFlag("--flash-attn") || HasFlag("-fa"),
                CacheTypeK:   GetStr(_rxCacheK, "f16"),
                CacheTypeV:   GetStr(_rxCacheV, "f16"),
                MmprojPath:   GetStr(_rxMmproj, ""),
                ThreadsBatch: GetInt(_rxTb, absent: -1),
                SplitMode:    GetStr(_rxSplitMode, "layer"),
                KvOffload:    !HasFlag("--no-kv-offload"),
                ContextShift: !HasFlag("--no-context-shift"));
        }
    }
}
