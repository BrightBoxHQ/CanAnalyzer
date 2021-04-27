namespace canAnalyzer
{
    partial class ucBruteForcer
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.gbResponse = new System.Windows.Forms.GroupBox();
            this.label9 = new System.Windows.Forms.Label();
            this.cbResponseHeaderPos = new System.Windows.Forms.ComboBox();
            this.txtRsp7 = new System.Windows.Forms.TextBox();
            this.txtRsp6 = new System.Windows.Forms.TextBox();
            this.txtRsp5 = new System.Windows.Forms.TextBox();
            this.txtRsp4 = new System.Windows.Forms.TextBox();
            this.txtRsp3 = new System.Windows.Forms.TextBox();
            this.txtRsp2 = new System.Windows.Forms.TextBox();
            this.txtRsp1 = new System.Windows.Forms.TextBox();
            this.txtRsp0 = new System.Windows.Forms.TextBox();
            this.txtRespId = new System.Windows.Forms.TextBox();
            this.gbRequest = new System.Windows.Forms.GroupBox();
            this.txtFlow7 = new System.Windows.Forms.TextBox();
            this.txtFlow6 = new System.Windows.Forms.TextBox();
            this.txtFlow5 = new System.Windows.Forms.TextBox();
            this.txtFlow4 = new System.Windows.Forms.TextBox();
            this.txtFlow3 = new System.Windows.Forms.TextBox();
            this.txtFlow2 = new System.Windows.Forms.TextBox();
            this.txtFlow1 = new System.Windows.Forms.TextBox();
            this.txtFlow0 = new System.Windows.Forms.TextBox();
            this.cbFlowDLC = new System.Windows.Forms.ComboBox();
            this.label5 = new System.Windows.Forms.Label();
            this.txtReq7 = new System.Windows.Forms.TextBox();
            this.txtReq6 = new System.Windows.Forms.TextBox();
            this.txtReq5 = new System.Windows.Forms.TextBox();
            this.txtReq4 = new System.Windows.Forms.TextBox();
            this.txtReq3 = new System.Windows.Forms.TextBox();
            this.txtReq2 = new System.Windows.Forms.TextBox();
            this.txtReq1 = new System.Windows.Forms.TextBox();
            this.txtReq0 = new System.Windows.Forms.TextBox();
            this.cbReqDLC = new System.Windows.Forms.ComboBox();
            this.txtReqId = new System.Windows.Forms.TextBox();
            this.gbSettings = new System.Windows.Forms.GroupBox();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.label3 = new System.Windows.Forms.Label();
            this.tbReportName = new System.Windows.Forms.TextBox();
            this.numDelay = new System.Windows.Forms.NumericUpDown();
            this.label6 = new System.Windows.Forms.Label();
            this.tbReportPath = new System.Windows.Forms.TextBox();
            this.cbTemplates = new System.Windows.Forms.ComboBox();
            this.btnReportPath = new System.Windows.Forms.Button();
            this.numAttempts = new System.Windows.Forms.NumericUpDown();
            this.numTimeout = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbIs29bit = new System.Windows.Forms.CheckBox();
            this.gbData = new System.Windows.Forms.GroupBox();
            this.label10 = new System.Windows.Forms.Label();
            this.lblCurValue = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.btnLoadData = new System.Windows.Forms.Button();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.txtValRange = new System.Windows.Forms.TextBox();
            this.menu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.pnlTrace = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.gbResponse.SuspendLayout();
            this.gbRequest.SuspendLayout();
            this.gbSettings.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAttempts)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).BeginInit();
            this.gbData.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.gbResponse);
            this.panel1.Controls.Add(this.gbRequest);
            this.panel1.Controls.Add(this.gbSettings);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(5, 5);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(682, 290);
            this.panel1.TabIndex = 0;
            // 
            // gbResponse
            // 
            this.gbResponse.Controls.Add(this.label9);
            this.gbResponse.Controls.Add(this.cbResponseHeaderPos);
            this.gbResponse.Controls.Add(this.txtRsp7);
            this.gbResponse.Controls.Add(this.txtRsp6);
            this.gbResponse.Controls.Add(this.txtRsp5);
            this.gbResponse.Controls.Add(this.txtRsp4);
            this.gbResponse.Controls.Add(this.txtRsp3);
            this.gbResponse.Controls.Add(this.txtRsp2);
            this.gbResponse.Controls.Add(this.txtRsp1);
            this.gbResponse.Controls.Add(this.txtRsp0);
            this.gbResponse.Controls.Add(this.txtRespId);
            this.gbResponse.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gbResponse.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbResponse.Location = new System.Drawing.Point(0, 174);
            this.gbResponse.Margin = new System.Windows.Forms.Padding(10);
            this.gbResponse.Name = "gbResponse";
            this.gbResponse.Size = new System.Drawing.Size(570, 66);
            this.gbResponse.TabIndex = 12;
            this.gbResponse.TabStop = false;
            this.gbResponse.Text = "Response format";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(112, 46);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(58, 17);
            this.label9.TabIndex = 23;
            this.label9.Text = "Hdr pos";
            // 
            // cbResponseHeaderPos
            // 
            this.cbResponseHeaderPos.FormattingEnabled = true;
            this.cbResponseHeaderPos.Location = new System.Drawing.Point(115, 18);
            this.cbResponseHeaderPos.Name = "cbResponseHeaderPos";
            this.cbResponseHeaderPos.Size = new System.Drawing.Size(48, 24);
            this.cbResponseHeaderPos.TabIndex = 14;
            // 
            // txtRsp7
            // 
            this.txtRsp7.Location = new System.Drawing.Point(520, 20);
            this.txtRsp7.Name = "txtRsp7";
            this.txtRsp7.Size = new System.Drawing.Size(42, 22);
            this.txtRsp7.TabIndex = 11;
            // 
            // txtRsp6
            // 
            this.txtRsp6.Location = new System.Drawing.Point(470, 20);
            this.txtRsp6.Name = "txtRsp6";
            this.txtRsp6.Size = new System.Drawing.Size(42, 22);
            this.txtRsp6.TabIndex = 10;
            // 
            // txtRsp5
            // 
            this.txtRsp5.Location = new System.Drawing.Point(420, 20);
            this.txtRsp5.Name = "txtRsp5";
            this.txtRsp5.Size = new System.Drawing.Size(42, 22);
            this.txtRsp5.TabIndex = 9;
            // 
            // txtRsp4
            // 
            this.txtRsp4.Location = new System.Drawing.Point(370, 20);
            this.txtRsp4.Name = "txtRsp4";
            this.txtRsp4.Size = new System.Drawing.Size(42, 22);
            this.txtRsp4.TabIndex = 8;
            this.txtRsp4.Text = "X";
            // 
            // txtRsp3
            // 
            this.txtRsp3.Location = new System.Drawing.Point(320, 20);
            this.txtRsp3.Name = "txtRsp3";
            this.txtRsp3.Size = new System.Drawing.Size(42, 22);
            this.txtRsp3.TabIndex = 7;
            this.txtRsp3.Text = "X>>8";
            // 
            // txtRsp2
            // 
            this.txtRsp2.Location = new System.Drawing.Point(270, 20);
            this.txtRsp2.Name = "txtRsp2";
            this.txtRsp2.Size = new System.Drawing.Size(42, 22);
            this.txtRsp2.TabIndex = 6;
            this.txtRsp2.Text = "0x62";
            // 
            // txtRsp1
            // 
            this.txtRsp1.Location = new System.Drawing.Point(220, 20);
            this.txtRsp1.Name = "txtRsp1";
            this.txtRsp1.Size = new System.Drawing.Size(42, 22);
            this.txtRsp1.TabIndex = 5;
            this.txtRsp1.Text = "H2";
            // 
            // txtRsp0
            // 
            this.txtRsp0.Location = new System.Drawing.Point(172, 20);
            this.txtRsp0.Name = "txtRsp0";
            this.txtRsp0.Size = new System.Drawing.Size(42, 22);
            this.txtRsp0.TabIndex = 4;
            this.txtRsp0.Text = "Hdr1";
            // 
            // txtRespId
            // 
            this.txtRespId.Location = new System.Drawing.Point(6, 18);
            this.txtRespId.Multiline = true;
            this.txtRespId.Name = "txtRespId";
            this.txtRespId.Size = new System.Drawing.Size(100, 24);
            this.txtRespId.TabIndex = 0;
            this.txtRespId.Text = "Req_ID + 8";
            // 
            // gbRequest
            // 
            this.gbRequest.Controls.Add(this.txtFlow7);
            this.gbRequest.Controls.Add(this.txtFlow6);
            this.gbRequest.Controls.Add(this.txtFlow5);
            this.gbRequest.Controls.Add(this.txtFlow4);
            this.gbRequest.Controls.Add(this.txtFlow3);
            this.gbRequest.Controls.Add(this.txtFlow2);
            this.gbRequest.Controls.Add(this.txtFlow1);
            this.gbRequest.Controls.Add(this.txtFlow0);
            this.gbRequest.Controls.Add(this.cbFlowDLC);
            this.gbRequest.Controls.Add(this.label5);
            this.gbRequest.Controls.Add(this.txtReq7);
            this.gbRequest.Controls.Add(this.txtReq6);
            this.gbRequest.Controls.Add(this.txtReq5);
            this.gbRequest.Controls.Add(this.txtReq4);
            this.gbRequest.Controls.Add(this.txtReq3);
            this.gbRequest.Controls.Add(this.txtReq2);
            this.gbRequest.Controls.Add(this.txtReq1);
            this.gbRequest.Controls.Add(this.txtReq0);
            this.gbRequest.Controls.Add(this.cbReqDLC);
            this.gbRequest.Controls.Add(this.txtReqId);
            this.gbRequest.Location = new System.Drawing.Point(0, 85);
            this.gbRequest.Margin = new System.Windows.Forms.Padding(10);
            this.gbRequest.Name = "gbRequest";
            this.gbRequest.Size = new System.Drawing.Size(570, 82);
            this.gbRequest.TabIndex = 1;
            this.gbRequest.TabStop = false;
            this.gbRequest.Text = "Request format";
            // 
            // txtFlow7
            // 
            this.txtFlow7.Location = new System.Drawing.Point(520, 55);
            this.txtFlow7.Name = "txtFlow7";
            this.txtFlow7.Size = new System.Drawing.Size(42, 22);
            this.txtFlow7.TabIndex = 21;
            this.txtFlow7.Text = "0";
            // 
            // txtFlow6
            // 
            this.txtFlow6.Location = new System.Drawing.Point(465, 55);
            this.txtFlow6.Name = "txtFlow6";
            this.txtFlow6.Size = new System.Drawing.Size(42, 22);
            this.txtFlow6.TabIndex = 20;
            this.txtFlow6.Text = "0";
            // 
            // txtFlow5
            // 
            this.txtFlow5.Location = new System.Drawing.Point(415, 55);
            this.txtFlow5.Name = "txtFlow5";
            this.txtFlow5.Size = new System.Drawing.Size(42, 22);
            this.txtFlow5.TabIndex = 19;
            this.txtFlow5.Text = "0";
            // 
            // txtFlow4
            // 
            this.txtFlow4.Location = new System.Drawing.Point(365, 55);
            this.txtFlow4.Name = "txtFlow4";
            this.txtFlow4.Size = new System.Drawing.Size(42, 22);
            this.txtFlow4.TabIndex = 18;
            this.txtFlow4.Text = "0";
            // 
            // txtFlow3
            // 
            this.txtFlow3.Location = new System.Drawing.Point(315, 55);
            this.txtFlow3.Name = "txtFlow3";
            this.txtFlow3.Size = new System.Drawing.Size(42, 22);
            this.txtFlow3.TabIndex = 17;
            this.txtFlow3.Text = "0";
            // 
            // txtFlow2
            // 
            this.txtFlow2.Location = new System.Drawing.Point(265, 55);
            this.txtFlow2.Name = "txtFlow2";
            this.txtFlow2.Size = new System.Drawing.Size(42, 22);
            this.txtFlow2.TabIndex = 16;
            this.txtFlow2.Text = "0";
            // 
            // txtFlow1
            // 
            this.txtFlow1.Location = new System.Drawing.Point(215, 55);
            this.txtFlow1.Name = "txtFlow1";
            this.txtFlow1.Size = new System.Drawing.Size(42, 22);
            this.txtFlow1.TabIndex = 15;
            this.txtFlow1.Text = "0";
            // 
            // txtFlow0
            // 
            this.txtFlow0.Location = new System.Drawing.Point(172, 55);
            this.txtFlow0.Name = "txtFlow0";
            this.txtFlow0.Size = new System.Drawing.Size(42, 22);
            this.txtFlow0.TabIndex = 14;
            this.txtFlow0.Text = "0x30";
            // 
            // cbFlowDLC
            // 
            this.cbFlowDLC.FormattingEnabled = true;
            this.cbFlowDLC.Location = new System.Drawing.Point(115, 53);
            this.cbFlowDLC.Name = "cbFlowDLC";
            this.cbFlowDLC.Size = new System.Drawing.Size(48, 24);
            this.cbFlowDLC.TabIndex = 13;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(65, 58);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(36, 17);
            this.label5.TabIndex = 12;
            this.label5.Text = "Flow";
            // 
            // txtReq7
            // 
            this.txtReq7.Location = new System.Drawing.Point(520, 20);
            this.txtReq7.Name = "txtReq7";
            this.txtReq7.Size = new System.Drawing.Size(42, 22);
            this.txtReq7.TabIndex = 11;
            this.txtReq7.Text = "0";
            // 
            // txtReq6
            // 
            this.txtReq6.Location = new System.Drawing.Point(470, 20);
            this.txtReq6.Name = "txtReq6";
            this.txtReq6.Size = new System.Drawing.Size(42, 22);
            this.txtReq6.TabIndex = 10;
            this.txtReq6.Text = "0";
            // 
            // txtReq5
            // 
            this.txtReq5.Location = new System.Drawing.Point(420, 20);
            this.txtReq5.Name = "txtReq5";
            this.txtReq5.Size = new System.Drawing.Size(42, 22);
            this.txtReq5.TabIndex = 9;
            this.txtReq5.Text = "0";
            // 
            // txtReq4
            // 
            this.txtReq4.Location = new System.Drawing.Point(370, 20);
            this.txtReq4.Name = "txtReq4";
            this.txtReq4.Size = new System.Drawing.Size(42, 22);
            this.txtReq4.TabIndex = 8;
            this.txtReq4.Text = "0";
            // 
            // txtReq3
            // 
            this.txtReq3.Location = new System.Drawing.Point(320, 20);
            this.txtReq3.Name = "txtReq3";
            this.txtReq3.Size = new System.Drawing.Size(42, 22);
            this.txtReq3.TabIndex = 7;
            this.txtReq3.Text = "X";
            // 
            // txtReq2
            // 
            this.txtReq2.Location = new System.Drawing.Point(270, 20);
            this.txtReq2.Name = "txtReq2";
            this.txtReq2.Size = new System.Drawing.Size(42, 22);
            this.txtReq2.TabIndex = 6;
            this.txtReq2.Text = "X>>8";
            // 
            // txtReq1
            // 
            this.txtReq1.Location = new System.Drawing.Point(215, 20);
            this.txtReq1.Name = "txtReq1";
            this.txtReq1.Size = new System.Drawing.Size(42, 22);
            this.txtReq1.TabIndex = 5;
            this.txtReq1.Text = "0x22";
            // 
            // txtReq0
            // 
            this.txtReq0.Location = new System.Drawing.Point(172, 20);
            this.txtReq0.Name = "txtReq0";
            this.txtReq0.Size = new System.Drawing.Size(42, 22);
            this.txtReq0.TabIndex = 4;
            this.txtReq0.Text = "0x03";
            // 
            // cbReqDLC
            // 
            this.cbReqDLC.FormattingEnabled = true;
            this.cbReqDLC.Location = new System.Drawing.Point(115, 18);
            this.cbReqDLC.Name = "cbReqDLC";
            this.cbReqDLC.Size = new System.Drawing.Size(48, 24);
            this.cbReqDLC.TabIndex = 2;
            // 
            // txtReqId
            // 
            this.txtReqId.Location = new System.Drawing.Point(6, 18);
            this.txtReqId.Multiline = true;
            this.txtReqId.Name = "txtReqId";
            this.txtReqId.Size = new System.Drawing.Size(100, 24);
            this.txtReqId.TabIndex = 0;
            this.txtReqId.Text = "0x7E0";
            // 
            // gbSettings
            // 
            this.gbSettings.Controls.Add(this.label8);
            this.gbSettings.Controls.Add(this.label7);
            this.gbSettings.Controls.Add(this.label3);
            this.gbSettings.Controls.Add(this.tbReportName);
            this.gbSettings.Controls.Add(this.numDelay);
            this.gbSettings.Controls.Add(this.label6);
            this.gbSettings.Controls.Add(this.tbReportPath);
            this.gbSettings.Controls.Add(this.cbTemplates);
            this.gbSettings.Controls.Add(this.btnReportPath);
            this.gbSettings.Controls.Add(this.numAttempts);
            this.gbSettings.Controls.Add(this.numTimeout);
            this.gbSettings.Controls.Add(this.label2);
            this.gbSettings.Controls.Add(this.label1);
            this.gbSettings.Controls.Add(this.cbIs29bit);
            this.gbSettings.Location = new System.Drawing.Point(0, 0);
            this.gbSettings.Margin = new System.Windows.Forms.Padding(10);
            this.gbSettings.Name = "gbSettings";
            this.gbSettings.Size = new System.Drawing.Size(570, 86);
            this.gbSettings.TabIndex = 0;
            this.gbSettings.TabStop = false;
            this.gbSettings.Text = "Settings";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(7, 60);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(55, 17);
            this.label8.TabIndex = 15;
            this.label8.Text = "Report:";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(403, 60);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(45, 17);
            this.label7.TabIndex = 14;
            this.label7.Text = "Name";
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(272, 9);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(56, 17);
            this.label3.TabIndex = 13;
            this.label3.Text = "CAN 29";
            // 
            // tbReportName
            // 
            this.tbReportName.Location = new System.Drawing.Point(454, 58);
            this.tbReportName.Name = "tbReportName";
            this.tbReportName.Size = new System.Drawing.Size(103, 22);
            this.tbReportName.TabIndex = 11;
            // 
            // numDelay
            // 
            this.numDelay.Location = new System.Drawing.Point(418, 29);
            this.numDelay.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numDelay.Name = "numDelay";
            this.numDelay.Size = new System.Drawing.Size(63, 22);
            this.numDelay.TabIndex = 10;
            this.numDelay.Value = new decimal(new int[] {
            5,
            0,
            0,
            0});
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(415, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(42, 17);
            this.label6.TabIndex = 9;
            this.label6.Text = "delay";
            // 
            // tbReportPath
            // 
            this.tbReportPath.Location = new System.Drawing.Point(139, 57);
            this.tbReportPath.Name = "tbReportPath";
            this.tbReportPath.Size = new System.Drawing.Size(259, 22);
            this.tbReportPath.TabIndex = 6;
            // 
            // cbTemplates
            // 
            this.cbTemplates.FormattingEnabled = true;
            this.cbTemplates.Location = new System.Drawing.Point(9, 21);
            this.cbTemplates.Name = "cbTemplates";
            this.cbTemplates.Size = new System.Drawing.Size(198, 24);
            this.cbTemplates.TabIndex = 7;
            // 
            // btnReportPath
            // 
            this.btnReportPath.Location = new System.Drawing.Point(68, 56);
            this.btnReportPath.Name = "btnReportPath";
            this.btnReportPath.Size = new System.Drawing.Size(60, 23);
            this.btnReportPath.TabIndex = 5;
            this.btnReportPath.Text = "Folder";
            this.btnReportPath.UseVisualStyleBackColor = true;
            // 
            // numAttempts
            // 
            this.numAttempts.Location = new System.Drawing.Point(498, 29);
            this.numAttempts.Name = "numAttempts";
            this.numAttempts.Size = new System.Drawing.Size(59, 22);
            this.numAttempts.TabIndex = 4;
            // 
            // numTimeout
            // 
            this.numTimeout.Location = new System.Drawing.Point(339, 29);
            this.numTimeout.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.numTimeout.Name = "numTimeout";
            this.numTimeout.Size = new System.Drawing.Size(63, 22);
            this.numTimeout.TabIndex = 3;
            this.numTimeout.Value = new decimal(new int[] {
            150,
            0,
            0,
            0});
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(495, 9);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(62, 17);
            this.label2.TabIndex = 2;
            this.label2.Text = "attemtps";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(336, 9);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(54, 17);
            this.label1.TabIndex = 1;
            this.label1.Text = "timeout";
            // 
            // cbIs29bit
            // 
            this.cbIs29bit.AutoSize = true;
            this.cbIs29bit.Location = new System.Drawing.Point(289, 32);
            this.cbIs29bit.Name = "cbIs29bit";
            this.cbIs29bit.RightToLeft = System.Windows.Forms.RightToLeft.Yes;
            this.cbIs29bit.Size = new System.Drawing.Size(18, 17);
            this.cbIs29bit.TabIndex = 0;
            this.cbIs29bit.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.cbIs29bit.UseVisualStyleBackColor = true;
            // 
            // gbData
            // 
            this.gbData.Controls.Add(this.label10);
            this.gbData.Controls.Add(this.lblCurValue);
            this.gbData.Controls.Add(this.label4);
            this.gbData.Controls.Add(this.btnLoadData);
            this.gbData.Controls.Add(this.btnStartStop);
            this.gbData.Controls.Add(this.txtValRange);
            this.gbData.FlatStyle = System.Windows.Forms.FlatStyle.Flat;
            this.gbData.Font = new System.Drawing.Font("Microsoft Sans Serif", 7.8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.gbData.Location = new System.Drawing.Point(6, 244);
            this.gbData.Margin = new System.Windows.Forms.Padding(10);
            this.gbData.Name = "gbData";
            this.gbData.Size = new System.Drawing.Size(569, 45);
            this.gbData.TabIndex = 12;
            this.gbData.TabStop = false;
            this.gbData.Text = "Data";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(482, 8);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(72, 17);
            this.label10.TabIndex = 6;
            this.label10.Text = "Current X:";
            // 
            // lblCurValue
            // 
            this.lblCurValue.AutoSize = true;
            this.lblCurValue.Location = new System.Drawing.Point(482, 25);
            this.lblCurValue.Name = "lblCurValue";
            this.lblCurValue.Size = new System.Drawing.Size(70, 17);
            this.lblCurValue.TabIndex = 5;
            this.lblCurValue.Text = "0x123456";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(93, 20);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(29, 17);
            this.label4.TabIndex = 4;
            this.label4.Text = "X =";
            // 
            // btnLoadData
            // 
            this.btnLoadData.Location = new System.Drawing.Point(9, 19);
            this.btnLoadData.Name = "btnLoadData";
            this.btnLoadData.Size = new System.Drawing.Size(78, 23);
            this.btnLoadData.TabIndex = 3;
            this.btnLoadData.Text = "From File";
            this.btnLoadData.UseVisualStyleBackColor = true;
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(401, 16);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(75, 23);
            this.btnStartStop.TabIndex = 2;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            // 
            // txtValRange
            // 
            this.txtValRange.Location = new System.Drawing.Point(124, 17);
            this.txtValRange.Name = "txtValRange";
            this.txtValRange.Size = new System.Drawing.Size(273, 22);
            this.txtValRange.TabIndex = 1;
            this.txtValRange.Text = "0 - 0xFF";
            // 
            // menu
            // 
            this.menu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(61, 4);
            // 
            // pnlTrace
            // 
            this.pnlTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.pnlTrace.Location = new System.Drawing.Point(5, 295);
            this.pnlTrace.Name = "pnlTrace";
            this.pnlTrace.Size = new System.Drawing.Size(682, 226);
            this.pnlTrace.TabIndex = 13;
            // 
            // ucBruteForcer
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.pnlTrace);
            this.Controls.Add(this.gbData);
            this.Controls.Add(this.panel1);
            this.Name = "ucBruteForcer";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.Size = new System.Drawing.Size(692, 526);
            this.panel1.ResumeLayout(false);
            this.gbResponse.ResumeLayout(false);
            this.gbResponse.PerformLayout();
            this.gbRequest.ResumeLayout(false);
            this.gbRequest.PerformLayout();
            this.gbSettings.ResumeLayout(false);
            this.gbSettings.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numDelay)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numAttempts)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numTimeout)).EndInit();
            this.gbData.ResumeLayout(false);
            this.gbData.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.GroupBox gbSettings;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.CheckBox cbIs29bit;
        private System.Windows.Forms.TextBox tbReportPath;
        private System.Windows.Forms.Button btnReportPath;
        private System.Windows.Forms.NumericUpDown numAttempts;
        private System.Windows.Forms.NumericUpDown numTimeout;
        private System.Windows.Forms.GroupBox gbRequest;
        private System.Windows.Forms.ComboBox cbReqDLC;
        private System.Windows.Forms.TextBox txtReqId;
        private System.Windows.Forms.ComboBox cbTemplates;
        private System.Windows.Forms.TextBox txtReq7;
        private System.Windows.Forms.TextBox txtReq6;
        private System.Windows.Forms.TextBox txtReq5;
        private System.Windows.Forms.TextBox txtReq4;
        private System.Windows.Forms.TextBox txtReq3;
        private System.Windows.Forms.TextBox txtReq2;
        private System.Windows.Forms.TextBox txtReq1;
        private System.Windows.Forms.TextBox txtReq0;
        private System.Windows.Forms.GroupBox gbResponse;
        private System.Windows.Forms.TextBox txtRsp7;
        private System.Windows.Forms.TextBox txtRsp6;
        private System.Windows.Forms.TextBox txtRsp5;
        private System.Windows.Forms.TextBox txtRsp4;
        private System.Windows.Forms.TextBox txtRsp3;
        private System.Windows.Forms.TextBox txtRsp2;
        private System.Windows.Forms.TextBox txtRsp1;
        private System.Windows.Forms.TextBox txtRsp0;
        private System.Windows.Forms.TextBox txtRespId;
        private System.Windows.Forms.GroupBox gbData;
        private System.Windows.Forms.TextBox txtValRange;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.TextBox txtFlow7;
        private System.Windows.Forms.TextBox txtFlow6;
        private System.Windows.Forms.TextBox txtFlow5;
        private System.Windows.Forms.TextBox txtFlow4;
        private System.Windows.Forms.TextBox txtFlow3;
        private System.Windows.Forms.TextBox txtFlow2;
        private System.Windows.Forms.TextBox txtFlow1;
        private System.Windows.Forms.TextBox txtFlow0;
        private System.Windows.Forms.ComboBox cbFlowDLC;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label9;
        private System.Windows.Forms.ComboBox cbResponseHeaderPos;
        private System.Windows.Forms.NumericUpDown numDelay;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.Button btnLoadData;
        private System.Windows.Forms.TextBox tbReportName;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.Label label8;
        private System.Windows.Forms.Label label7;
        private System.Windows.Forms.ContextMenuStrip menu;
        private System.Windows.Forms.Label lblCurValue;
        private System.Windows.Forms.Label label10;
        private System.Windows.Forms.Panel pnlTrace;
    }
}
