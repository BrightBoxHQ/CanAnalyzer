namespace canAnalyzer
{
    partial class ucTestCA
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
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnStartStop = new System.Windows.Forms.Button();
            this.lblCaInfo = new System.Windows.Forms.Label();
            this.cbCarActionList = new System.Windows.Forms.ComboBox();
            this.panel2 = new System.Windows.Forms.Panel();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.btnStartStop);
            this.panel1.Controls.Add(this.lblCaInfo);
            this.panel1.Controls.Add(this.cbCarActionList);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Top;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(635, 43);
            this.panel1.TabIndex = 12;
            // 
            // btnStartStop
            // 
            this.btnStartStop.Location = new System.Drawing.Point(5, 10);
            this.btnStartStop.Name = "btnStartStop";
            this.btnStartStop.Size = new System.Drawing.Size(75, 23);
            this.btnStartStop.TabIndex = 13;
            this.btnStartStop.Text = "Start";
            this.btnStartStop.UseVisualStyleBackColor = true;
            this.btnStartStop.Click += new System.EventHandler(this.btnStartStop_Click);
            // 
            // lblCaInfo
            // 
            this.lblCaInfo.AutoSize = true;
            this.lblCaInfo.Location = new System.Drawing.Point(281, 18);
            this.lblCaInfo.Name = "lblCaInfo";
            this.lblCaInfo.Size = new System.Drawing.Size(104, 17);
            this.lblCaInfo.TabIndex = 12;
            this.lblCaInfo.Text = "HopHeyLalaley";
            // 
            // cbCarActionList
            // 
            this.cbCarActionList.FormattingEnabled = true;
            this.cbCarActionList.Location = new System.Drawing.Point(86, 11);
            this.cbCarActionList.Name = "cbCarActionList";
            this.cbCarActionList.Size = new System.Drawing.Size(189, 24);
            this.cbCarActionList.TabIndex = 11;
            // 
            // panel2
            // 
            this.panel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel2.Location = new System.Drawing.Point(0, 43);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(635, 496);
            this.panel2.TabIndex = 13;
            // 
            // ucTestCA
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Name = "ucTestCA";
            this.Size = new System.Drawing.Size(635, 539);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.ComboBox cbCarActionList;
        private System.Windows.Forms.Label lblCaInfo;
        private System.Windows.Forms.Button btnStartStop;
        private System.Windows.Forms.Panel panel2;
    }
}
