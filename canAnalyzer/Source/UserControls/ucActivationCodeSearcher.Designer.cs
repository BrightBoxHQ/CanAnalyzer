namespace canAnalyzer
{
    partial class ucActivationCodeSearcher
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
            this.btnStartStop = new System.Windows.Forms.Button();
            this.gbSettingsMain = new System.Windows.Forms.GroupBox();
            this.numIdTo = new System.Windows.Forms.NumericUpDown();
            this.numIdFrom = new System.Windows.Forms.NumericUpDown();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.rb29BitId = new System.Windows.Forms.RadioButton();
            this.rb11BitId = new System.Windows.Forms.RadioButton();
            this.tbTrace = new System.Windows.Forms.TextBox();
            this.pnlConfig = new System.Windows.Forms.Panel();
            this.gbSettingsExtra = new System.Windows.Forms.GroupBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tbIdCheck = new System.Windows.Forms.TextBox();
            this.tbBusSleep = new System.Windows.Forms.TextBox();
            this.label5 = new System.Windows.Forms.Label();
            this.label4 = new System.Windows.Forms.Label();
            this.tbData = new System.Windows.Forms.TextBox();
            this.tbCount = new System.Windows.Forms.TextBox();
            this.label3 = new System.Windows.Forms.Label();
            this.panel2 = new System.Windows.Forms.Panel();
            this.gbSettingsMain.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numIdTo)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIdFrom)).BeginInit();
            this.pnlConfig.SuspendLayout();
            this.gbSettingsExtra.SuspendLayout();
            this.panel2.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(86, 331);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(75, 23);
            this.btnStartStop.TabIndex = 3;
            this.btnStartStop.Text = "btnStart";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // gbSettingsMain
            // 
            this.gbSettingsMain.Controls.Add(this.numIdTo);
            this.gbSettingsMain.Controls.Add(this.numIdFrom);
            this.gbSettingsMain.Controls.Add(this.label2);
            this.gbSettingsMain.Controls.Add(this.label1);
            this.gbSettingsMain.Controls.Add(this.rb29BitId);
            this.gbSettingsMain.Controls.Add(this.rb11BitId);
            this.gbSettingsMain.Location = new System.Drawing.Point(10, 5);
            this.gbSettingsMain.Name = "gbSettingsMain";
            this.gbSettingsMain.Size = new System.Drawing.Size(156, 140);
            this.gbSettingsMain.TabIndex = 2;
            this.gbSettingsMain.TabStop = false;
            this.gbSettingsMain.Text = "Main Settings";
            // 
            // numIdTo
            // 
            this.numIdTo.Location = new System.Drawing.Point(62, 112);
            this.numIdTo.Name = "numIdTo";
            this.numIdTo.Size = new System.Drawing.Size(87, 22);
            this.numIdTo.TabIndex = 7;
            // 
            // numIdFrom
            // 
            this.numIdFrom.Location = new System.Drawing.Point(63, 84);
            this.numIdFrom.Name = "numIdFrom";
            this.numIdFrom.Size = new System.Drawing.Size(87, 22);
            this.numIdFrom.TabIndex = 6;
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(7, 114);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(37, 17);
            this.label2.TabIndex = 5;
            this.label2.Text = "ID to";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(7, 86);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(53, 17);
            this.label1.TabIndex = 4;
            this.label1.Text = "ID from";
            // 
            // rb29BitId
            // 
            this.rb29BitId.AutoSize = true;
            this.rb29BitId.Location = new System.Drawing.Point(7, 49);
            this.rb29BitId.Name = "rb29BitId";
            this.rb29BitId.Size = new System.Drawing.Size(81, 21);
            this.rb29BitId.TabIndex = 1;
            this.rb29BitId.Text = "29 bit ID";
            this.rb29BitId.UseVisualStyleBackColor = true;
            // 
            // rb11BitId
            // 
            this.rb11BitId.AutoSize = true;
            this.rb11BitId.Checked = true;
            this.rb11BitId.Location = new System.Drawing.Point(7, 22);
            this.rb11BitId.Name = "rb11BitId";
            this.rb11BitId.Size = new System.Drawing.Size(81, 21);
            this.rb11BitId.TabIndex = 0;
            this.rb11BitId.TabStop = true;
            this.rb11BitId.Text = "11 bit ID";
            this.rb11BitId.UseVisualStyleBackColor = true;
            // 
            // tbTrace
            // 
            this.tbTrace.BackColor = System.Drawing.SystemColors.HighlightText;
            this.tbTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbTrace.Location = new System.Drawing.Point(0, 0);
            this.tbTrace.Name = "tbTrace";
            this.tbTrace.Size = new System.Drawing.Size(155, 22);
            this.tbTrace.TabIndex = 6;
            // 
            // pnlConfig
            // 
            this.pnlConfig.Controls.Add(this.gbSettingsExtra);
            this.pnlConfig.Controls.Add(this.gbSettingsMain);
            this.pnlConfig.Controls.Add(this.btnStartStop);
            this.pnlConfig.Dock = System.Windows.Forms.DockStyle.Left;
            this.pnlConfig.Location = new System.Drawing.Point(0, 0);
            this.pnlConfig.Name = "pnlConfig";
            this.pnlConfig.Size = new System.Drawing.Size(182, 427);
            this.pnlConfig.TabIndex = 5;
            // 
            // gbSettingsExtra
            // 
            this.gbSettingsExtra.Controls.Add(this.label6);
            this.gbSettingsExtra.Controls.Add(this.tbIdCheck);
            this.gbSettingsExtra.Controls.Add(this.tbBusSleep);
            this.gbSettingsExtra.Controls.Add(this.label5);
            this.gbSettingsExtra.Controls.Add(this.label4);
            this.gbSettingsExtra.Controls.Add(this.tbData);
            this.gbSettingsExtra.Controls.Add(this.tbCount);
            this.gbSettingsExtra.Controls.Add(this.label3);
            this.gbSettingsExtra.Location = new System.Drawing.Point(10, 150);
            this.gbSettingsExtra.Name = "gbSettingsExtra";
            this.gbSettingsExtra.Size = new System.Drawing.Size(156, 175);
            this.gbSettingsExtra.TabIndex = 4;
            this.gbSettingsExtra.TabStop = false;
            this.gbSettingsExtra.Text = "Extra settings";
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(8, 152);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(62, 17);
            this.label6.TabIndex = 7;
            this.label6.Text = "Id Check";
            // 
            // tbIdCheck
            // 
            this.tbIdCheck.Location = new System.Drawing.Point(86, 149);
            this.tbIdCheck.Name = "tbIdCheck";
            this.tbIdCheck.Size = new System.Drawing.Size(65, 22);
            this.tbIdCheck.TabIndex = 6;
            // 
            // tbBusSleep
            // 
            this.tbBusSleep.Location = new System.Drawing.Point(86, 121);
            this.tbBusSleep.Name = "tbBusSleep";
            this.tbBusSleep.Size = new System.Drawing.Size(64, 22);
            this.tbBusSleep.TabIndex = 5;
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(8, 124);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(72, 17);
            this.label5.TabIndex = 4;
            this.label5.Text = "Bus Sleep";
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(7, 65);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(77, 17);
            this.label4.TabIndex = 3;
            this.label4.Text = "Data string";
            // 
            // tbData
            // 
            this.tbData.Location = new System.Drawing.Point(10, 85);
            this.tbData.Name = "tbData";
            this.tbData.Size = new System.Drawing.Size(140, 22);
            this.tbData.TabIndex = 2;
            // 
            // tbCount
            // 
            this.tbCount.Location = new System.Drawing.Point(84, 28);
            this.tbCount.Name = "tbCount";
            this.tbCount.Size = new System.Drawing.Size(65, 22);
            this.tbCount.TabIndex = 1;
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(7, 31);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(72, 17);
            this.label3.TabIndex = 0;
            this.label3.Text = "Messages";
            // 
            // panel2
            // 
            this.panel2.Controls.Add(this.tbTrace);
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(182, 0);
            this.panel2.Margin = new System.Windows.Forms.Padding(20);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(155, 427);
            this.panel2.TabIndex = 7;
            // 
            // ucActivationCodeSearcher
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.pnlConfig);
            this.Name = "ucActivationCodeSearcher";
            this.Size = new System.Drawing.Size(337, 427);
            this.gbSettingsMain.ResumeLayout(false);
            this.gbSettingsMain.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numIdTo)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numIdFrom)).EndInit();
            this.pnlConfig.ResumeLayout(false);
            this.gbSettingsExtra.ResumeLayout(false);
            this.gbSettingsExtra.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.GroupBox gbSettingsMain;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.RadioButton rb29BitId;
        private System.Windows.Forms.RadioButton rb11BitId;
        private System.Windows.Forms.TextBox tbTrace;
        private System.Windows.Forms.Panel pnlConfig;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.NumericUpDown numIdTo;
        private System.Windows.Forms.NumericUpDown numIdFrom;
        private System.Windows.Forms.GroupBox gbSettingsExtra;
        private System.Windows.Forms.TextBox tbBusSleep;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.TextBox tbData;
        private System.Windows.Forms.TextBox tbCount;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.Label label6;
        private System.Windows.Forms.TextBox tbIdCheck;
    }
}
