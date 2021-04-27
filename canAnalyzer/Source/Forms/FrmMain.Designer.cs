namespace canAnalyzer
{
    partial class FrmMain
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.tableLayoutData = new System.Windows.Forms.TableLayoutPanel();
            this.tabControl = new System.Windows.Forms.TabControl();
            this.menuStrip = new System.Windows.Forms.MenuStrip();
            this.fileToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.saveSessionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.loadSessionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.remotoToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.connectToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.settingsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.resetToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.toolsToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.screenshootToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.findAValueToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.compressionToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.maskToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.startToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pauseToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.clearToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.aboutToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.m_status = new System.Windows.Forms.StatusStrip();
            this.statusConnState = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSpeed = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusPort = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusSilentMode = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusAutosaveTrace = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusFwVer = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusMaskFilterEn = new System.Windows.Forms.ToolStripStatusLabel();
            this.statusMsgCnt = new System.Windows.Forms.ToolStripStatusLabel();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.statusErrors = new System.Windows.Forms.ToolStripStatusLabel();
            this.tableLayoutData.SuspendLayout();
            this.menuStrip.SuspendLayout();
            this.m_status.SuspendLayout();
            this.tableLayoutPanel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tableLayoutData
            // 
            this.tableLayoutData.ColumnCount = 2;
            this.tableLayoutData.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 55F));
            this.tableLayoutData.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 45F));
            this.tableLayoutData.Controls.Add(this.tabControl, 1, 0);
            this.tableLayoutData.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutData.Location = new System.Drawing.Point(0, 30);
            this.tableLayoutData.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutData.Name = "tableLayoutData";
            this.tableLayoutData.RowCount = 1;
            this.tableLayoutData.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutData.Size = new System.Drawing.Size(1182, 393);
            this.tableLayoutData.TabIndex = 1;
            // 
            // tabControl
            // 
            this.tabControl.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tabControl.Location = new System.Drawing.Point(650, 0);
            this.tabControl.Margin = new System.Windows.Forms.Padding(0);
            this.tabControl.Name = "tabControl";
            this.tabControl.SelectedIndex = 0;
            this.tabControl.Size = new System.Drawing.Size(532, 393);
            this.tabControl.TabIndex = 0;
            // 
            // menuStrip
            // 
            this.menuStrip.Dock = System.Windows.Forms.DockStyle.Fill;
            this.menuStrip.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menuStrip.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.fileToolStripMenuItem,
            this.remotoToolStripMenuItem,
            this.connectToolStripMenuItem,
            this.settingsToolStripMenuItem,
            this.resetToolStripMenuItem,
            this.toolsToolStripMenuItem,
            this.maskToolStripMenuItem,
            this.aboutToolStripMenuItem});
            this.menuStrip.Location = new System.Drawing.Point(0, 0);
            this.menuStrip.Name = "menuStrip";
            this.menuStrip.Size = new System.Drawing.Size(1182, 30);
            this.menuStrip.TabIndex = 3;
            this.menuStrip.Text = "menuStrip1";
            this.menuStrip.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.menuStrip1_ItemClicked);
            // 
            // fileToolStripMenuItem
            // 
            this.fileToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.saveSessionToolStripMenuItem,
            this.loadSessionToolStripMenuItem});
            this.fileToolStripMenuItem.Name = "fileToolStripMenuItem";
            this.fileToolStripMenuItem.Size = new System.Drawing.Size(44, 26);
            this.fileToolStripMenuItem.Text = "File";
            this.fileToolStripMenuItem.Visible = false;
            // 
            // saveSessionToolStripMenuItem
            // 
            this.saveSessionToolStripMenuItem.Name = "saveSessionToolStripMenuItem";
            this.saveSessionToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.saveSessionToolStripMenuItem.Text = "Save Session";
            this.saveSessionToolStripMenuItem.Click += new System.EventHandler(this.saveSessionToolStripMenuItem_Click);
            // 
            // loadSessionToolStripMenuItem
            // 
            this.loadSessionToolStripMenuItem.Name = "loadSessionToolStripMenuItem";
            this.loadSessionToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.loadSessionToolStripMenuItem.Text = "Load Session";
            this.loadSessionToolStripMenuItem.Click += new System.EventHandler(this.loadSessionToolStripMenuItem_Click);
            // 
            // remotoToolStripMenuItem
            // 
            this.remotoToolStripMenuItem.Name = "remotoToolStripMenuItem";
            this.remotoToolStripMenuItem.Size = new System.Drawing.Size(74, 26);
            this.remotoToolStripMenuItem.Text = "Remoto";
            this.remotoToolStripMenuItem.Visible = false;
            // 
            // connectToolStripMenuItem
            // 
            this.connectToolStripMenuItem.Name = "connectToolStripMenuItem";
            this.connectToolStripMenuItem.Size = new System.Drawing.Size(75, 26);
            this.connectToolStripMenuItem.Text = "Connect";
            // 
            // settingsToolStripMenuItem
            // 
            this.settingsToolStripMenuItem.Name = "settingsToolStripMenuItem";
            this.settingsToolStripMenuItem.Size = new System.Drawing.Size(153, 26);
            this.settingsToolStripMenuItem.Text = "Connection Settings";
            // 
            // resetToolStripMenuItem
            // 
            this.resetToolStripMenuItem.Name = "resetToolStripMenuItem";
            this.resetToolStripMenuItem.Size = new System.Drawing.Size(57, 26);
            this.resetToolStripMenuItem.Text = "Reset";
            // 
            // toolsToolStripMenuItem
            // 
            this.toolsToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.screenshootToolStripMenuItem,
            this.findAValueToolStripMenuItem,
            this.compressionToolStripMenuItem});
            this.toolsToolStripMenuItem.Name = "toolsToolStripMenuItem";
            this.toolsToolStripMenuItem.Size = new System.Drawing.Size(56, 26);
            this.toolsToolStripMenuItem.Text = "Tools";
            // 
            // screenshootToolStripMenuItem
            // 
            this.screenshootToolStripMenuItem.Name = "screenshootToolStripMenuItem";
            this.screenshootToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.screenshootToolStripMenuItem.Text = "Screenshot";
            // 
            // findAValueToolStripMenuItem
            // 
            this.findAValueToolStripMenuItem.Name = "findAValueToolStripMenuItem";
            this.findAValueToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.findAValueToolStripMenuItem.Text = "Find a Value";
            // 
            // compressionToolStripMenuItem
            // 
            this.compressionToolStripMenuItem.Name = "compressionToolStripMenuItem";
            this.compressionToolStripMenuItem.Size = new System.Drawing.Size(170, 26);
            this.compressionToolStripMenuItem.Text = "Compression";
            // 
            // maskToolStripMenuItem
            // 
            this.maskToolStripMenuItem.DropDownItems.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.startToolStripMenuItem,
            this.pauseToolStripMenuItem,
            this.clearToolStripMenuItem});
            this.maskToolStripMenuItem.Name = "maskToolStripMenuItem";
            this.maskToolStripMenuItem.Size = new System.Drawing.Size(55, 26);
            this.maskToolStripMenuItem.Text = "Mask";
            // 
            // startToolStripMenuItem
            // 
            this.startToolStripMenuItem.Name = "startToolStripMenuItem";
            this.startToolStripMenuItem.Size = new System.Drawing.Size(219, 26);
            this.startToolStripMenuItem.Text = "Start Filter Learning";
            this.startToolStripMenuItem.Click += new System.EventHandler(this.startToolStripMenuItem_Click);
            // 
            // pauseToolStripMenuItem
            // 
            this.pauseToolStripMenuItem.Name = "pauseToolStripMenuItem";
            this.pauseToolStripMenuItem.Size = new System.Drawing.Size(219, 26);
            this.pauseToolStripMenuItem.Text = "Pause Filter Learning";
            this.pauseToolStripMenuItem.Click += new System.EventHandler(this.pauseToolStripMenuItem_Click);
            // 
            // clearToolStripMenuItem
            // 
            this.clearToolStripMenuItem.Name = "clearToolStripMenuItem";
            this.clearToolStripMenuItem.Size = new System.Drawing.Size(219, 26);
            this.clearToolStripMenuItem.Text = "Clear Filter";
            this.clearToolStripMenuItem.Click += new System.EventHandler(this.clearToolStripMenuItem_Click);
            // 
            // aboutToolStripMenuItem
            // 
            this.aboutToolStripMenuItem.Name = "aboutToolStripMenuItem";
            this.aboutToolStripMenuItem.Size = new System.Drawing.Size(62, 26);
            this.aboutToolStripMenuItem.Text = "About";
            // 
            // m_status
            // 
            this.m_status.Dock = System.Windows.Forms.DockStyle.Fill;
            this.m_status.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.m_status.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.statusConnState,
            this.statusSpeed,
            this.statusPort,
            this.statusSilentMode,
            this.statusAutosaveTrace,
            this.statusFwVer,
            this.statusMaskFilterEn,
            this.statusErrors,
            this.statusMsgCnt});
            this.m_status.Location = new System.Drawing.Point(0, 423);
            this.m_status.Name = "m_status";
            this.m_status.Size = new System.Drawing.Size(1182, 30);
            this.m_status.TabIndex = 4;
            this.m_status.Text = "statusStrip1";
            // 
            // statusConnState
            // 
            this.statusConnState.Name = "statusConnState";
            this.statusConnState.Size = new System.Drawing.Size(115, 25);
            this.statusConnState.Text = "statusConnState";
            // 
            // statusSpeed
            // 
            this.statusSpeed.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusSpeed.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusSpeed.Name = "statusSpeed";
            this.statusSpeed.Size = new System.Drawing.Size(93, 25);
            this.statusSpeed.Text = "statusSpeed";
            // 
            // statusPort
            // 
            this.statusPort.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusPort.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusPort.Name = "statusPort";
            this.statusPort.Size = new System.Drawing.Size(77, 25);
            this.statusPort.Text = "statusPort";
            // 
            // statusSilentMode
            // 
            this.statusSilentMode.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusSilentMode.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusSilentMode.Name = "statusSilentMode";
            this.statusSilentMode.Size = new System.Drawing.Size(50, 25);
            this.statusSilentMode.Text = "Silent";
            // 
            // statusAutosaveTrace
            // 
            this.statusAutosaveTrace.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusAutosaveTrace.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusAutosaveTrace.Name = "statusAutosaveTrace";
            this.statusAutosaveTrace.Size = new System.Drawing.Size(79, 25);
            this.statusAutosaveTrace.Text = "SaveTrace";
            // 
            // statusFwVer
            // 
            this.statusFwVer.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusFwVer.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusFwVer.Name = "statusFwVer";
            this.statusFwVer.Size = new System.Drawing.Size(90, 25);
            this.statusFwVer.Text = "statusFwVer";
            // 
            // statusMaskFilterEn
            // 
            this.statusMaskFilterEn.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusMaskFilterEn.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusMaskFilterEn.Name = "statusMaskFilterEn";
            this.statusMaskFilterEn.Size = new System.Drawing.Size(46, 25);
            this.statusMaskFilterEn.Text = "Filter";
            // 
            // statusMsgCnt
            // 
            this.statusMsgCnt.BorderSides = ((System.Windows.Forms.ToolStripStatusLabelBorderSides)((System.Windows.Forms.ToolStripStatusLabelBorderSides.Left | System.Windows.Forms.ToolStripStatusLabelBorderSides.Right)));
            this.statusMsgCnt.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusMsgCnt.Name = "statusMsgCnt";
            this.statusMsgCnt.Size = new System.Drawing.Size(69, 25);
            this.statusMsgCnt.Text = "received";
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Controls.Add(this.menuStrip, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.m_status, 0, 2);
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutData, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 3;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(1182, 453);
            this.tableLayoutPanel1.TabIndex = 5;
            // 
            // statusErrors
            // 
            this.statusErrors.BorderSides = System.Windows.Forms.ToolStripStatusLabelBorderSides.Left;
            this.statusErrors.Margin = new System.Windows.Forms.Padding(20, 3, 0, 2);
            this.statusErrors.Name = "statusErrors";
            this.statusErrors.Size = new System.Drawing.Size(89, 25);
            this.statusErrors.Text = "statusErrors";
            // 
            // FrmMain
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1182, 453);
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "FrmMain";
            this.Text = "Form1";
            this.tableLayoutData.ResumeLayout(false);
            this.menuStrip.ResumeLayout(false);
            this.menuStrip.PerformLayout();
            this.m_status.ResumeLayout(false);
            this.m_status.PerformLayout();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TableLayoutPanel tableLayoutData;
        private System.Windows.Forms.MenuStrip menuStrip;
        private System.Windows.Forms.ToolStripMenuItem connectToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem settingsToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem resetToolStripMenuItem;
        private System.Windows.Forms.StatusStrip m_status;
        private System.Windows.Forms.ToolStripStatusLabel statusConnState;
        private System.Windows.Forms.ToolStripStatusLabel statusSpeed;
        private System.Windows.Forms.ToolStripStatusLabel statusFwVer;
        private System.Windows.Forms.ToolStripMenuItem toolsToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel statusPort;
        private System.Windows.Forms.ToolStripMenuItem aboutToolStripMenuItem;
        private System.Windows.Forms.TabControl tabControl;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.ToolStripMenuItem remotoToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel statusMsgCnt;
        private System.Windows.Forms.ToolStripMenuItem maskToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem startToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pauseToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem clearToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem fileToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem saveSessionToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem loadSessionToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel statusMaskFilterEn;
        private System.Windows.Forms.ToolStripStatusLabel statusSilentMode;
        private System.Windows.Forms.ToolStripStatusLabel statusAutosaveTrace;
        private System.Windows.Forms.ToolStripMenuItem findAValueToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem screenshootToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem compressionToolStripMenuItem;
        private System.Windows.Forms.ToolStripStatusLabel statusErrors;
    }
}