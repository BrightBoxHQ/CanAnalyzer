namespace canAnalyzer
{
    partial class ucScript
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
            this.tbScript = new System.Windows.Forms.TextBox();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.tableLayoutPanel2 = new System.Windows.Forms.TableLayoutPanel();
            this.btnCleanTrace = new System.Windows.Forms.Button();
            this.btnContinue = new System.Windows.Forms.Button();
            this.btnStart = new System.Windows.Forms.Button();
            this.tbTrace = new System.Windows.Forms.RichTextBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.cbEnableTrace = new System.Windows.Forms.CheckBox();
            this.cbRestartScript = new System.Windows.Forms.CheckBox();
            this.tableLayoutPanel1.SuspendLayout();
            this.tableLayoutPanel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // tbScript
            // 
            this.tbScript.Location = new System.Drawing.Point(3, 3);
            this.tbScript.Name = "tbScript";
            this.tbScript.Size = new System.Drawing.Size(100, 22);
            this.tbScript.TabIndex = 0;
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 2;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 57.81638F));
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 42.18362F));
            this.tableLayoutPanel1.Controls.Add(this.tableLayoutPanel2, 0, 1);
            this.tableLayoutPanel1.Controls.Add(this.tbScript, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.tbTrace, 1, 0);
            this.tableLayoutPanel1.Controls.Add(this.panel1, 1, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 30F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 20F));
            this.tableLayoutPanel1.Size = new System.Drawing.Size(631, 400);
            this.tableLayoutPanel1.TabIndex = 2;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // tableLayoutPanel2
            // 
            this.tableLayoutPanel2.ColumnCount = 3;
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 30F));
            this.tableLayoutPanel2.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 40F));
            this.tableLayoutPanel2.Controls.Add(this.btnCleanTrace, 2, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnContinue, 0, 0);
            this.tableLayoutPanel2.Controls.Add(this.btnStart, 1, 0);
            this.tableLayoutPanel2.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel2.Location = new System.Drawing.Point(0, 370);
            this.tableLayoutPanel2.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel2.Name = "tableLayoutPanel2";
            this.tableLayoutPanel2.RowCount = 1;
            this.tableLayoutPanel2.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel2.Size = new System.Drawing.Size(364, 30);
            this.tableLayoutPanel2.TabIndex = 3;
            // 
            // btnCleanTrace
            // 
            this.btnCleanTrace.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnCleanTrace.Location = new System.Drawing.Point(236, 3);
            this.btnCleanTrace.Name = "btnCleanTrace";
            this.btnCleanTrace.Size = new System.Drawing.Size(125, 24);
            this.btnCleanTrace.TabIndex = 6;
            this.btnCleanTrace.Text = "Clean Trace";
            this.btnCleanTrace.UseVisualStyleBackColor = true;
            this.btnCleanTrace.Click += new System.EventHandler(this.btnCleanTrace_Click);
            // 
            // btnContinue
            // 
            this.btnContinue.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnContinue.Location = new System.Drawing.Point(17, 3);
            this.btnContinue.Name = "btnContinue";
            this.btnContinue.Size = new System.Drawing.Size(89, 24);
            this.btnContinue.TabIndex = 4;
            this.btnContinue.Text = "Continue";
            this.btnContinue.UseVisualStyleBackColor = true;
            // 
            // btnStart
            // 
            this.btnStart.Dock = System.Windows.Forms.DockStyle.Right;
            this.btnStart.Location = new System.Drawing.Point(126, 3);
            this.btnStart.Name = "btnStart";
            this.btnStart.Size = new System.Drawing.Size(89, 24);
            this.btnStart.TabIndex = 5;
            this.btnStart.Text = "Execute";
            this.btnStart.UseVisualStyleBackColor = true;
            // 
            // tbTrace
            // 
            this.tbTrace.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tbTrace.Location = new System.Drawing.Point(367, 3);
            this.tbTrace.Name = "tbTrace";
            this.tbTrace.Size = new System.Drawing.Size(261, 364);
            this.tbTrace.TabIndex = 4;
            this.tbTrace.Text = "";
            // 
            // panel1
            // 
            this.panel1.Controls.Add(this.cbEnableTrace);
            this.panel1.Controls.Add(this.cbRestartScript);
            this.panel1.Location = new System.Drawing.Point(364, 370);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(261, 30);
            this.panel1.TabIndex = 5;
            // 
            // cbEnableTrace
            // 
            this.cbEnableTrace.AutoSize = true;
            this.cbEnableTrace.Location = new System.Drawing.Point(125, 3);
            this.cbEnableTrace.Name = "cbEnableTrace";
            this.cbEnableTrace.Size = new System.Drawing.Size(115, 21);
            this.cbEnableTrace.TabIndex = 6;
            this.cbEnableTrace.Text = "Enable Trace";
            this.cbEnableTrace.UseVisualStyleBackColor = true;
            this.cbEnableTrace.CheckedChanged += new System.EventHandler(this.cbEnableTrace_CheckedChanged);
            // 
            // cbRestartScript
            // 
            this.cbRestartScript.AutoSize = true;
            this.cbRestartScript.Location = new System.Drawing.Point(3, 3);
            this.cbRestartScript.Name = "cbRestartScript";
            this.cbRestartScript.Size = new System.Drawing.Size(116, 21);
            this.cbRestartScript.TabIndex = 5;
            this.cbRestartScript.Text = "Restart Script";
            this.cbRestartScript.UseVisualStyleBackColor = true;
            // 
            // ucScript
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "ucScript";
            this.Size = new System.Drawing.Size(631, 400);
            this.tableLayoutPanel1.ResumeLayout(false);
            this.tableLayoutPanel1.PerformLayout();
            this.tableLayoutPanel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.TextBox tbScript;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Button btnContinue;
        private System.Windows.Forms.Button btnStart;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel2;
        private System.Windows.Forms.RichTextBox tbTrace;
        private System.Windows.Forms.CheckBox cbRestartScript;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.CheckBox cbEnableTrace;
        private System.Windows.Forms.Button btnCleanTrace;
    }
}
