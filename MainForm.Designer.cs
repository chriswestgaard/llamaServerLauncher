using System.Drawing;
using System.Windows.Forms;

namespace LlamaServerLauncher
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Tabs
        private TabControl tabMain;
        private TabPage tabModel, tabServer, tabSampling, tabAdvanced, tabLog, tabPerf;

        // Log tab
        private RichTextBox rtbLog;
        private Button btnClearLog;

        // Perf tab
        private TreeView treePerf;
        private Button btnClearPerf;

        // Model tab
        private RichTextBox rtbTips;

        private Button btnBrowse;
        private ComboBox cbModel;
        private NumericUpDown nudGpuLayers, nudCtxSize;
        private ComboBox cbNglMode;
        private CheckBox chkThreadsAuto, chkCtxDefault, chkSeedRandom;
        private TextBox txtCpuInfo, txtGpuInfo;
        private UsageGraph graphCpu, graphRam, graphGpu, graphVram, graphCtx;

        // Server tab
        private TextBox txtHost, txtTools;
        private NumericUpDown nudPort, nudThreads, nudParallel, nudBatchSize, nudUBatchSize;
        private CheckBox chkFlashAttn, chkContBatching;

        // Cache tab
        private ComboBox cbCacheK, cbCacheV, cbReasoning;
        private CheckBox chkMmap, chkMlock;

        // Sampling tab
        private NumericUpDown nudTemperature, nudTopK, nudTopP, nudMinP, nudSeed, nudRepeatPenalty;

        // Advanced tab
        private TextBox txtApiKey, txtExePath, txtExtraArgs;
        private Button btnBrowseExe;
        private CheckBox chkEmbedding, chkRerank, chkMetrics;

        // Bottom chrome
        private TableLayoutPanel tlpMain;
        private TextBox txtCmdPreview;
        private Button btnLaunch;
        private Button btnOpenChat;
        private Label lblStatus;

        // Tooltips
        private ToolTip _tip;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
                components.Dispose();
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();

            _tip = new ToolTip { AutomaticDelay = 300, AutoPopDelay = 10000, InitialDelay = 300 };

            // ── Instantiate field controls ──────────────────────────────────
            this.tabMain        = new TabControl();
            this.tabModel       = new TabPage { Text = "Model"    };
            this.tabServer      = new TabPage { Text = "Server"   };
            this.tabSampling    = new TabPage { Text = "Sampling" };
            this.tabAdvanced    = new TabPage { Text = "Advanced" };


            this.btnBrowse       = new Button    { Text = "...", Dock = DockStyle.Fill };
            this.cbModel         = new ComboBox  { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbNglMode       = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbNglMode.Items.AddRange(new[] { "Auto", "CPU only", "GPU only", "Custom" });
            this.cbNglMode.SelectedIndex = 0;
            this.nudGpuLayers    = new NumericUpDown { Minimum = 1, Maximum = 998, Value = 32, Width = 60, Visible = false };
            this.chkCtxDefault   = new CheckBox { Text = "Model default", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 8, 0) };
            this.nudCtxSize      = new NumericUpDown { Minimum = 512, Maximum = 131072, Value = 4096, Increment = 1024, AutoSize = false, MinimumSize = new Size(120, 24), Visible = false };
            this.txtCpuInfo      = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = System.Drawing.SystemColors.Control, ForeColor = System.Drawing.Color.DimGray, TabStop = false };
            this.txtGpuInfo      = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = System.Drawing.SystemColors.Control, ForeColor = System.Drawing.Color.DimGray, TabStop = false };
            this.graphCpu  = new UsageGraph { Dock = DockStyle.Fill, Title = "CPU",     GraphColor = Color.FromArgb( 19, 194,  56), Margin = new Padding(0, 2, 2, 0) };
            this.graphRam  = new UsageGraph { Dock = DockStyle.Fill, Title = "RAM",     GraphColor = Color.FromArgb(  0, 183, 235), Margin = new Padding(2, 2, 0, 0) };
            this.graphGpu  = new UsageGraph { Dock = DockStyle.Fill, Title = "GPU",     GraphColor = Color.FromArgb(255, 183,   0), Margin = new Padding(0, 2, 2, 0) };
            this.graphVram = new UsageGraph { Dock = DockStyle.Fill, Title = "VRAM",    GraphColor = Color.FromArgb(255, 100,   0), Margin = new Padding(2, 2, 0, 0) };
            this.graphCtx  = new UsageGraph { Dock = DockStyle.Fill, Title = "Context", GraphColor = Color.FromArgb(180, 100, 255), Margin = new Padding(0, 2, 0,  0) };

            this.txtHost         = new TextBox   { Dock = DockStyle.Fill, Text = "127.0.0.1" };
            this.nudPort         = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 65535, Value = 8080  };
            this.chkThreadsAuto  = new CheckBox { Text = "Auto", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 8, 0) };
            this.nudThreads      = new NumericUpDown { Minimum = 1, Maximum = 256, Value = 4, Width = 60, Visible = false };
            this.nudParallel     = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 128,   Value = 1    };
            this.nudBatchSize    = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 65536, Value = 2048 };
            this.nudUBatchSize   = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 65536, Value = 512  };
            this.chkFlashAttn    = new CheckBox { Text = "Flash Attention",    AutoSize = true, Checked = false };
            this.chkContBatching = new CheckBox { Text = "Continuous Batching", AutoSize = true, Checked = false };
            this.txtTools        = new TextBox  { Dock = DockStyle.Fill, Text = "" };

            this.cbCacheK    = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbCacheV    = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbReasoning = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.chkMmap     = new CheckBox { Text = "Memory-map file  (mmap)",  AutoSize = true, Checked = true };
            this.chkMlock    = new CheckBox { Text = "Lock model in RAM  (mlock)", AutoSize = true };

            this.nudTemperature   = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 5M,          Value = 0.80M, DecimalPlaces = 2, Increment = 0.05M };
            this.nudTopK          = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0,  Maximum = 10000,        Value = 40    };
            this.nudTopP          = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 1M,           Value = 0.95M, DecimalPlaces = 2, Increment = 0.01M };
            this.nudMinP          = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 1M,           Value = 0.05M, DecimalPlaces = 2, Increment = 0.01M };
            this.chkSeedRandom    = new CheckBox { Text = "Random", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 8, 0) };
            this.nudSeed          = new NumericUpDown { Minimum = 0, Maximum = int.MaxValue, Value = 0, Width = 100, Visible = false };
            this.nudRepeatPenalty = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 3M,           Value = 1.00M, DecimalPlaces = 2, Increment = 0.05M };

            this.txtApiKey    = new TextBox { Dock = DockStyle.Fill };
            this.txtExePath   = new TextBox { Dock = DockStyle.Fill };
            this.txtExtraArgs = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 56, ScrollBars = ScrollBars.Vertical };
            this.btnBrowseExe = new Button  { Text = "...", Dock = DockStyle.Fill };
            this.chkEmbedding = new CheckBox { Text = "Embedding Mode",    AutoSize = true };
            this.chkRerank    = new CheckBox { Text = "Reranking Mode",   AutoSize = true };
            this.chkMetrics   = new CheckBox { Text = "Prometheus Metrics", AutoSize = true };

            this.tlpMain       = new TableLayoutPanel();
            this.txtCmdPreview = new TextBox { ReadOnly = true, Multiline = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 7.5F), BackColor = System.Drawing.SystemColors.ControlLight };
            this.btnLaunch     = new Button  { Text = "Launch llama-server", Dock = DockStyle.Fill };
            this.btnOpenChat   = new Button  { Text = "Open Chat UI", Dock = DockStyle.Fill, Enabled = false };
            this.lblStatus     = new Label   { AutoSize = false, Dock = DockStyle.Fill, Text = "", TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };

            // ── ComboBox items ──────────────────────────────────────────────
            var cacheTypes = new[] { "f16", "f32", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1" };
            this.cbCacheK.Items.AddRange(cacheTypes);   this.cbCacheK.SelectedIndex = 0;
            this.cbCacheV.Items.AddRange(cacheTypes);   this.cbCacheV.SelectedIndex = 0;
            this.cbReasoning.Items.AddRange(new[] { "auto", "on", "off" });
            this.cbReasoning.SelectedIndex = 0;

            // ── MODEL TAB ──────────────────────────────────────────────────
            var tlpModelRow = MakeRow2(this.cbModel, this.btnBrowse, 34);
            var tlpModel = MakeTlp(5);

            tlpModel.RowStyles[3] = new RowStyle(SizeType.Absolute, 160F);  // HW side-by-side panel
            tlpModel.RowStyles[4] = new RowStyle(SizeType.Absolute, 110F);  // Context graph

            AddRow(tlpModel, 0, MakeLbl("Model File"), tlpModelRow, "The .gguf model file to load and serve.");

            // ── 2-column performance settings (row 1) ────────────────────
            var tlpPerfCols = new TableLayoutPanel
            {
                AutoSize = true, Dock = DockStyle.Top,
                ColumnCount = 2, RowCount = 1, Margin = new Padding(0),
                MinimumSize = new Size(0, 60)
            };
            tlpPerfCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPerfCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpPerfCols.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            var tlpPerfL = MakeTlp(7);
            var pnlNgl = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlNgl.Controls.Add(this.cbNglMode);
            pnlNgl.Controls.Add(this.nudGpuLayers);
            this.cbNglMode.SelectedIndexChanged += (_, _) => this.nudGpuLayers.Visible = this.cbNglMode.SelectedIndex == 3;
            AddRow(tlpPerfL, 0, MakeLbl("GPU Layers  (-ngl)"),  pnlNgl, "Number of model layers to offload to GPU VRAM.\nAuto  = server decides at startup.\nCPU only  = no GPU offload.\nGPU only  = all layers (passes -ngl 999).\nCustom  = specify exact layer count. (-ngl)");

            var pnlCtx = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlCtx.Controls.Add(this.chkCtxDefault);
            pnlCtx.Controls.Add(this.nudCtxSize);
            pnlCtx.Controls[1].AutoSize = false;
            pnlCtx.Controls[1].MinimumSize = new Size(120, 24);
            pnlCtx.Controls[1].MaximumSize = new Size(150, 24);
            pnlCtx.Controls[1].Width = 150;
            this.chkCtxDefault.CheckedChanged += (_, _) => this.nudCtxSize.Visible = !this.chkCtxDefault.Checked;
            AddRow(tlpPerfL, 1, MakeLbl("Context Size  (-c)"),  pnlCtx, "Maximum number of tokens in the context window.\n\"Model default\" uses the value embedded in the model file.\nLarger contexts require more VRAM/RAM. (-c)");
            AddRow(tlpPerfL, 2, MakeLbl("Batch Size  (-b)"),     this.nudBatchSize,  "Logical maximum batch size (default: 2048).\nLarger values improve prompt-processing throughput. (-b)");
            AddRow(tlpPerfL, 3, MakeLbl("UBatch Size  (-ub)"),   this.nudUBatchSize, "Physical maximum batch size (default: 512).\nSmaller values reduce peak VRAM usage. (-ub)");
            AddRow(tlpPerfL, 4, MakeLbl("Cache Type K  (-ctk)"), this.cbCacheK,      "KV cache data type for Keys (default: f16).\nAllowed: f32, f16, bf16, q8_0, q4_0, q4_1, iq4_nl, q5_0, q5_1.\nbf16 / q8_0 saves VRAM with minor quality loss. (-ctk)");
            AddRow(tlpPerfL, 5, MakeLbl("Cache Type V  (-ctv)"), this.cbCacheV,      "KV cache data type for Values (default: f16).\nSame options as Cache Type K. (-ctv)");
            AddRow(tlpPerfL, 6, MakeLbl("Reasoning  (-rea)"),    this.cbReasoning,   "Use reasoning/thinking in chat (default: auto).\n'auto' = detect from model template.\n'on'   = enable reasoning tokens.\n'off'  = disable reasoning. (-rea)");

            var tlpPerfR = MakeTlp(6);
            var pnlThreads = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlThreads.Controls.Add(this.chkThreadsAuto);
            pnlThreads.Controls.Add(this.nudThreads);
            this.chkThreadsAuto.CheckedChanged += (_, _) => this.nudThreads.Visible = !this.chkThreadsAuto.Checked;
            AddRow(tlpPerfR, 0, MakeLbl("Threads  (-t)"),  pnlThreads, "CPU threads used during generation.\nAuto = server detects from CPU core count.\nFor best results, match to physical core count. (-t)");
            AddRow(tlpPerfR, 1, MakeLbl("Parallel Slots  (-np)"), this.nudParallel, "Number of parallel sequences to decode (default: 1).\nEach slot reserves additional KV-cache memory. (-np)");
            AddChk(tlpPerfR, 2, this.chkFlashAttn,    "Flash Attention: faster inference and lower VRAM usage (default: auto).\nChecked = force on; unchecked = use default (auto).\nRequires a compatible model. (-fa)");
            AddChk(tlpPerfR, 3, this.chkContBatching, "Process new requests without waiting for current ones to finish.\nImproves throughput under concurrent load. (-cb)");
            AddChk(tlpPerfR, 4, this.chkMmap,  "Memory-map model for faster load (default: enabled).\nUncheck to fully load into RAM (--no-mmap). (--mmap)");
            AddChk(tlpPerfR, 5, this.chkMlock, "Force system to keep model in RAM rather than swapping.\nRequires sufficient free RAM. (--mlock)");

            tlpPerfCols.Controls.Add(tlpPerfL, 0, 0);
            tlpPerfCols.Controls.Add(tlpPerfR, 1, 0);
            Span3(tlpModel, tlpPerfCols, 0, 1);

            // ── Hardware heading (row 2) ──────────────────────────────────
            var lblHwSep = new Label { Text = "Hardware", AutoSize = false, Dock = DockStyle.Fill, ForeColor = System.Drawing.Color.Gray, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(2, 6, 2, 6) };
            Span3(tlpModel, lblHwSep, 0, 2);

            // ── CPU/RAM dual-graph panel ──────────────────────────────────
            var tlpCpuGraphs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            tlpCpuGraphs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCpuGraphs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpCpuGraphs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCpuGraphs.Controls.Add(this.graphCpu, 0, 0);
            tlpCpuGraphs.Controls.Add(this.graphRam, 1, 0);

            // ── GPU/VRAM dual-graph panel ─────────────────────────────────
            var tlpGpuGraphs = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 1, Margin = new Padding(0) };
            tlpGpuGraphs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpGpuGraphs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpGpuGraphs.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpGpuGraphs.Controls.Add(this.graphGpu,  0, 0);
            tlpGpuGraphs.Controls.Add(this.graphVram, 1, 0);

            // ── CPU info header ───────────────────────────────────────────
            var tlpCpuHdr = new TableLayoutPanel { Dock = DockStyle.Top, Height = 26, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 4) };
            tlpCpuHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpCpuHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCpuHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tlpCpuHdr.Controls.Add(MakeInfo("CPU model, core count, and thread count."), 0, 0);
            tlpCpuHdr.Controls.Add(this.txtCpuInfo, 1, 0);

            // ── CPU column (info + graphs stacked) ───────────────────────
            var tlpCpuCol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(0, 0, 2, 0) };
            tlpCpuCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpCpuCol.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpCpuCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpCpuCol.Controls.Add(tlpCpuHdr,    0, 0);
            tlpCpuCol.Controls.Add(tlpCpuGraphs, 0, 1);

            // ── GPU info header ───────────────────────────────────────────
            var tlpGpuHdr = new TableLayoutPanel { Dock = DockStyle.Top, Height = 26, ColumnCount = 2, RowCount = 1, Margin = new Padding(0, 0, 0, 4) };
            tlpGpuHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpGpuHdr.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpGpuHdr.RowStyles.Add(new RowStyle(SizeType.Absolute, 26F));
            tlpGpuHdr.Controls.Add(MakeInfo("GPU model and total VRAM."), 0, 0);
            tlpGpuHdr.Controls.Add(this.txtGpuInfo, 1, 0);

            // ── GPU column (info + graphs stacked) ───────────────────────
            var tlpGpuCol = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 1, RowCount = 2, Margin = new Padding(2, 0, 0, 0) };
            tlpGpuCol.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpGpuCol.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpGpuCol.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpGpuCol.Controls.Add(tlpGpuHdr,    0, 0);
            tlpGpuCol.Controls.Add(tlpGpuGraphs, 0, 1);

            // ── Hardware side-by-side panel (row 3) ──────────────────────
            var tlpHwCols = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 2, RowCount = 2, Margin = new Padding(0) };
            tlpHwCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpHwCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 50F));
            tlpHwCols.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpHwCols.Controls.Add(tlpCpuCol, 0, 0);
            tlpHwCols.Controls.Add(tlpGpuCol, 1, 0);
            Span3(tlpModel, tlpHwCols, 0, 3);

            // ── Context graph (row 4) ─────────────────────────────────────
            Span3(tlpModel, this.graphCtx, 0, 4);

            // ── Performance tips — pinned to the bottom of the tab (always visible)
            this.rtbTips = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 8.5F),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = true,
                BorderStyle = BorderStyle.None,
                Margin = new Padding(4, 0, 4, 4)
            };
            var lblPerfSep = new Label
            {
                Text = "Performance", Dock = DockStyle.Top, Height = 22,
                ForeColor = System.Drawing.Color.Gray,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(8, 6, 2, 0),
                TextAlign = System.Drawing.ContentAlignment.BottomLeft
            };
            var pnlTips = new Panel { Dock = DockStyle.Bottom, Height = 110 };
            pnlTips.Controls.Add(this.rtbTips);
            pnlTips.Controls.Add(lblPerfSep);

            this.tabModel.Controls.Add(Scrollable(tlpModel));
            this.tabModel.Controls.Add(pnlTips);

            // ── SERVER TAB ────────────────────────────────────────────────
            var tlpServer = MakeTlp(3);
            AddRow(tlpServer, 0, MakeLbl("Host  (--host)"),   this.txtHost,  "IP address to listen on (default: 127.0.0.1).\n127.0.0.1 = local only.\n0.0.0.0 = accept from any interface. (--host)");
            AddRow(tlpServer, 1, MakeLbl("Port  (--port)"),   this.nudPort,  "TCP port for the HTTP API. Default: 8080. (--port)");
            AddRow(tlpServer, 2, MakeLbl("Tools  (--tools)"), this.txtTools, "Built-in tools available to AI agents.\nUse 'all' to enable everything, or a comma-separated list.\nLeave blank to disable. (--tools)");
            this.tabServer.Controls.Add(Scrollable(tlpServer));

            // ── SAMPLING TAB ────────────────────────────────────────────
            var tlpSampling = MakeTlp(6);
            AddRow(tlpSampling, 0, MakeLbl("Temperature"),       this.nudTemperature,   "Controls randomness of token selection (default: 0.80).\n0 = greedy / deterministic.\nHigher = more creative but less coherent. (--temperature)");
            AddRow(tlpSampling, 1, MakeLbl("Top-K  (0 = disabled)"),   this.nudTopK, "Top-K sampling (default: 40, 0 = disabled).\nOnly sample from the K most likely tokens. (--top-k)");
            AddRow(tlpSampling, 2, MakeLbl("Top-P  (1.0 = disabled)"), this.nudTopP, "Nucleus sampling (default: 0.95, 1.0 = disabled).\nOnly consider tokens within the top cumulative probability. (--top-p)");
            AddRow(tlpSampling, 3, MakeLbl("Min-P  (0.0 = disabled)"), this.nudMinP, "Min-P sampling (default: 0.05, 0.0 = disabled).\nMinimum probability relative to the top token. (--min-p)");
            var pnlSeed = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlSeed.Controls.Add(this.chkSeedRandom);
            pnlSeed.Controls.Add(this.nudSeed);
            this.chkSeedRandom.CheckedChanged += (_, _) => this.nudSeed.Visible = !this.chkSeedRandom.Checked;
            AddRow(tlpSampling, 4, MakeLbl("Seed  (-s)"), pnlSeed, "RNG seed for reproducible outputs.\nRandom = different output each run.\nFixed seed = same output for the same prompt. (-s)");
            AddRow(tlpSampling, 5, MakeLbl("Repeat Penalty"),    this.nudRepeatPenalty, "Penalize repeat sequence of tokens (default: 1.00, 1.0 = disabled).\nHigher values reduce repetition. (--repeat-penalty)");
            this.tabSampling.Controls.Add(Scrollable(tlpSampling));

            // ── ADVANCED TAB ────────────────────────────────────────────
            var tlpExeRow = MakeRow2(this.txtExePath, this.btnBrowseExe, 34);
            var lblExtra  = new Label { Text = "Extra Arguments:", AutoSize = true, Margin = new Padding(0, 4, 0, 2) };
            var tlpAdv = MakeTlp(8);
            AddRow(tlpAdv, 0, MakeLbl("API Key  (--api-key)"), this.txtApiKey, "Require this Bearer token in the Authorization header.\nLeave blank to disable authentication. (--api-key)");
            AddChk(tlpAdv, 1, this.chkEmbedding, "Restrict the server to the /embeddings endpoint.\nDisables chat completion endpoints. (--embedding)");
            AddChk(tlpAdv, 2, this.chkRerank,    "Enable the /rerank endpoint for cross-encoder\ndocument ranking tasks. (--rerank)");
            AddChk(tlpAdv, 3, this.chkMetrics,   "Expose a Prometheus-compatible /metrics endpoint\nfor monitoring and alerting. (--metrics)");
            AddRow(tlpAdv, 4, MakeLbl("Server EXE"), tlpExeRow,          "Path to the llama-server executable.\nAuto-detected from PATH on first launch.");
            Span3(tlpAdv, lblExtra, 0, 5);
            Span3(tlpAdv, this.txtExtraArgs, 0, 6);
            this.tabAdvanced.Controls.Add(Scrollable(tlpAdv));

            // ── LOG TAB ──────────────────────────────────────────────────
            this.tabLog = new TabPage { Text = "Log" };
            this.rtbLog = new RichTextBox
            {
                Dock = DockStyle.Fill,
                ReadOnly = true,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 8.5F),
                ScrollBars = RichTextBoxScrollBars.Vertical,
                WordWrap = false
            };
            this.btnClearLog = new Button { Text = "Delete Log", Dock = DockStyle.Bottom, Height = 34 };
            this.btnClearLog.Click += (_, _) => { this.rtbLog.Clear(); try { System.IO.File.Delete(_logFilePath); } catch { } _logViewRenderedBytes = -1; };
            this.tabLog.Controls.Add(this.rtbLog);
            this.tabLog.Controls.Add(this.btnClearLog);

            // ── PERF TAB ──────────────────────────────────────────────────
            this.tabPerf = new TabPage { Text = "Perf" };

            this.treePerf = new TreeView
            {
                Dock = DockStyle.Fill,
                BackColor = Color.FromArgb(20, 20, 20),
                ForeColor = Color.LightGray,
                Font = new Font("Consolas", 8.5F),
                BorderStyle = BorderStyle.None,
                LineColor = Color.FromArgb(60, 60, 60),
                ShowLines = true,
                ShowPlusMinus = true,
                ShowRootLines = true,
                HideSelection = false,
                FullRowSelect = true,
                ItemHeight = 22,
                DrawMode = TreeViewDrawMode.OwnerDrawText
            };
            this.treePerf.DrawNode += (_, e) =>
            {
                if (e.Node == null) { e.DrawDefault = true; return; }

                bool selected = (e.State & TreeNodeStates.Selected) != 0;
                var  font     = e.Node.NodeFont ?? this.treePerf.Font;

                // ── Config nodes (level 1): main text | current-settings marker | load hint ──
                if (e.Node.Level == 1)
                {
                    using var bgBrush = new SolidBrush(selected ? Color.FromArgb(40, 70, 40) : this.treePerf.BackColor);
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);

                    const string currentMarker = "  <- current settings";
                    string nodeText   = e.Node.Text;
                    bool hasCurrent   = nodeText.EndsWith(currentMarker);
                    string mainText   = hasCurrent ? nodeText[..^currentMarker.Length] : nodeText;
                    var    mainColor  = selected ? Color.White : e.Node.ForeColor;

                    int cx = e.Bounds.Left;

                    int mainW = TextRenderer.MeasureText(e.Graphics, mainText, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    TextRenderer.DrawText(e.Graphics, mainText, font,
                        new Rectangle(cx, e.Bounds.Top, mainW, e.Bounds.Height),
                        mainColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    cx += mainW;

                    if (hasCurrent)
                    {
                        int markerW = TextRenderer.MeasureText(e.Graphics, currentMarker, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                        TextRenderer.DrawText(e.Graphics, currentMarker, font,
                            new Rectangle(cx, e.Bounds.Top, markerW, e.Bounds.Height),
                            Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                        cx += markerW;
                    }

                    const string loadHint = "  [load settings]";
                    int hintW = TextRenderer.MeasureText(e.Graphics, loadHint, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    TextRenderer.DrawText(e.Graphics, loadHint, font,
                        new Rectangle(cx, e.Bounds.Top, hintW, e.Bounds.Height),
                        Color.FromArgb(80, 160, 220), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    return;
                }

                // ── Model nodes: name (green) | t/s (yellow) | active marker (white) ──
                const string loadedMarker = "  <- currently loaded model";
                string text      = e.Node.Text;
                bool hasLoaded   = text.EndsWith(loadedMarker);
                string baseText  = hasLoaded ? text[..^loadedMarker.Length] : text;
                int    sep       = baseText.LastIndexOf("  (");
                bool   hasTps    = sep >= 0 && baseText.EndsWith(" t/s)");

                if (!hasTps && !hasLoaded) { e.DrawDefault = true; return; }

                using var bgBrush2 = new SolidBrush(selected ? Color.FromArgb(40, 70, 40) : this.treePerf.BackColor);
                e.Graphics.FillRectangle(bgBrush2, e.Bounds);

                string namePart  = hasTps ? baseText[..sep] : baseText;
                string tpsPart   = hasTps ? baseText[sep..] : "";
                var    nameColor = selected ? Color.White : e.Node.ForeColor;
                var    tpsColor  = selected ? Color.LightYellow : Color.FromArgb(255, 200, 0);

                int x = e.Bounds.Left;

                int nameW = TextRenderer.MeasureText(e.Graphics, namePart, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                TextRenderer.DrawText(e.Graphics, namePart, font,
                    new Rectangle(x, e.Bounds.Top, nameW, e.Bounds.Height),
                    nameColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                x += nameW;

                if (hasTps)
                {
                    int tpsW = TextRenderer.MeasureText(e.Graphics, tpsPart, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    TextRenderer.DrawText(e.Graphics, tpsPart, font,
                        new Rectangle(x, e.Bounds.Top, tpsW, e.Bounds.Height),
                        tpsColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    x += tpsW;
                }

                if (hasLoaded)
                {
                    TextRenderer.DrawText(e.Graphics, loadedMarker, font,
                        new Rectangle(x, e.Bounds.Top, e.Bounds.Width - (x - e.Bounds.Left), e.Bounds.Height),
                        Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                }
            };
            this.treePerf.NodeMouseClick += (_, e) =>
            {
                if (e.Node?.Level != 1) return;
                using var g = this.treePerf.CreateGraphics();
                var font  = e.Node.NodeFont ?? this.treePerf.Font;
                int mainW = TextRenderer.MeasureText(g, e.Node.Text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                if (e.X >= e.Node.Bounds.Left + mainW)
                {
                    this.treePerf.SelectedNode = e.Node;
                    LoadSelectedPerfConfig();
                }
            };
            this.treePerf.MouseMove += (_, e) =>
            {
                var node = this.treePerf.GetNodeAt(e.X, e.Y);
                bool hand = false;
                if (node?.Level == 1)
                {
                    using var g = this.treePerf.CreateGraphics();
                    var font  = node.NodeFont ?? this.treePerf.Font;
                    int mainW = TextRenderer.MeasureText(g, node.Text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    hand = e.X >= node.Bounds.Left + mainW;
                }
                this.treePerf.Cursor = hand ? Cursors.Hand : Cursors.Default;
            };
            this.treePerf.MouseLeave += (_, _) => this.treePerf.Cursor = Cursors.Default;

            this.btnClearPerf = new Button { Text = "Clear Log", Dock = DockStyle.Right, Width = 90 };
            this.btnClearPerf.Click += btnClearPerf_Click;
            var pnlPerfBottom = new Panel { Dock = DockStyle.Bottom, Height = 34 };
            pnlPerfBottom.Controls.Add(this.btnClearPerf);

            this.tabPerf.Controls.Add(this.treePerf);
            this.tabPerf.Controls.Add(pnlPerfBottom);

            // ── TAB CONTROL ──────────────────────────────────────────────
            this.tabMain.TabPages.AddRange(new TabPage[] { tabModel, tabServer, tabSampling, tabAdvanced, tabLog, tabPerf });
            this.tabMain.Dock = DockStyle.Fill;
            this.tabMain.SelectedIndexChanged += (_, _) =>
            {
                if (tabMain.SelectedTab == tabPerf) RefreshPerfLog();
                if (tabMain.SelectedTab == tabLog)  RefreshLogView();
            };

            // ── MAIN TABLE LAYOUT ─────────────────────────────────────────
            this.tlpMain.Dock = DockStyle.Fill;
            this.tlpMain.Padding = new Padding(8);
            this.tlpMain.ColumnCount = 1;
            this.tlpMain.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            this.tlpMain.RowCount = 3;
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // tabs
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 52F));   // cmd preview
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));   // buttons + status

            // Status label on left, buttons on right — all in one row
            var tlpButtons = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0) };
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220F));
            tlpButtons.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 140F));
            tlpButtons.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpButtons.Controls.Add(this.lblStatus,   0, 0);
            tlpButtons.Controls.Add(this.btnLaunch,   1, 0);
            tlpButtons.Controls.Add(this.btnOpenChat, 2, 0);

            this.tlpMain.Controls.Add(this.tabMain,       0, 0);
            this.tlpMain.Controls.Add(this.txtCmdPreview, 0, 1);
            this.tlpMain.Controls.Add(tlpButtons,         0, 2);

            // ── Event handlers ────────────────────────────────────────────
            this.btnLaunch.Click    += btnLaunch_Click;
            this.btnOpenChat.Click  += btnOpenChat_Click;
            this.btnBrowse.Click    += btnBrowse_Click;
            this.btnBrowseExe.Click += btnBrowseExe_Click;

            void refreshPreview(object s, System.EventArgs e) => UpdateCommandPreview();
            foreach (var n in new NumericUpDown[] { nudGpuLayers, nudCtxSize, nudPort, nudThreads, nudParallel, nudBatchSize, nudUBatchSize, nudTemperature, nudTopK, nudTopP, nudMinP, nudSeed, nudRepeatPenalty })
                n.ValueChanged += refreshPreview;
            foreach (var c in new CheckBox[] { chkFlashAttn, chkContBatching, chkMmap, chkMlock, chkEmbedding, chkRerank, chkMetrics, chkThreadsAuto, chkCtxDefault, chkSeedRandom })
                c.CheckedChanged += refreshPreview;
            foreach (var c in new ComboBox[] { cbModel, cbCacheK, cbCacheV, cbReasoning, cbNglMode })
                c.SelectedIndexChanged += refreshPreview;
            foreach (var t in new TextBox[] { txtHost, txtTools, txtApiKey, txtExtraArgs, txtExePath })
                t.TextChanged += refreshPreview;

            // ── Form ──────────────────────────────────────────────────────
            this.ClientSize   = new Size(1200, 1024);
            this.MinimumSize  = new Size(1000, 720);
            this.Controls.Add(this.tlpMain);
            this.FormBorderStyle = FormBorderStyle.Sizable;
            this.StartPosition   = FormStartPosition.CenterScreen;
            this.Text = "Llama-Server Launcher";
            var iconStream = typeof(MainForm).Assembly.GetManifestResourceStream("LlamaServerLauncher.app.ico");
            if (iconStream != null) this.Icon = new System.Drawing.Icon(iconStream);
        }

        // ── Layout helpers ────────────────────────────────────────────────

        private static Label MakeLbl(string text) => new Label
        {
            Text = text, Dock = DockStyle.Fill, AutoSize = false,
            TextAlign = System.Drawing.ContentAlignment.MiddleLeft
        };

        // 3-column TLP: [label 230px | icon 22px | control fill]
        private static TableLayoutPanel MakeTlp(int rows)
        {
            var tlp = new TableLayoutPanel
            {
                AutoSize = true, ColumnCount = 3,
                Dock = DockStyle.Top,
                Padding = new Padding(4, 4, 4, 4)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.RowCount = rows;
            for (int i = 0; i < rows; i++)
                tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            return tlp;
        }

        private void AddRow(TableLayoutPanel tlp, int row, Label lbl, Control ctrl, string tooltip)
        {
            tlp.Controls.Add(lbl,              0, row);
            tlp.Controls.Add(MakeInfo(tooltip), 1, row);
            tlp.Controls.Add(ctrl,              2, row);
        }

        private void AddChk(TableLayoutPanel tlp, int row, CheckBox chk, string tooltip)
        {
            tlp.Controls.Add(chk,              0, row);
            tlp.Controls.Add(MakeInfo(tooltip), 1, row);
        }

        private static void Span3(TableLayoutPanel tlp, Control c, int col, int row)
        {
            tlp.Controls.Add(c, col, row);
            tlp.SetColumnSpan(c, 3);
        }

        private static Panel Scrollable(TableLayoutPanel inner)
        {
            var p = new Panel { Dock = DockStyle.Fill, AutoScroll = true };
            p.Controls.Add(inner);
            return p;
        }

        private static TableLayoutPanel MakeRow2(Control left, Control right, int rightWidth)
        {
            var tlp = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 2,
                RowCount = 1, Margin = new Padding(0), AutoSize = true
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, rightWidth));
            tlp.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlp.Controls.Add(left,  0, 0);
            tlp.Controls.Add(right, 1, 0);
            return tlp;
        }

        private Label MakeInfo(string tooltip)
        {
            var lbl = new Label
            {
                Text = "ⓘ",
                Dock = DockStyle.Fill,
                AutoSize = false,
                TextAlign = System.Drawing.ContentAlignment.MiddleCenter,
                ForeColor = System.Drawing.Color.SteelBlue,
                Cursor = Cursors.Help
            };
            _tip.SetToolTip(lbl, tooltip);
            return lbl;
        }

        #endregion
    }
}
