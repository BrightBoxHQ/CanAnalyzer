namespace canAnalyzer
{
    partial class ucGenericTest
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

        #region Component Designer generated code

        /// <summary> 
        /// Required method for Designer support - do not modify 
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tbTrace = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnPath = new System.Windows.Forms.Button();
            this.tbSavePath = new System.Windows.Forms.TextBox();
            this.pnlInfo = new System.Windows.Forms.Panel();
            this.label1 = new System.Windows.Forms.Label();
            this.tbComment = new System.Windows.Forms.TextBox();
            this.lblPowerState = new System.Windows.Forms.Label();
            this.cbVehiclePwrState = new System.Windows.Forms.ComboBox();
            this.lblVehicleName = new System.Windows.Forms.Label();
            this.tbVehicleName = new System.Windows.Forms.TextBox();
            this.gbConfig = new System.Windows.Forms.GroupBox();
            this.cbDirectEcuReq = new System.Windows.Forms.CheckBox();
            this.cbObd29bit = new System.Windows.Forms.CheckBox();
            this.lblCfgTrace = new System.Windows.Forms.Label();
            this.numCfgTrace = new System.Windows.Forms.NumericUpDown();
            this.cbObd = new System.Windows.Forms.CheckBox();
            this.btnStart = new System.Windows.Forms.Button();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.pnlInfo.SuspendLayout();
            this.gbConfig.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCfgTrace)).BeginInit();
            this.SuspendLayout();
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.tbTrace, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 167F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(500, 470);
            this.tableLayoutPanel1.TabIndex = 16;
            // 
            // tbTrace
            // 
            this.tbTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbTrace.Location = new System.Drawing.Point(6, 173);
            this.tbTrace.Margin = new System.Windows.Forms.Padding(6);
            this.tbTrace.Name = "tbTrace";
            this.tbTrace.Size = new System.Drawing.Size(488, 291);
            this.tbTrace.TabIndex = 16;
            this.tbTrace.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnPath);
            this.panel1.Controls.Add(this.tbSavePath);
            this.panel1.Controls.Add(this.pnlInfo);
            this.panel1.Controls.Add(this.gbConfig);
            this.panel1.Controls.Add(this.btnStart);
            this.panel1.Location = new System.Drawing.Point(3, 3);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(491, 159);
            this.panel1.TabIndex = 15;
            // 
            // btnPath
            // 
            this.btnPath.Location = new System.Drawing.Point(10, 125);
            this.btnPath.Name = "btnPath";
            this.btnPath.Size = new System.Drawing.Size(50, 23);
            this.btnPath.TabIndex = 26;
            this.btnPath.Text = "Path";
            this.btnPath.UseVisualStyleBackColor = true;
            this.btnPath.Click += new System.EventHandler(this.btnPath_Click);
            // 
            // tbSavePath
            // 
            this.tbSavePath.Location = new System.Drawing.Point(75, 125);
            this.tbSavePath.Name = "tbSavePath";
            this.tbSavePath.Size = new System.Drawing.Size(215, 22);
            this.tbSavePath.TabIndex = 24;
            // 
            // pnlInfo
            // 
            this.pnlInfo.Controls.Add(this.label1);
            this.pnlInfo.Controls.Add(this.tbComment);
            this.pnlInfo.Controls.Add(this.lblPowerState);
            this.pnlInfo.Controls.Add(this.cbVehiclePwrState);
            this.pnlInfo.Controls.Add(this.lblVehicleName);
            this.pnlInfo.Controls.Add(this.tbVehicleName);
            this.pnlInfo.Location = new System.Drawing.Point(3, 3);
            this.pnlInfo.Name = "pnlInfo";
            this.pnlInfo.Size = new System.Drawing.Size(287, 110);
            this.pnlInfo.TabIndex = 23;
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(31, 43);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(67, 17);
            this.label1.TabIndex = 28;
            this.label1.Text = "Comment";
            // 
            // tbComment
            // 
            this.tbComment.Location = new System.Drawing.Point(105, 40);
            this.tbComment.Name = "tbComment";
            this.tbComment.Size = new System.Drawing.Size(170, 22);
            this.tbComment.TabIndex = 2;
            // 
            // lblPowerState
            // 
            this.lblPowerState.AutoSize = true;
            this.lblPowerState.Location = new System.Drawing.Point(14, 78);
            this.lblPowerState.Name = "lblPowerState";
            this.lblPowerState.Size = new System.Drawing.Size(84, 17);
            this.lblPowerState.TabIndex = 26;
            this.lblPowerState.Text = "Power State";
            // 
            // cbVehiclePwrState
            // 
            this.cbVehiclePwrState.Cursor = System.Windows.Forms.Cursors.Default;
            this.cbVehiclePwrState.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbVehiclePwrState.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbVehiclePwrState.FormattingEnabled = true;
            this.cbVehiclePwrState.ItemHeight = 16;
            this.cbVehiclePwrState.Location = new System.Drawing.Point(105, 75);
            this.cbVehiclePwrState.Margin = new System.Windows.Forms.Padding(4);
            this.cbVehiclePwrState.Name = "cbVehiclePwrState";
            this.cbVehiclePwrState.Size = new System.Drawing.Size(170, 24);
            this.cbVehiclePwrState.TabIndex = 3;
            // 
            // lblVehicleName
            // 
            this.lblVehicleName.AutoSize = true;
            this.lblVehicleName.Location = new System.Drawing.Point(4, 8);
            this.lblVehicleName.Name = "lblVehicleName";
            this.lblVehicleName.Size = new System.Drawing.Size(95, 17);
            this.lblVehicleName.TabIndex = 24;
            this.lblVehicleName.Text = "Vehicle Name";
            // 
            // tbVehicleName
            // 
            this.tbVehicleName.Location = new System.Drawing.Point(105, 5);
            this.tbVehicleName.Name = "tbVehicleName";
            this.tbVehicleName.Size = new System.Drawing.Size(170, 22);
            this.tbVehicleName.TabIndex = 1;
            // 
            // gbConfig
            // 
            this.gbConfig.Controls.Add(this.cbDirectEcuReq);
            this.gbConfig.Controls.Add(this.cbObd29bit);
            this.gbConfig.Controls.Add(this.lblCfgTrace);
            this.gbConfig.Controls.Add(this.numCfgTrace);
            this.gbConfig.Controls.Add(this.cbObd);
            this.gbConfig.Location = new System.Drawing.Point(310, 3);
            this.gbConfig.Name = "gbConfig";
            this.gbConfig.Size = new System.Drawing.Size(175, 125);
            this.gbConfig.TabIndex = 19;
            this.gbConfig.TabStop = false;
            this.gbConfig.Text = "Config";
            // 
            // cbDirectEcuReq
            // 
            this.cbDirectEcuReq.AutoSize = true;
            this.cbDirectEcuReq.Location = new System.Drawing.Point(6, 100);
            this.cbDirectEcuReq.Name = "cbDirectEcuReq";
            this.cbDirectEcuReq.Size = new System.Drawing.Size(124, 21);
            this.cbDirectEcuReq.TabIndex = 7;
            this.cbDirectEcuReq.Text = "Direct ECU req";
            this.cbDirectEcuReq.UseVisualStyleBackColor = true;
            // 
            // cbObd29bit
            // 
            this.cbObd29bit.AutoSize = true;
            this.cbObd29bit.Location = new System.Drawing.Point(6, 75);
            this.cbObd29bit.Name = "cbObd29bit";
            this.cbObd29bit.Size = new System.Drawing.Size(136, 21);
            this.cbObd29bit.TabIndex = 6;
            this.cbObd29bit.Text = "CAN Ext ID (29b)";
            this.cbObd29bit.UseVisualStyleBackColor = true;
            // 
            // lblCfgTrace
            // 
            this.lblCfgTrace.AutoSize = true;
            this.lblCfgTrace.Location = new System.Drawing.Point(70, 26);
            this.lblCfgTrace.Name = "lblCfgTrace";
            this.lblCfgTrace.Size = new System.Drawing.Size(100, 17);
            this.lblCfgTrace.TabIndex = 3;
            this.lblCfgTrace.Text = "Flow scan, sec";
            // 
            // numCfgTrace
            // 
            this.numCfgTrace.Location = new System.Drawing.Point(6, 24);
            this.numCfgTrace.Maximum = new decimal(new int[] {
            120,
            0,
            0,
            0});
            this.numCfgTrace.Name = "numCfgTrace";
            this.numCfgTrace.Size = new System.Drawing.Size(60, 22);
            this.numCfgTrace.TabIndex = 4;
            this.numCfgTrace.Value = new decimal(new int[] {
            15,
            0,
            0,
            0});
            // 
            // cbObd
            // 
            this.cbObd.AutoSize = true;
            this.cbObd.Location = new System.Drawing.Point(6, 50);
            this.cbObd.Name = "cbObd";
            this.cbObd.Size = new System.Drawing.Size(60, 21);
            this.cbObd.TabIndex = 5;
            this.cbObd.Text = "OBD";
            this.cbObd.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            this.btnStart.Location = new System.Drawing.Point(400, 133);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(85, 23);
            this.btnStart.TabIndex = 7;
            this.btnStart.Text = "Start";
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // ucGenericTest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucGenericTest";
            this.Size = new System.Drawing.Size(500, 470);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.pnlInfo.ResumeLayout(false);
            this.pnlInfo.PerformLayout();
            this.gbConfig.ResumeLayout(false);
            this.gbConfig.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numCfgTrace)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox gbConfig;
        private System.Windows.Forms.CheckBox cbObd;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.RichTextBox tbTrace;
        private System.Windows.Forms.Label lblCfgTrace;
        private System.Windows.Forms.NumericUpDown numCfgTrace;
        private System.Windows.Forms.Panel pnlInfo;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.TextBox tbComment;
        private System.Windows.Forms.Label lblPowerState;
        private System.Windows.Forms.ComboBox cbVehiclePwrState;
        private System.Windows.Forms.Label lblVehicleName;
        private System.Windows.Forms.TextBox tbVehicleName;
        private System.Windows.Forms.CheckBox cbObd29bit;
        private System.Windows.Forms.TextBox tbSavePath;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.Button btnPath;
        private System.Windows.Forms.CheckBox cbDirectEcuReq;
    }
}
