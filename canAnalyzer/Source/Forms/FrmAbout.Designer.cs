namespace canAnalyzer
{
    partial class FrmAbout
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
            this.txtAppName = new System.Windows.Forms.Label();
            this.txtVersion = new System.Windows.Forms.Label();
            this.txtEmail = new System.Windows.Forms.Label();
            this.txtCopyRight = new System.Windows.Forms.Label();
            this.txtReason = new System.Windows.Forms.Label();
            this.txtReason2 = new System.Windows.Forms.Label();
            this.SuspendLayout();
            // 
            // txtAppName
            // 
            this.txtAppName.AutoSize = true;
            this.txtAppName.Font = new System.Drawing.Font("Microsoft Sans Serif", 14F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtAppName.ForeColor = System.Drawing.SystemColors.Window;
            this.txtAppName.Location = new System.Drawing.Point(96, 20);
            this.txtAppName.Margin = new System.Windows.Forms.Padding(0);
            this.txtAppName.Name = "txtAppName";
            this.txtAppName.Size = new System.Drawing.Size(27, 29);
            this.txtAppName.TabIndex = 0;
            this.txtAppName.Text = "1";
            this.txtAppName.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtVersion
            // 
            this.txtVersion.AutoSize = true;
            this.txtVersion.Font = new System.Drawing.Font("Microsoft Sans Serif", 10.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtVersion.ForeColor = System.Drawing.SystemColors.Window;
            this.txtVersion.Location = new System.Drawing.Point(98, 50);
            this.txtVersion.Margin = new System.Windows.Forms.Padding(0);
            this.txtVersion.Name = "txtVersion";
            this.txtVersion.Size = new System.Drawing.Size(21, 22);
            this.txtVersion.TabIndex = 1;
            this.txtVersion.Text = "2";
            this.txtVersion.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtEmail
            // 
            this.txtEmail.AutoSize = true;
            this.txtEmail.Cursor = System.Windows.Forms.Cursors.Hand;
            this.txtEmail.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Underline))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtEmail.ForeColor = System.Drawing.SystemColors.ControlLightLight;
            this.txtEmail.Location = new System.Drawing.Point(107, 175);
            this.txtEmail.Margin = new System.Windows.Forms.Padding(0);
            this.txtEmail.Name = "txtEmail";
            this.txtEmail.Size = new System.Drawing.Size(17, 18);
            this.txtEmail.TabIndex = 2;
            this.txtEmail.Text = "3";
            this.txtEmail.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            this.txtEmail.Click += new System.EventHandler(this.txtEmail_Click);
            // 
            // txtCopyRight
            // 
            this.txtCopyRight.AutoSize = true;
            this.txtCopyRight.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.5F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtCopyRight.ForeColor = System.Drawing.SystemColors.Window;
            this.txtCopyRight.Location = new System.Drawing.Point(107, 200);
            this.txtCopyRight.Margin = new System.Windows.Forms.Padding(0);
            this.txtCopyRight.Name = "txtCopyRight";
            this.txtCopyRight.Size = new System.Drawing.Size(16, 18);
            this.txtCopyRight.TabIndex = 4;
            this.txtCopyRight.Text = "4";
            this.txtCopyRight.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtReason
            // 
            this.txtReason.AutoSize = true;
            this.txtReason.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtReason.ForeColor = System.Drawing.SystemColors.Window;
            this.txtReason.Location = new System.Drawing.Point(98, 100);
            this.txtReason.Margin = new System.Windows.Forms.Padding(0);
            this.txtReason.Name = "txtReason";
            this.txtReason.Size = new System.Drawing.Size(19, 20);
            this.txtReason.TabIndex = 5;
            this.txtReason.Text = "5";
            this.txtReason.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // txtReason2
            // 
            this.txtReason2.AutoSize = true;
            this.txtReason2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9.5F, ((System.Drawing.FontStyle)((System.Drawing.FontStyle.Bold | System.Drawing.FontStyle.Italic))), System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.txtReason2.ForeColor = System.Drawing.SystemColors.Window;
            this.txtReason2.Location = new System.Drawing.Point(97, 130);
            this.txtReason2.Margin = new System.Windows.Forms.Padding(0);
            this.txtReason2.Name = "txtReason2";
            this.txtReason2.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.txtReason2.Size = new System.Drawing.Size(19, 20);
            this.txtReason2.TabIndex = 6;
            this.txtReason2.Text = "6";
            this.txtReason2.TextAlign = System.Drawing.ContentAlignment.MiddleCenter;
            // 
            // FrmAbout
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(262, 229);
            this.Controls.Add(this.txtReason2);
            this.Controls.Add(this.txtReason);
            this.Controls.Add(this.txtCopyRight);
            this.Controls.Add(this.txtEmail);
            this.Controls.Add(this.txtVersion);
            this.Controls.Add(this.txtAppName);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmAbout";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmAbout";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label txtAppName;
        private System.Windows.Forms.Label txtVersion;
        private System.Windows.Forms.Label txtEmail;
        private System.Windows.Forms.Label txtCopyRight;
        private System.Windows.Forms.Label txtReason;
        private System.Windows.Forms.Label txtReason2;
    }
}