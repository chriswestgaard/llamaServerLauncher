using System.Drawing;
using System.Windows.Forms;

namespace LlamaServerLauncher
{
    partial class MainForm
    {
        private System.ComponentModel.IContainer components = null;

        // Tabs
        private DarkTabControl tabMain;
        private TabPage tabModel, tabServer, tabSampling, tabAdvanced, tabLog, tabPerf;

        // Log tab
        private RichTextBox rtbLog;
        private Button btnClearLog;

        // Perf tab
        private TreeView treePerf;

        // Model tab
        private RichTextBox rtbTips;

        private Button btnBrowse;
        private ComboBox cbModel;
        private CheckBox chkMmproj;
        private TextBox txtMmprojPath;
        private Button btnBrowseMmproj;
        private NumericUpDown nudGpuLayers;
        private CheckBox chkNglAuto;
        private Label lblLayerCount, lblCtxSize, lblCtxPerSlot;
        private NumericUpDown nudCtxSize;
        private CheckBox chkThreadsAuto, chkCtxDefault, chkSeedRandom;
        private TextBox txtCpuInfo, txtGpuInfo;
        private UsageGraph graphCpu, graphRam, graphGpu, graphVram, graphCtx;

        // Server tab
        private RadioButton rdoHostLocal, rdoHostAll, rdoHostCustom;
        private TextBox txtHostCustom, txtTools;
        private NumericUpDown nudPort, nudThreads, nudParallel, nudBatchSize, nudUBatchSize;
        private CheckBox chkFlashAttn, chkContBatching, chkJinja;

        // Cache tab
        private ComboBox cbCacheK, cbCacheV, cbReasoning, cbSplitMode;
        private CheckBox chkMmap, chkMlock, chkKvOffload, chkContextShift;
        private NumericUpDown nudDefragThold;
        private CheckBox chkThreadsBatchAuto;
        private NumericUpDown nudThreadsBatch;
        private NumericUpDown nudCacheReuse;

        // Sampling tab
        private NumericUpDown nudTemperature, nudTopK, nudTopP, nudMinP, nudSeed, nudRepeatPenalty;

        // Advanced tab
        private TextBox txtApiKey, txtExePath, txtExtraArgs;
        private Button btnBrowseExe;
        private CheckBox chkEmbedding, chkRerank, chkMetrics;

        // Model tab buttons (top right)
        private Button btnResetDefaults, btnOpenChat, btnDarkMode;

       // Bottom chrome
        private TableLayoutPanel tlpMain;
        private TextBox txtCmdPreview;
        private Button btnLaunch;
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
            this.tabMain        = new DarkTabControl();
            this.tabModel       = new TabPage { Text = "Model"    };
            this.tabServer      = new TabPage { Text = "Server"   };
            this.tabSampling    = new TabPage { Text = "Sampling" };
            this.tabAdvanced    = new TabPage { Text = "Advanced" };


            this.btnBrowse        = new Button    { Text = "…", Dock = DockStyle.Fill, MinimumSize = new Size(0, 28), Margin = new Padding(4, 0, 0, 0) };
            this.chkMmproj        = new CheckBox { Text = "", AutoSize = true, Margin = new Padding(3, 0, 4, 0), Anchor = AnchorStyles.None };
            this.txtMmprojPath    = new TextBox  { Dock = DockStyle.Fill, ReadOnly = true, Enabled = false };
            this.btnBrowseMmproj  = new Button   { Text = "…", Dock = DockStyle.Fill, Enabled = false, MinimumSize = new Size(0, 28), Margin = new Padding(4, 0, 0, 0) };
            this.cbModel         = new ComboBox  { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.chkNglAuto    = new CheckBox { Text = "Auto", AutoSize = true, Checked = true, Margin = new Padding(0, 5, 6, 0) };
            this.nudGpuLayers  = new NumericUpDown { Minimum = 0, Maximum = 200, Value = 200, Dock = DockStyle.Fill, Enabled = false };
this.lblLayerCount = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DimGray, Margin = new Padding(0, 5, 0, 0), Text = "" };
            this.chkCtxDefault   = new CheckBox { Text = "Model default", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 0, 0) };
            this.lblCtxSize      = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DimGray, Margin = new Padding(0, 4, 0, 0), Text = "" };
            this.lblCtxPerSlot   = new Label { AutoSize = true, ForeColor = System.Drawing.Color.DimGray, Margin = new Padding(6, 4, 0, 0), Text = "" };
            this.nudCtxSize      = new NumericUpDown { Minimum = 512, Maximum = 10000000, Value = 4096, Increment = 1024, Dock = DockStyle.Fill, Visible = false, TextAlign = HorizontalAlignment.Right, Margin = new Padding(0, 0, 0, 4) };
            this.txtCpuInfo      = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = System.Drawing.SystemColors.Control, ForeColor = System.Drawing.Color.DimGray, TabStop = false };
            this.txtGpuInfo      = new TextBox { ReadOnly = true, Dock = DockStyle.Fill, BorderStyle = BorderStyle.None, BackColor = System.Drawing.SystemColors.Control, ForeColor = System.Drawing.Color.DimGray, TabStop = false };
            this.graphCpu  = new UsageGraph { Dock = DockStyle.Fill, Title = "CPU",     GraphColor = Color.FromArgb( 19, 194,  56), Margin = new Padding(0, 2, 2, 0) };
            this.graphRam  = new UsageGraph { Dock = DockStyle.Fill, Title = "RAM",     GraphColor = Color.FromArgb(  0, 183, 235), Margin = new Padding(2, 2, 0, 0) };
            this.graphGpu  = new UsageGraph { Dock = DockStyle.Fill, Title = "GPU",     GraphColor = Color.FromArgb(255, 183,   0), Margin = new Padding(0, 2, 2, 0) };
            this.graphVram = new UsageGraph { Dock = DockStyle.Fill, Title = "VRAM",    GraphColor = Color.FromArgb(255, 100,   0), Margin = new Padding(2, 2, 0, 0) };
            this.graphCtx  = new UsageGraph { Dock = DockStyle.Fill, Title = "Context", GraphColor = Color.FromArgb(180, 100, 255), Margin = new Padding(0, 2, 0,  0) };

            this.rdoHostLocal  = new RadioButton { Text = "Local only",       AutoSize = true, Checked = true, Margin = new Padding(0, 2, 14, 0) };
            this.rdoHostAll    = new RadioButton { Text = "All",         AutoSize = true, Margin = new Padding(0, 2, 14, 0) };
            this.rdoHostCustom = new RadioButton { Text = "Custom",          AutoSize = true, Margin = new Padding(0, 2, 6,  0) };
            this.txtHostCustom = new TextBox     { Width = 140, Visible = false, Margin = new Padding(0, 1, 0, 0) };
            this.nudPort         = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 65535, Value = 8080  };
            this.chkThreadsAuto  = new CheckBox { Text = "Auto", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 8, 0) };
            this.nudThreads      = new NumericUpDown { Minimum = 1, Maximum = 256, Value = 4, Width = 60, Visible = false };
            this.nudParallel     = new NumericUpDown { Width = 60, Minimum = 1,  Maximum = 128,   Value = 1    };
            this.nudBatchSize    = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 65536, Value = 2048 };
            this.nudUBatchSize   = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 1,  Maximum = 65536, Value = 512  };
            this.chkFlashAttn    = new CheckBox { Text = "Flash Attention",    AutoSize = true, Checked = false };
            this.chkContBatching = new CheckBox { Text = "Continuous Batching", AutoSize = true, Checked = false };
            this.chkJinja        = new CheckBox { Text = "Jinja Templates",     AutoSize = true, Checked = true };
            this.txtTools        = new TextBox  { Dock = DockStyle.Fill, Text = "" };

            this.cbCacheK    = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbCacheV    = new ComboBox { Dock = DockStyle.Fill, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbReasoning = new ComboBox { Width = 120, DropDownStyle = ComboBoxStyle.DropDownList };
            this.chkMmap     = new CheckBox { Text = "Memory-map file  (mmap)",  AutoSize = true, Checked = true };
            this.chkMlock    = new CheckBox { Text = "Lock model in RAM  (mlock)", AutoSize = true };

            this.cbSplitMode       = new ComboBox { Width = 110, DropDownStyle = ComboBoxStyle.DropDownList };
            this.cbSplitMode.Items.AddRange(new[] { "layer", "none", "row", "tensor" });
            this.cbSplitMode.SelectedIndex = 0;
            this.chkKvOffload      = new CheckBox { Text = "KV Offload", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 0, 0) };
            this.nudDefragThold    = new NumericUpDown { Dock = DockStyle.Fill, Minimum = -1M, Maximum = 1M, Value = -1M, DecimalPlaces = 2, Increment = 0.05M };
            this.chkThreadsBatchAuto = new CheckBox { Text = "Auto", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 8, 0) };
            this.nudThreadsBatch   = new NumericUpDown { Minimum = 1, Maximum = 256, Value = 4, Width = 60, Visible = false };
            this.chkContextShift   = new CheckBox { Text = "Context Shift", AutoSize = true, Checked = true };
            this.nudCacheReuse     = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0, Maximum = 32768, Value = 0 };

            this.nudTemperature   = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 5M,          Value = 0.80M, DecimalPlaces = 2, Increment = 0.05M };
            this.nudTopK          = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0,  Maximum = 10000,        Value = 40    };
            this.nudTopP          = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 1M,           Value = 0.95M, DecimalPlaces = 2, Increment = 0.01M };
            this.nudMinP          = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 1M,           Value = 0.05M, DecimalPlaces = 2, Increment = 0.01M };
            this.chkSeedRandom    = new CheckBox { Text = "Random", AutoSize = true, Checked = true, Margin = new Padding(0, 2, 8, 0) };
            this.nudSeed          = new NumericUpDown { Minimum = 0, Maximum = int.MaxValue, Value = 0, Width = 100, Visible = false };
            this.nudRepeatPenalty = new NumericUpDown { Dock = DockStyle.Fill, Minimum = 0M, Maximum = 3M,           Value = 1.10M, DecimalPlaces = 2, Increment = 0.05M };

            this.txtApiKey    = new TextBox { Dock = DockStyle.Fill };
            this.txtExePath   = new TextBox { Dock = DockStyle.Fill };
            this.txtExtraArgs = new TextBox { Dock = DockStyle.Fill, Multiline = true, Height = 56, ScrollBars = ScrollBars.Vertical };
            this.btnBrowseExe = new Button  { Text = "…", Dock = DockStyle.Fill, MinimumSize = new Size(0, 28), Margin = new Padding(4, 0, 0, 0) };
            this.chkEmbedding = new CheckBox { Text = "Embedding Mode",    AutoSize = true };
            this.chkRerank    = new CheckBox { Text = "Reranking Mode",   AutoSize = true };
            this.chkMetrics   = new CheckBox { Text = "Prometheus Metrics", AutoSize = true };
            this.btnResetDefaults = new Button
            {
                Text = "",
                Size = new Size(44, 44),
                Margin = new Padding(0, 4, 4, 4),
                Font = new Font("Segoe MDL2 Assets", this.Font.Size + 4F, FontStyle.Regular),
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.FromArgb(180, 110, 30),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(0)
            };
            _tip.SetToolTip(this.btnResetDefaults, "Reset all parameters to default values");
            this.btnDarkMode = new Button
            {
                Text = "",
                Size = new Size(44, 44),
                Margin = new Padding(0, 4, 4, 4),
                Font = new Font("Segoe MDL2 Assets", this.Font.Size + 4F, FontStyle.Regular),
                FlatStyle = FlatStyle.Standard,
                BackColor = Color.FromArgb(50, 70, 90),
                ForeColor = Color.White,
                UseVisualStyleBackColor = false,
                TextAlign = ContentAlignment.MiddleCenter,
                Padding = new Padding(2, 0, 0, 0)
            };
            _tip.SetToolTip(this.btnDarkMode, "Toggle dark/light theme");
            this.btnOpenChat      = new Button { Text = "Open Chat UI", Dock = DockStyle.Fill, Margin = new Padding(0, 4, 0, 4), Font = new Font(this.Font, FontStyle.Bold), Enabled = false };

            this.tlpMain       = new TableLayoutPanel();
            this.txtCmdPreview = new TextBox { ReadOnly = true, Multiline = true, WordWrap = true, ScrollBars = ScrollBars.Vertical, Dock = DockStyle.Fill, Font = new Font("Consolas", 7.5F), BackColor = System.Drawing.SystemColors.ControlLight };
            this.btnLaunch     = new Button  { Text = "Launch llama-server", Dock = DockStyle.Fill, Margin = new Padding(8, 4, 0, 4), Font = new Font(this.Font, FontStyle.Bold) };
            this.lblStatus     = new Label   { AutoSize = false, Dock = DockStyle.Fill, Text = "", TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(4, 0, 0, 0) };

            // ── ComboBox items ──────────────────────────────────────────────
            var cacheTypes = new[] { "f16", "f32", "bf16", "q8_0", "q4_0", "q4_1", "iq4_nl", "q5_0", "q5_1", "turbo3", "turbo4"  };
            this.cbCacheK.Items.AddRange(cacheTypes);   this.cbCacheK.SelectedIndex = 0;
            this.cbCacheV.Items.AddRange(cacheTypes);   this.cbCacheV.SelectedIndex = 0;
            this.cbReasoning.Items.AddRange(new[] { "auto", "on", "off" });
            this.cbReasoning.SelectedIndex = 0;

            // ── MODEL TAB ──────────────────────────────────────────────────
            var tlpModelRow = MakeRow2(this.cbModel, this.btnBrowse, 34);
            var tlpModel = MakeTlp(10);

            tlpModel.RowStyles[4] = new RowStyle(SizeType.Absolute, 160F);  // HW side-by-side panel
            tlpModel.RowStyles[5] = new RowStyle(SizeType.Absolute, 130F);  // Context graph
            tlpModel.RowStyles[6] = new RowStyle(SizeType.Absolute, 36F);   // Perf Observations label
            tlpModel.RowStyles[7] = new RowStyle(SizeType.Absolute, 110F);  // Perf Observations box

            // ── Top header: 2 columns [Model File | Image Input] on left, [Reset | Open Chat] on right ──
            var tlpTopHeader = new TableLayoutPanel
            {
                Dock = DockStyle.Top, ColumnCount = 1, RowCount = 2,
                Margin = new Padding(0), AutoSize = true, Padding = new Padding(4, 4, 4, 4)
            };
            tlpTopHeader.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpTopHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpTopHeader.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            // Left column: Model File and Image Input
            var tlpLeft = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 2,
                Margin = new Padding(0, 0, 4, 0), AutoSize = true, Padding = new Padding(0, 0, 4, 0)
            };
            tlpLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 230F));
            tlpLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpLeft.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpLeft.RowStyles.Add(new RowStyle(SizeType.AutoSize));

            AddRow(tlpLeft, 0, MakeLbl("Model File"), tlpModelRow, "The .gguf model file to load and serve.");
            var tlpMmprojRow = new TableLayoutPanel
            {
                Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1,
                Margin = new Padding(0), AutoSize = true
            };
            tlpMmprojRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 30F));
            tlpMmprojRow.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpMmprojRow.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 34F));
            tlpMmprojRow.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpMmprojRow.Controls.Add(this.chkMmproj,       0, 0);
            tlpMmprojRow.Controls.Add(this.txtMmprojPath,   1, 0);
            tlpMmprojRow.Controls.Add(this.btnBrowseMmproj, 2, 0);
            this.chkMmproj.CheckedChanged += (_, _) =>
            {
                this.txtMmprojPath.Enabled   = this.chkMmproj.Checked;
                this.btnBrowseMmproj.Enabled = this.chkMmproj.Checked;
            };
            AddRow(tlpLeft, 1, MakeLbl("Image Input  (--mmproj)"), tlpMmprojRow, "Load a multimodal projector (.gguf) to enable image/vision input.\nRequired for vision-capable models such as LLaVA or Qwen-VL.\n(--mmproj)");

            tlpTopHeader.Controls.Add(tlpLeft, 0, 0);
            Span3(tlpModel, tlpTopHeader, 0, 0);

            // ── GroupBox: GPU & Threading (left half, row 2) ─────────────
            var tlpGpuThreading = MakeTlp(5, 185);

            var tlpNgl = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 3, RowCount = 1, Margin = new Padding(0), AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, MinimumSize = new Size(0, nudGpuLayers.PreferredSize.Height + nudGpuLayers.Margin.Vertical) };
            tlpNgl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpNgl.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));
            tlpNgl.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpNgl.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpNgl.Controls.Add(this.chkNglAuto,    0, 0);
            tlpNgl.Controls.Add(this.nudGpuLayers,  1, 0);
            tlpNgl.Controls.Add(this.lblLayerCount, 2, 0);
            AddRow(tlpGpuThreading, 0, MakeLbl("GPU Layers  (-ngl)"), tlpNgl, "Number of model layers to offload to GPU VRAM.\n0 = CPU only. Max value = GPU only (-ngl 999).\nAuto = server decides at startup. (-ngl)");
            AddRow(tlpGpuThreading, 1, MakeLbl("Split Mode  (-sm)"),  this.cbSplitMode, "Multi-GPU tensor split strategy (default: layer).\nlayer  = split by layers across GPUs.\nnone   = single GPU only.\nrow    = split by matrix rows.\ntensor = split by tensor dimensions. (-sm)");

            var pnlThreads = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, MinimumSize = new Size(0, nudThreads.PreferredSize.Height + nudThreads.Margin.Vertical) };
            pnlThreads.Controls.Add(this.chkThreadsAuto);
            pnlThreads.Controls.Add(this.nudThreads);
            this.chkThreadsAuto.CheckedChanged += (_, _) => this.nudThreads.Visible = !this.chkThreadsAuto.Checked;
            AddRow(tlpGpuThreading, 2, MakeLbl("Threads  (-t)"), pnlThreads, "CPU threads used during generation.\nAuto = server detects from CPU core count.\nFor best results, match to physical core count. (-t)");

            var pnlThreadsBatch = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, MinimumSize = new Size(0, nudThreadsBatch.PreferredSize.Height + nudThreadsBatch.Margin.Vertical) };
            pnlThreadsBatch.Controls.Add(this.chkThreadsBatchAuto);
            pnlThreadsBatch.Controls.Add(this.nudThreadsBatch);
            this.chkThreadsBatchAuto.CheckedChanged += (_, _) => this.nudThreadsBatch.Visible = !this.chkThreadsBatchAuto.Checked;
            AddRow(tlpGpuThreading, 3, MakeLbl("Threads Batch  (-tb)"), pnlThreadsBatch, "CPU threads used during prompt processing / prefill.\nAuto = same as generation threads.\nSet higher than generation threads to speed up long prompts. (-tb)");
            var pnlParallel = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlParallel.Controls.Add(this.nudParallel);
            pnlParallel.Controls.Add(this.lblCtxPerSlot);
            AddRow(tlpGpuThreading, 4, MakeLbl("Parallel Slots  (-np)"), pnlParallel, "Number of parallel sequences to decode (default: 1).\nEach slot reserves additional KV-cache memory. (-np)");

            var grpGpuThreading = MakeGroup("GPU && Threading");
            grpGpuThreading.Controls.Add(tlpGpuThreading);

            // ── GroupBox: Context & Cache (right half, row 2) ────────────
            var tlpCtxCache = MakeTlp(7, 185);

            var flpCtx = new FlowLayoutPanel
            {
                Dock = DockStyle.Fill, AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink,
                Margin = new Padding(0), WrapContents = false
            };
            flpCtx.Controls.Add(this.chkCtxDefault);
            flpCtx.Controls.Add(this.lblCtxSize);
            flpCtx.Controls.Add(this.nudCtxSize);
            this.chkCtxDefault.CheckedChanged += (_, _) =>
            {
                this.lblCtxSize.Visible = this.chkCtxDefault.Checked;
                this.nudCtxSize.Visible = !this.chkCtxDefault.Checked;
            };
             AddRow(tlpCtxCache, 0, MakeLbl("Context Size  (-c)"),    flpCtx,             "Maximum number of tokens in the context window.\n\"Model default\" uses the value embedded in the model file.\nLarger contexts require more VRAM/RAM. (-c)");
            AddRow(tlpCtxCache, 1, MakeLbl("KV Offload"),            this.chkKvOffload,  "Offload KV cache to GPU VRAM (default: on).\nUncheck to keep KV cache in system RAM — useful when VRAM is tight.\n(-nkvo / --no-kv-offload)");
            AddRow(tlpCtxCache, 2, MakeLbl("Batch Size  (-b)"),      this.nudBatchSize,  "Logical maximum batch size (default: 2048).\nLarger values improve prompt-processing throughput. (-b)");
            AddRow(tlpCtxCache, 3, MakeLbl("UBatch Size  (-ub)"),    this.nudUBatchSize, "Physical maximum batch size (default: 512).\nSmaller values reduce peak VRAM usage. (-ub)");
            AddRow(tlpCtxCache, 4, MakeLbl("Cache Type K  (-ctk)"),  this.cbCacheK,      "KV cache data type for Keys (default: f16).\nAllowed: f32, f16, bf16, q8_0, q4_0, q4_1, iq4_nl, q5_0, q5_1.\nbf16 / q8_0 saves VRAM with minor quality loss. (-ctk)");
            AddRow(tlpCtxCache, 5, MakeLbl("Cache Type V  (-ctv)"),  this.cbCacheV,      "KV cache data type for Values (default: f16).\nSame options as Cache Type K. (-ctv)");
            AddRow(tlpCtxCache, 6, MakeLbl("Defrag Threshold (-dt)"), this.nudDefragThold, "KV cache defragmentation threshold (default: -1 = disabled).\n0.0–1.0 = trigger defrag when fragmentation exceeds this ratio.\nHelps with long sessions that cycle many slots. (-dt)");

            var grpCtxCache = MakeGroup("Context && Cache");
            grpCtxCache.Controls.Add(tlpCtxCache);

            // ── GroupBox: Features (right column) ────────────────────────
            // Reasoning labeled row at top, checkboxes flowing horizontally below
            var tlpReasoning = new TableLayoutPanel
            {
                AutoSize = true, Dock = DockStyle.Top, ColumnCount = 3, RowCount = 1,
                Padding = new Padding(4, 4, 4, 2), Margin = new Padding(0)
            };
            tlpReasoning.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 155F));
            tlpReasoning.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 22F));
            tlpReasoning.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
            tlpReasoning.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpReasoning.Controls.Add(MakeLbl("Reasoning  (-rea)"), 0, 0);
            tlpReasoning.Controls.Add(MakeInfo("Use reasoning/thinking in chat (default: auto).\n'auto' = detect from model template.\n'on'   = enable reasoning tokens.\n'off'  = disable reasoning. (-rea)"), 1, 0);
            tlpReasoning.Controls.Add(this.cbReasoning, 2, 0);

            var flwFeatureChecks = new FlowLayoutPanel
            {
                Dock = DockStyle.Top, AutoSize = true,
                WrapContents = false, FlowDirection = FlowDirection.TopDown,
                Padding = new Padding(4, 2, 4, 4)
            };
            flwFeatureChecks.Controls.Add(MakeCheckItem(this.chkFlashAttn,    "Flash Attention: faster inference and lower VRAM usage (default: auto).\nChecked = force on; unchecked = use default (auto).\nRequires a compatible model. (-fa)"));
            flwFeatureChecks.Controls.Add(MakeCheckItem(this.chkContBatching, "Process new requests without waiting for current ones to finish.\nImproves throughput under concurrent load. (-cb)"));
            flwFeatureChecks.Controls.Add(MakeCheckItem(this.chkMmap,         "Memory-map model for faster load (default: enabled).\nUncheck to fully load into RAM (--no-mmap). (--mmap)"));
            flwFeatureChecks.Controls.Add(MakeCheckItem(this.chkMlock,        "Force system to keep model in RAM rather than swapping.\nRequires sufficient free RAM. (--mlock)"));
            flwFeatureChecks.Controls.Add(MakeCheckItem(this.chkContextShift, "Shift context window when full instead of erroring (default: on).\nUncheck (--no-context-shift) to return an error when context is exhausted.\nLeave on for infinite generation / long conversations."));
            flwFeatureChecks.Controls.Add(MakeCheckItem(this.chkJinja,        "Use Jinja2 engine to parse the model's chat template (default: on).\nUncheck to disable (--no-jinja). Leave on unless you have template parsing issues. (--no-jinja)"));

            var grpFeatures = MakeGroup("Features");
            grpFeatures.Dock = DockStyle.Top;
            grpFeatures.Controls.Add(flwFeatureChecks);
            grpFeatures.Controls.Add(tlpReasoning); // added last → rendered on top (DockStyle.Top stacking)

            // ── Three-column GroupBox layout: [GPU & Threading | Context & Cache | Features]
            var tlpParamCols = new TableLayoutPanel
            {
                AutoSize = true, Dock = DockStyle.Top,
                ColumnCount = 3, RowCount = 1, Margin = new Padding(0),
                MinimumSize = new Size(0, 60)
            };
            tlpParamCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 35.00F));
            tlpParamCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 40.00F));
            tlpParamCols.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 25.00F));
            tlpParamCols.RowStyles.Add(new RowStyle(SizeType.AutoSize));
            tlpParamCols.Controls.Add(grpGpuThreading, 0, 0);
            tlpParamCols.Controls.Add(grpCtxCache,     1, 0);
            tlpParamCols.Controls.Add(grpFeatures,     2, 0);
            Span3(tlpModel, tlpParamCols, 0, 2);

            // ── Hardware heading (row 3) ──────────────────────────────────
            var lblHwSep = new Label { Text = "Hardware", AutoSize = false, Dock = DockStyle.Fill, ForeColor = System.Drawing.Color.Gray, Font = new Font(Font, FontStyle.Bold), Margin = new Padding(2, 6, 2, 6) };
            Span3(tlpModel, lblHwSep, 0, 3);

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
            Span3(tlpModel, tlpHwCols, 0, 4);

            // ── Context graph (row 5) ─────────────────────────────────────
            Span3(tlpModel, this.graphCtx, 0, 5);

            // ── Performance Observations (rows 6-7, straight under context graph)
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
                Text = "Performance Observations", AutoSize = false, Dock = DockStyle.Fill,
                ForeColor = System.Drawing.Color.Gray,
                Font = new Font(Font, FontStyle.Bold),
                Margin = new Padding(2, 6, 2, 6)
            };
            Span3(tlpModel, lblPerfSep,    0, 6);
            Span3(tlpModel, this.rtbTips,  0, 7);


            this.tabModel.Controls.Add(Scrollable(tlpModel));

            // ── SERVER TAB ────────────────────────────────────────────────
            var tlpServer = MakeTlp(4);
            var pnlHost = new FlowLayoutPanel { AutoSize = true, WrapContents = false };
            pnlHost.Controls.Add(this.rdoHostLocal);
            pnlHost.Controls.Add(this.rdoHostAll);
            pnlHost.Controls.Add(this.rdoHostCustom);
            pnlHost.Controls.Add(this.txtHostCustom);
            this.rdoHostCustom.CheckedChanged += (_, _) => this.txtHostCustom.Visible = this.rdoHostCustom.Checked;
            AddRow(tlpServer, 0, MakeLbl("Allow connect (--host)"), pnlHost, "IP address to listen on (default: 127.0.0.1).\n127.0.0.1 = local only.\n0.0.0.0 = accept from any interface. (--host)");
            AddRow(tlpServer, 1, MakeLbl("Port  (--port)"),   this.nudPort,  "TCP port for the HTTP API. Default: 8080. (--port)");
            AddRow(tlpServer, 2, MakeLbl("Tools  (--tools)"), this.txtTools, "Built-in tools available to AI agents.\nUse 'all' to enable everything, or a comma-separated list.\nLeave blank to disable. (--tools)");
            AddRow(tlpServer, 3, MakeLbl("Cache Reuse  (--cache-reuse)"), this.nudCacheReuse, "Minimum chunk size (tokens) for prompt cache reuse (default: 0 = disabled).\nHigher values skip reuse of small overlapping prefixes — reduces overhead.\nSet to 256–1024 for best cache hit rate with typical prompts. (--cache-reuse)");
            this.tabServer.Controls.Add(Scrollable(tlpServer));

            // ── SAMPLING TAB ────────────────────────────────────────────
            var tlpSampling = MakeTlp(6);
            AddRow(tlpSampling, 0, MakeLbl("Temperature"),       this.nudTemperature,   "Controls randomness of token selection (default: 0.80).\n0 = greedy / deterministic.\nHigher = more creative but less coherent. (--temperature)");
            AddRow(tlpSampling, 1, MakeLbl("Top-K  (0 = disabled)"),   this.nudTopK, "Top-K sampling (default: 40, 0 = disabled).\nOnly sample from the K most likely tokens. (--top-k)");
            AddRow(tlpSampling, 2, MakeLbl("Top-P  (1.0 = disabled)"), this.nudTopP, "Nucleus sampling (default: 0.95, 1.0 = disabled).\nOnly consider tokens within the top cumulative probability. (--top-p)");
            AddRow(tlpSampling, 3, MakeLbl("Min-P  (0.0 = disabled)"), this.nudMinP, "Min-P sampling (default: 0.05, 0.0 = disabled).\nMinimum probability relative to the top token. (--min-p)");
            var pnlSeed = new FlowLayoutPanel { AutoSize = true, AutoSizeMode = AutoSizeMode.GrowAndShrink, WrapContents = false, MinimumSize = new Size(0, nudSeed.PreferredSize.Height + nudSeed.Margin.Vertical) };
            pnlSeed.Controls.Add(this.chkSeedRandom);
            pnlSeed.Controls.Add(this.nudSeed);
            this.chkSeedRandom.CheckedChanged += (_, _) => this.nudSeed.Visible = !this.chkSeedRandom.Checked;
            AddRow(tlpSampling, 4, MakeLbl("Seed  (-s)"), pnlSeed, "RNG seed for reproducible outputs.\nRandom = different output each run.\nFixed seed = same output for the same prompt. (-s)");
            AddRow(tlpSampling, 5, MakeLbl("Repeat Penalty"),    this.nudRepeatPenalty, "Penalize repeat sequence of tokens (default: 1.10, 1.0 = disabled).\nHigher values reduce repetition. (--repeat-penalty)");
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
            this.btnClearLog.Click += (_, _) => ClearLog();
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

                // ── Config nodes (level 1): main text | parameters | marker ──
                if (e.Node.Level == 1)
                {
                    using var bgBrush = new SolidBrush(selected ? Color.FromArgb(40, 70, 40) : this.treePerf.BackColor);
                    e.Graphics.FillRectangle(bgBrush, e.Bounds);

                    var item = e.Node.Tag as PerfConfigItem;
                    bool hasCurrent = item?.IsCurrent ?? false;
                    string mainText = e.Node.Text; // star + tps + count
                    var    mainColor = selected ? Color.White : e.Node.ForeColor;

                    int cx = e.Bounds.Left;

                    // 1. Draw main text (star, tps, count)
                    int mainW = TextRenderer.MeasureText(e.Graphics, mainText, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                    TextRenderer.DrawText(e.Graphics, mainText, font,
                        new Rectangle(cx, e.Bounds.Top, mainW, e.Bounds.Height),
                        mainColor, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    cx += mainW;

                    // 2. Draw parameters (ArgsLabel)
                    if (item != null && !string.IsNullOrEmpty(item.ArgsLabel))
                    {
                        string argsText = $"  {item.ArgsLabel}";
                        int argsW = TextRenderer.MeasureText(e.Graphics, argsText, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                        TextRenderer.DrawText(e.Graphics, argsText, font,
                            new Rectangle(cx, e.Bounds.Top, argsW, e.Bounds.Height),
                            Color.FromArgb(150, 150, 150), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                        cx += argsW;
                    }

                    // 3. Draw marker
                    if (hasCurrent)
                    {
                        const string currentMarker = "  <- current settings";
                        int markerW = TextRenderer.MeasureText(e.Graphics, currentMarker, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                        TextRenderer.DrawText(e.Graphics, currentMarker, font,
                            new Rectangle(cx, e.Bounds.Top, markerW, e.Bounds.Height),
                            Color.White, TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
                    else
                    {
                        const string loadHint = "  [load settings]";
                        int hintW = TextRenderer.MeasureText(e.Graphics, loadHint, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                        TextRenderer.DrawText(e.Graphics, loadHint, font,
                            new Rectangle(cx, e.Bounds.Top, hintW, e.Bounds.Height),
                            Color.FromArgb(80, 160, 220), TextFormatFlags.Left | TextFormatFlags.VerticalCenter | TextFormatFlags.NoPadding);
                    }
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
                var item = e.Node.Tag as PerfConfigItem;
                if (item == null || item.IsCurrent) return;
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
                    var item = node.Tag as PerfConfigItem;
                    if (item != null && !item.IsCurrent)
                    {
                        using var g = this.treePerf.CreateGraphics();
                        var font  = node.NodeFont ?? this.treePerf.Font;
                        int mainW = TextRenderer.MeasureText(g, node.Text, font, Size.Empty, TextFormatFlags.NoPadding).Width;
                        hand = e.X >= node.Bounds.Left + mainW;
                    }
                }
                this.treePerf.Cursor = hand ? Cursors.Hand : Cursors.Default;
            };
            this.treePerf.MouseLeave += (_, _) => this.treePerf.Cursor = Cursors.Default;

            this.tabPerf.Controls.Add(this.treePerf);

            // ── TAB CONTROL ──────────────────────────────────────────────
            this.tabMain.TabPages.AddRange(new TabPage[] { tabModel, tabServer, tabSampling, tabAdvanced, tabLog, tabPerf });
            this.tabMain.Dock     = DockStyle.Fill;
            this.tabMain.DrawMode = TabDrawMode.OwnerDrawFixed;
            this.tabMain.SizeMode = TabSizeMode.Fixed;
            this.tabMain.ItemSize = new Size(92, 26);
            this.tabMain.DrawItem += tabMain_DrawItem;
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
            this.tlpMain.RowCount = 4;
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));   // tabs
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 20F));   // cmd preview label
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 72F));   // cmd preview
            this.tlpMain.RowStyles.Add(new RowStyle(SizeType.Absolute, 44F));   // status + launch

            // Status label on left, reset + dark mode toggle + Open Chat + Launch buttons on right — all in one row
            var tlpStatusBar = new TableLayoutPanel { Dock = DockStyle.Fill, ColumnCount = 5, RowCount = 1, Margin = new Padding(0) };
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100F));    // status fills
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));    // reset defaults
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 52F));    // dark mode toggle
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180F));   // open chat ui
            tlpStatusBar.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 240F));   // launch fixed width
            tlpStatusBar.RowStyles.Add(new RowStyle(SizeType.Percent, 100F));
            tlpStatusBar.Controls.Add(this.lblStatus,        0, 0);
            tlpStatusBar.Controls.Add(this.btnResetDefaults, 1, 0);
            tlpStatusBar.Controls.Add(this.btnDarkMode,      2, 0);
            tlpStatusBar.Controls.Add(this.btnOpenChat,      3, 0);
            tlpStatusBar.Controls.Add(this.btnLaunch,        4, 0);

            var lblCmdHint = new Label { Text = "Command preview — click to copy", Dock = DockStyle.Fill, Font = new Font(this.Font.FontFamily, 7F), ForeColor = System.Drawing.SystemColors.GrayText, TextAlign = System.Drawing.ContentAlignment.MiddleLeft, Padding = new Padding(2, 0, 0, 0) };
            this.tlpMain.Controls.Add(this.tabMain,       0, 0);
            this.tlpMain.Controls.Add(lblCmdHint,         0, 1);
            this.tlpMain.Controls.Add(this.txtCmdPreview, 0, 2);
            this.tlpMain.Controls.Add(tlpStatusBar,       0, 3);

            // ── Event handlers ────────────────────────────────────────────
            this.txtCmdPreview.Click   += txtCmdPreview_Click;
            this.btnLaunch.Click       += btnLaunch_Click;
            this.btnBrowse.Click       += btnBrowse_Click;
            this.btnBrowseExe.Click    += btnBrowseExe_Click;
            this.btnBrowseMmproj.Click += btnBrowseMmproj_Click;
            this.btnResetDefaults.Click += btnResetDefaults_Click;
            this.btnOpenChat.Click      += btnOpenChat_Click;
            this.btnOpenChat.Paint      += btnOpenChat_Paint;
            this.btnDarkMode.Click      += (_, _) => { ApplyTheme(!_isDark); SaveConfig(); };


            void refreshPreview(object s, System.EventArgs e) => UpdateCommandPreview();
            foreach (var n in new NumericUpDown[] { nudCtxSize, nudPort, nudThreads, nudThreadsBatch, nudParallel, nudBatchSize, nudUBatchSize, nudDefragThold, nudCacheReuse, nudTemperature, nudTopK, nudTopP, nudMinP, nudSeed, nudRepeatPenalty })
                n.ValueChanged += refreshPreview;
            foreach (var c in new CheckBox[] { chkFlashAttn, chkContBatching, chkJinja, chkMmap, chkMlock, chkEmbedding, chkRerank, chkMetrics, chkThreadsAuto, chkThreadsBatchAuto, chkCtxDefault, chkSeedRandom, chkMmproj, chkKvOffload, chkContextShift, chkNglAuto })
                c.CheckedChanged += refreshPreview;
            this.nudGpuLayers.ValueChanged += refreshPreview;
            foreach (var c in new ComboBox[] { cbModel, cbCacheK, cbCacheV, cbReasoning, cbSplitMode })
                c.SelectedIndexChanged += refreshPreview;
            foreach (var r in new RadioButton[] { rdoHostLocal, rdoHostAll, rdoHostCustom })
                r.CheckedChanged += refreshPreview;
            foreach (var t in new TextBox[] { txtHostCustom, txtTools, txtApiKey, txtExtraArgs, txtExePath })
                t.TextChanged += refreshPreview;

            void refreshCtxPerSlot(object s, System.EventArgs e) => UpdateCtxPerSlot();
            this.nudParallel.ValueChanged      += refreshCtxPerSlot;
            this.nudCtxSize.ValueChanged        += refreshCtxPerSlot;
            this.chkCtxDefault.CheckedChanged   += refreshCtxPerSlot;

            // ── Form ──────────────────────────────────────────────────────
            this.ClientSize   = new Size(1500, 1200);
            this.MinimumSize  = new Size(1280, 900);
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

        // 3-column TLP: [label labelW px | icon 22px | control fill]
        private static TableLayoutPanel MakeTlp(int rows, int labelW = 230)
        {
            var tlp = new TableLayoutPanel
            {
                AutoSize = true, ColumnCount = 3,
                Dock = DockStyle.Top,
                Padding = new Padding(4, 4, 4, 4)
            };
            tlp.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, (float)labelW));
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

        private static GroupBox MakeGroup(string title)
        {
            return new GroupBox
            {
                Text = title,
                Dock = DockStyle.Fill,
                AutoSize = true,
                Padding = new Padding(6, 10, 6, 6)
            };
        }

        private FlowLayoutPanel MakeCheckItem(CheckBox chk, string tooltip)
        {
            var info = MakeInfo(tooltip);
            info.AutoSize = false;
            info.Dock = DockStyle.None;
            info.Size = new Size(20, chk.Height > 0 ? chk.Height : 20);
            var pnl = new FlowLayoutPanel
            {
                AutoSize = true, WrapContents = false,
                Margin = new Padding(0, 2, 24, 2)
            };
            pnl.Controls.Add(chk);
            pnl.Controls.Add(info);
            return pnl;
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
