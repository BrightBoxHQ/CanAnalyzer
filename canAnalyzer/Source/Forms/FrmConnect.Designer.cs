namespace canAnalyzer
{
    partial class FrmConnect
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
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.cbSpeed = new System.Windows.Forms.ComboBox();
            this.cbPort = new System.Windows.Forms.ComboBox();
            this.btnOk = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.cbPortFilter = new System.Windows.Forms.CheckBox();
            this.cbSilentMode = new System.Windows.Forms.CheckBox();
            this.cbTraceSave = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(20, 71);
            this.label2.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(50, 18);
            this.label2.TabIndex = 8;
            this.label2.Text = "Speed";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(20, 21);
            this.label1.Margin = new System.Windows.Forms.Padding(4, 0, 4, 0);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(36, 18);
            this.label1.TabIndex = 7;
            this.label1.Text = "Port";
            // 
            // cbSpeed
            // 
            this.cbSpeed.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbSpeed.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbSpeed.FormattingEnabled = true;
            this.cbSpeed.ItemHeight = 16;
            this.cbSpeed.Location = new System.Drawing.Point(90, 70);
            this.cbSpeed.Margin = new System.Windows.Forms.Padding(4);
            this.cbSpeed.Name = "cbSpeed";
            this.cbSpeed.Size = new System.Drawing.Size(125, 24);
            this.cbSpeed.TabIndex = 6;
            // 
            // cbPort
            // 
            this.cbPort.Cursor = System.Windows.Forms.Cursors.Default;
            this.cbPort.DropDownStyle = System.Windows.Forms.ComboBoxStyle.DropDownList;
            this.cbPort.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.cbPort.FormattingEnabled = true;
            this.cbPort.ItemHeight = 16;
            this.cbPort.Location = new System.Drawing.Point(90, 20);
            this.cbPort.Margin = new System.Windows.Forms.Padding(4);
            this.cbPort.Name = "cbPort";
            this.cbPort.Size = new System.Drawing.Size(125, 24);
            this.cbPort.TabIndex = 5;
            // 
            // btnOk
            // 
            this.btnOk.DialogResult = System.Windows.Forms.DialogResult.OK;
            this.btnOk.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnOk.Location = new System.Drawing.Point(162, 215);
            this.btnOk.Margin = new System.Windows.Forms.Padding(4);
            this.btnOk.Name = "btnOk";
            this.btnOk.Size = new System.Drawing.Size(80, 28);
            this.btnOk.TabIndex = 9;
            this.btnOk.Text = "Ok";
            this.btnOk.UseVisualStyleBackColor = true;
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Font = new System.Drawing.Font("Microsoft Sans Serif", 9F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnCancel.Location = new System.Drawing.Point(16, 215);
            this.btnCancel.Margin = new System.Windows.Forms.Padding(4);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(80, 28);
            this.btnCancel.TabIndex = 10;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            // 
            // cbPortFilter
            // 
            this.cbPortFilter.AutoSize = true;
            this.cbPortFilter.Location = new System.Drawing.Point(20, 110);
            this.cbPortFilter.Name = "cbPortFilter";
            this.cbPortFilter.Size = new System.Drawing.Size(126, 21);
            this.cbPortFilter.TabIndex = 11;
            this.cbPortFilter.Text = "FTDI ports only";
            this.cbPortFilter.UseVisualStyleBackColor = true;
            this.cbPortFilter.CheckedChanged += new System.EventHandler(this.cbPortFilter_CheckedChanged);
            // 
            // cbSilentMode
            // 
            this.cbSilentMode.AutoSize = true;
            this.cbSilentMode.Location = new System.Drawing.Point(20, 140);
            this.cbSilentMode.Name = "cbSilentMode";
            this.cbSilentMode.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.cbSilentMode.Size = new System.Drawing.Size(169, 21);
            this.cbSilentMode.TabIndex = 12;
            this.cbSilentMode.Text = "CAN Listen only mode";
            this.cbSilentMode.UseVisualStyleBackColor = true;
            this.cbSilentMode.CheckedChanged += new System.EventHandler(this.cbSilentMode_CheckedChanged);
            // 
            // cbTraceSave
            // 
            this.cbTraceSave.AutoSize = true;
            this.cbTraceSave.Location = new System.Drawing.Point(20, 170);
            this.cbTraceSave.Name = "cbTraceSave";
            this.cbTraceSave.Size = new System.Drawing.Size(157, 21);
            this.cbTraceSave.TabIndex = 13;
            this.cbTraceSave.Text = "Autosave CAN trace";
            this.cbTraceSave.UseVisualStyleBackColor = true;
            this.cbTraceSave.CheckedChanged += new System.EventHandler(this.cbTraceSave_CheckedChanged);
            // 
            // FrmConnect
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.ClientSize = new System.Drawing.Size(252, 253);
            this.Controls.Add(this.cbTraceSave);
            this.Controls.Add(this.cbSilentMode);
            this.Controls.Add(this.cbPortFilter);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnOk);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.cbSpeed);
            this.Controls.Add(this.cbPort);
            this.Font = new System.Drawing.Font("Microsoft Sans Serif", 8F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedSingle;
            this.Margin = new System.Windows.Forms.Padding(4);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmConnect";
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "Connection Settings";
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.ComboBox cbSpeed;
        private System.Windows.Forms.ComboBox cbPort;
        private System.Windows.Forms.Button btnOk;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.CheckBox cbPortFilter;
        private System.Windows.Forms.CheckBox cbSilentMode;
        private System.Windows.Forms.CheckBox cbTraceSave;
    }
}