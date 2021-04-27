namespace canAnalyzer
{
    partial class FrmCompress
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
            this.txtPathSource = new System.Windows.Forms.TextBox();
            this.btnPathSource = new System.Windows.Forms.Button();
            this.gbSource = new System.Windows.Forms.GroupBox();
            this.gbDest = new System.Windows.Forms.GroupBox();
            this.txtPathDest = new System.Windows.Forms.TextBox();
            this.btnPathDest = new System.Windows.Forms.Button();
            this.btnAct = new System.Windows.Forms.Button();
            this.rbActCompress = new System.Windows.Forms.RadioButton();
            this.rbActDecompress = new System.Windows.Forms.RadioButton();
            this.gbAct = new System.Windows.Forms.GroupBox();
            this.pbProgress = new System.Windows.Forms.ProgressBar();
            this.gbProgress = new System.Windows.Forms.GroupBox();
            this.txtState = new System.Windows.Forms.RichTextBox();
            this.btnAbort = new System.Windows.Forms.Button();
            this.gbSource.SuspendLayout();
            this.gbDest.SuspendLayout();
            this.gbAct.SuspendLayout();
            this.gbProgress.SuspendLayout();
            this.SuspendLayout();
            // 
            // txtPathSource
            // 
            this.txtPathSource.Location = new System.Drawing.Point(20, 24);
            this.txtPathSource.Name = "txtPathSource";
            this.txtPathSource.Size = new System.Drawing.Size(350, 22);
            this.txtPathSource.TabIndex = 1;
            // 
            // btnPathSource
            // 
            this.btnPathSource.Location = new System.Drawing.Point(380, 24);
            this.btnPathSource.Name = "btnPathSource";
            this.btnPathSource.Size = new System.Drawing.Size(60, 23);
            this.btnPathSource.TabIndex = 2;
            this.btnPathSource.Text = "Path";
            this.btnPathSource.UseVisualStyleBackColor = true;
            // 
            // gbSource
            // 
            this.gbSource.Controls.Add(this.txtPathSource);
            this.gbSource.Controls.Add(this.btnPathSource);
            this.gbSource.Location = new System.Drawing.Point(12, 12);
            this.gbSource.Name = "gbSource";
            this.gbSource.Size = new System.Drawing.Size(450, 55);
            this.gbSource.TabIndex = 3;
            this.gbSource.TabStop = false;
            this.gbSource.Text = "groupBox1";
            // 
            // gbDest
            // 
            this.gbDest.Controls.Add(this.txtPathDest);
            this.gbDest.Controls.Add(this.btnPathDest);
            this.gbDest.Location = new System.Drawing.Point(12, 74);
            this.gbDest.Name = "gbDest";
            this.gbDest.Size = new System.Drawing.Size(450, 55);
            this.gbDest.TabIndex = 4;
            this.gbDest.TabStop = false;
            this.gbDest.Text = "groupBox2";
            // 
            // txtPathDest
            // 
            this.txtPathDest.Location = new System.Drawing.Point(20, 24);
            this.txtPathDest.Name = "txtPathDest";
            this.txtPathDest.Size = new System.Drawing.Size(350, 22);
            this.txtPathDest.TabIndex = 1;
            // 
            // btnPathDest
            // 
            this.btnPathDest.Location = new System.Drawing.Point(380, 24);
            this.btnPathDest.Name = "btnPathDest";
            this.btnPathDest.Size = new System.Drawing.Size(60, 23);
            this.btnPathDest.TabIndex = 2;
            this.btnPathDest.Text = "Path";
            this.btnPathDest.UseVisualStyleBackColor = true;
            // 
            // btnAct
            // 
            this.btnAct.Location = new System.Drawing.Point(12, 85);
            this.btnAct.Name = "btnAct";
            this.btnAct.Size = new System.Drawing.Size(75, 23);
            this.btnAct.TabIndex = 5;
            this.btnAct.Text = "Execute";
            this.btnAct.UseVisualStyleBackColor = true;
            // 
            // rbActCompress
            // 
            this.rbActCompress.AutoSize = true;
            this.rbActCompress.Location = new System.Drawing.Point(12, 52);
            this.rbActCompress.Name = "rbActCompress";
            this.rbActCompress.Size = new System.Drawing.Size(92, 21);
            this.rbActCompress.TabIndex = 6;
            this.rbActCompress.TabStop = true;
            this.rbActCompress.Text = "Compress";
            this.rbActCompress.UseVisualStyleBackColor = true;
            // 
            // rbActDecompress
            // 
            this.rbActDecompress.AutoSize = true;
            this.rbActDecompress.Location = new System.Drawing.Point(12, 26);
            this.rbActDecompress.Name = "rbActDecompress";
            this.rbActDecompress.Size = new System.Drawing.Size(108, 21);
            this.rbActDecompress.TabIndex = 7;
            this.rbActDecompress.TabStop = true;
            this.rbActDecompress.Text = "Decompress";
            this.rbActDecompress.UseVisualStyleBackColor = true;
            // 
            // gbAct
            // 
            this.gbAct.Controls.Add(this.rbActCompress);
            this.gbAct.Controls.Add(this.btnAct);
            this.gbAct.Controls.Add(this.rbActDecompress);
            this.gbAct.Location = new System.Drawing.Point(480, 12);
            this.gbAct.Name = "gbAct";
            this.gbAct.Size = new System.Drawing.Size(130, 117);
            this.gbAct.TabIndex = 8;
            this.gbAct.TabStop = false;
            this.gbAct.Text = "groupBox3";
            // 
            // pbProgress
            // 
            this.pbProgress.Location = new System.Drawing.Point(20, 24);
            this.pbProgress.Name = "pbProgress";
            this.pbProgress.Size = new System.Drawing.Size(350, 22);
            this.pbProgress.TabIndex = 9;
            // 
            // gbProgress
            // 
            this.gbProgress.Controls.Add(this.txtState);
            this.gbProgress.Controls.Add(this.btnAbort);
            this.gbProgress.Controls.Add(this.pbProgress);
            this.gbProgress.Location = new System.Drawing.Point(12, 136);
            this.gbProgress.Name = "gbProgress";
            this.gbProgress.Size = new System.Drawing.Size(598, 55);
            this.gbProgress.TabIndex = 10;
            this.gbProgress.TabStop = false;
            this.gbProgress.Text = "gbProgress";
            // 
            // txtState
            // 
            this.txtState.Dock = System.Windows.Forms.DockStyle.Right;
            this.txtState.Location = new System.Drawing.Point(468, 18);
            this.txtState.Margin = new System.Windows.Forms.Padding(1);
            this.txtState.Name = "txtState";
            this.txtState.Size = new System.Drawing.Size(127, 34);
            this.txtState.TabIndex = 12;
            this.txtState.Text = "";
            // 
            // btnAbort
            // 
            this.btnAbort.Location = new System.Drawing.Point(380, 24);
            this.btnAbort.Name = "btnAbort";
            this.btnAbort.Size = new System.Drawing.Size(60, 23);
            this.btnAbort.TabIndex = 11;
            this.btnAbort.Text = "Abort";
            this.btnAbort.UseVisualStyleBackColor = true;
            // 
            // FrmCompress
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(622, 203);
            this.Controls.Add(this.gbProgress);
            this.Controls.Add(this.gbAct);
            this.Controls.Add(this.gbDest);
            this.Controls.Add(this.gbSource);
            this.Name = "FrmCompress";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmCompress";
            this.gbSource.ResumeLayout(false);
            this.gbSource.PerformLayout();
            this.gbDest.ResumeLayout(false);
            this.gbDest.PerformLayout();
            this.gbAct.ResumeLayout(false);
            this.gbAct.PerformLayout();
            this.gbProgress.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.TextBox txtPathSource;
        private System.Windows.Forms.Button btnPathSource;
        private System.Windows.Forms.GroupBox gbSource;
        private System.Windows.Forms.GroupBox gbDest;
        private System.Windows.Forms.TextBox txtPathDest;
        private System.Windows.Forms.Button btnPathDest;
        private System.Windows.Forms.Button btnAct;
        private System.Windows.Forms.RadioButton rbActCompress;
        private System.Windows.Forms.RadioButton rbActDecompress;
        private System.Windows.Forms.GroupBox gbAct;
        private System.Windows.Forms.ProgressBar pbProgress;
        private System.Windows.Forms.GroupBox gbProgress;
        private System.Windows.Forms.Button btnAbort;
        private System.Windows.Forms.RichTextBox txtState;
    }
}