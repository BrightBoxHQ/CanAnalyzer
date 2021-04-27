namespace canAnalyzer
{
    partial class FrmToolValFinder
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
            this.components = new System.ComponentModel.Container();
            this.btnSrchVal = new System.Windows.Forms.Button();
            this.gbSearchVal = new System.Windows.Forms.GroupBox();
            this.tbValue = new System.Windows.Forms.TextBox();
            this.lblVal = new System.Windows.Forms.Label();
            this.gbSearchRange = new System.Windows.Forms.GroupBox();
            this.lblTo = new System.Windows.Forms.Label();
            this.tbValTo = new System.Windows.Forms.TextBox();
            this.tbValFrom = new System.Windows.Forms.TextBox();
            this.lblFrom = new System.Windows.Forms.Label();
            this.btnSrchRange = new System.Windows.Forms.Button();
            this.grid = new System.Windows.Forms.DataGridView();
            this.panel2 = new System.Windows.Forms.Panel();
            this.gbSettings = new System.Windows.Forms.GroupBox();
            this.cbUseFactorDiv = new System.Windows.Forms.CheckBox();
            this.cbUseFactorMul = new System.Windows.Forms.CheckBox();
            this.cbBigEndian = new System.Windows.Forms.CheckBox();
            this.cbLittleEndian = new System.Windows.Forms.CheckBox();
            this.lblFactors = new System.Windows.Forms.Label();
            this.cbUseFactors = new System.Windows.Forms.CheckBox();
            this.tbFactors = new System.Windows.Forms.TextBox();
            this.gbMixMaxBytes = new System.Windows.Forms.GroupBox();
            this.cbMaxBytes = new System.Windows.Forms.ComboBox();
            this.lblMaxBytes = new System.Windows.Forms.Label();
            this.cbMinBytes = new System.Windows.Forms.ComboBox();
            this.lblMinBytes = new System.Windows.Forms.Label();
            this.cMenu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.copyToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.pasteToolStripMenuItem = new System.Windows.Forms.ToolStripMenuItem();
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.gbSearchVal.SuspendLayout();
            this.gbSearchRange.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.panel2.SuspendLayout();
            this.gbSettings.SuspendLayout();
            this.gbMixMaxBytes.SuspendLayout();
            this.cMenu.SuspendLayout();
            this.SuspendLayout();
            // 
            // btnSrchVal
            // 
            this.btnSrchVal.BackColor = System.Drawing.SystemColors.ButtonFace;
            this.btnSrchVal.BackgroundImageLayout = System.Windows.Forms.ImageLayout.None;
            this.btnSrchVal.ForeColor = System.Drawing.SystemColors.ControlText;
            this.btnSrchVal.Location = new System.Drawing.Point(105, 107);
            this.btnSrchVal.Name = "btnSrchVal";
            this.btnSrchVal.Size = new System.Drawing.Size(75, 23);
            this.btnSrchVal.TabIndex = 1;
            this.btnSrchVal.Text = "button1";
            this.btnSrchVal.UseVisualStyleBackColor = true;
            this.btnSrchVal.Click += new System.EventHandler(this.btnSrchVal_Click);
            // 
            // gbSearchVal
            // 
            this.gbSearchVal.Controls.Add(this.tbValue);
            this.gbSearchVal.Controls.Add(this.lblVal);
            this.gbSearchVal.Controls.Add(this.btnSrchVal);
            this.gbSearchVal.Location = new System.Drawing.Point(10, 10);
            this.gbSearchVal.Name = "gbSearchVal";
            this.gbSearchVal.Size = new System.Drawing.Size(195, 135);
            this.gbSearchVal.TabIndex = 9;
            this.gbSearchVal.TabStop = false;
            this.gbSearchVal.Text = "groupBox1";
            // 
            // tbValue
            // 
            this.tbValue.Location = new System.Drawing.Point(60, 27);
            this.tbValue.Name = "tbValue";
            this.tbValue.Size = new System.Drawing.Size(120, 22);
            this.tbValue.TabIndex = 0;
            // 
            // lblVal
            // 
            this.lblVal.AutoSize = true;
            this.lblVal.Location = new System.Drawing.Point(10, 30);
            this.lblVal.Name = "lblVal";
            this.lblVal.Size = new System.Drawing.Size(46, 17);
            this.lblVal.TabIndex = 0;
            this.lblVal.Text = "label1";
            // 
            // gbSearchRange
            // 
            this.gbSearchRange.Controls.Add(this.lblTo);
            this.gbSearchRange.Controls.Add(this.tbValTo);
            this.gbSearchRange.Controls.Add(this.tbValFrom);
            this.gbSearchRange.Controls.Add(this.lblFrom);
            this.gbSearchRange.Controls.Add(this.btnSrchRange);
            this.gbSearchRange.Location = new System.Drawing.Point(220, 10);
            this.gbSearchRange.Name = "gbSearchRange";
            this.gbSearchRange.Size = new System.Drawing.Size(195, 135);
            this.gbSearchRange.TabIndex = 10;
            this.gbSearchRange.TabStop = false;
            this.gbSearchRange.Text = "groupBox2";
            // 
            // lblTo
            // 
            this.lblTo.AutoSize = true;
            this.lblTo.Location = new System.Drawing.Point(10, 65);
            this.lblTo.Name = "lblTo";
            this.lblTo.Size = new System.Drawing.Size(46, 17);
            this.lblTo.TabIndex = 6;
            this.lblTo.Text = "label3";
            // 
            // tbValTo
            // 
            this.tbValTo.Location = new System.Drawing.Point(60, 62);
            this.tbValTo.Name = "tbValTo";
            this.tbValTo.Size = new System.Drawing.Size(114, 22);
            this.tbValTo.TabIndex = 3;
            // 
            // tbValFrom
            // 
            this.tbValFrom.Location = new System.Drawing.Point(60, 27);
            this.tbValFrom.Name = "tbValFrom";
            this.tbValFrom.Size = new System.Drawing.Size(114, 22);
            this.tbValFrom.TabIndex = 2;
            // 
            // lblFrom
            // 
            this.lblFrom.AutoSize = true;
            this.lblFrom.Location = new System.Drawing.Point(10, 30);
            this.lblFrom.Name = "lblFrom";
            this.lblFrom.Size = new System.Drawing.Size(46, 17);
            this.lblFrom.TabIndex = 0;
            this.lblFrom.Text = "label2";
            // 
            // btnSrchRange
            // 
            this.btnSrchRange.Location = new System.Drawing.Point(99, 107);
            this.btnSrchRange.Name = "btnSrchRange";
            this.btnSrchRange.Size = new System.Drawing.Size(75, 23);
            this.btnSrchRange.TabIndex = 4;
            this.btnSrchRange.Text = "button1";
            this.btnSrchRange.UseVisualStyleBackColor = true;
            this.btnSrchRange.Click += new System.EventHandler(this.btnSrchRange_Click);
            // 
            // grid
            // 
            this.grid.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.grid.BackgroundColor = System.Drawing.SystemColors.ButtonHighlight;
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Location = new System.Drawing.Point(5, 158);
            this.grid.Margin = new System.Windows.Forms.Padding(0);
            this.grid.Name = "grid";
            this.grid.ReadOnly = true;
            this.grid.RowTemplate.Height = 24;
            this.grid.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.grid.Size = new System.Drawing.Size(822, 290);
            this.grid.TabIndex = 11;
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.panel2.Controls.Add(this.gbSettings);
            this.panel2.Controls.Add(this.gbMixMaxBytes);
            this.panel2.Controls.Add(this.gbSearchRange);
            this.panel2.Controls.Add(this.gbSearchVal);
            this.panel2.Location = new System.Drawing.Point(0, 0);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(832, 155);
            this.panel2.TabIndex = 12;
            // 
            // gbSettings
            // 
            this.gbSettings.Controls.Add(this.cbUseFactorDiv);
            this.gbSettings.Controls.Add(this.cbUseFactorMul);
            this.gbSettings.Controls.Add(this.cbBigEndian);
            this.gbSettings.Controls.Add(this.cbLittleEndian);
            this.gbSettings.Controls.Add(this.lblFactors);
            this.gbSettings.Controls.Add(this.cbUseFactors);
            this.gbSettings.Controls.Add(this.tbFactors);
            this.gbSettings.Location = new System.Drawing.Point(615, 10);
            this.gbSettings.Name = "gbSettings";
            this.gbSettings.Size = new System.Drawing.Size(205, 135);
            this.gbSettings.TabIndex = 12;
            this.gbSettings.TabStop = false;
            this.gbSettings.Text = "groupBox1";
            // 
            // cbUseFactorDiv
            // 
            this.cbUseFactorDiv.AutoSize = true;
            this.cbUseFactorDiv.Location = new System.Drawing.Point(165, 75);
            this.cbUseFactorDiv.Name = "cbUseFactorDiv";
            this.cbUseFactorDiv.Size = new System.Drawing.Size(34, 21);
            this.cbUseFactorDiv.TabIndex = 6;
            this.cbUseFactorDiv.Text = "/";
            this.cbUseFactorDiv.UseVisualStyleBackColor = true;
            // 
            // cbUseFactorMul
            // 
            this.cbUseFactorMul.AutoSize = true;
            this.cbUseFactorMul.Location = new System.Drawing.Point(130, 75);
            this.cbUseFactorMul.Name = "cbUseFactorMul";
            this.cbUseFactorMul.Size = new System.Drawing.Size(36, 21);
            this.cbUseFactorMul.TabIndex = 5;
            this.cbUseFactorMul.Text = "x";
            this.cbUseFactorMul.UseVisualStyleBackColor = true;
            // 
            // cbBigEndian
            // 
            this.cbBigEndian.AutoSize = true;
            this.cbBigEndian.Location = new System.Drawing.Point(9, 48);
            this.cbBigEndian.Name = "cbBigEndian";
            this.cbBigEndian.Size = new System.Drawing.Size(98, 21);
            this.cbBigEndian.TabIndex = 4;
            this.cbBigEndian.Text = "checkBox2";
            this.cbBigEndian.UseVisualStyleBackColor = true;
            // 
            // cbLittleEndian
            // 
            this.cbLittleEndian.AutoSize = true;
            this.cbLittleEndian.Location = new System.Drawing.Point(9, 21);
            this.cbLittleEndian.Name = "cbLittleEndian";
            this.cbLittleEndian.Size = new System.Drawing.Size(98, 21);
            this.cbLittleEndian.TabIndex = 3;
            this.cbLittleEndian.Text = "checkBox1";
            this.cbLittleEndian.UseVisualStyleBackColor = true;
            // 
            // lblFactors
            // 
            this.lblFactors.AutoSize = true;
            this.lblFactors.Location = new System.Drawing.Point(6, 110);
            this.lblFactors.Name = "lblFactors";
            this.lblFactors.Size = new System.Drawing.Size(46, 17);
            this.lblFactors.TabIndex = 2;
            this.lblFactors.Text = "label1";
            // 
            // cbUseFactors
            // 
            this.cbUseFactors.AutoSize = true;
            this.cbUseFactors.Location = new System.Drawing.Point(9, 75);
            this.cbUseFactors.Name = "cbUseFactors";
            this.cbUseFactors.Size = new System.Drawing.Size(98, 21);
            this.cbUseFactors.TabIndex = 1;
            this.cbUseFactors.Text = "checkBox1";
            this.cbUseFactors.UseVisualStyleBackColor = true;
            // 
            // tbFactors
            // 
            this.tbFactors.Location = new System.Drawing.Point(82, 107);
            this.tbFactors.Name = "tbFactors";
            this.tbFactors.Size = new System.Drawing.Size(117, 22);
            this.tbFactors.TabIndex = 0;
            // 
            // gbMixMaxBytes
            // 
            this.gbMixMaxBytes.Controls.Add(this.cbMaxBytes);
            this.gbMixMaxBytes.Controls.Add(this.lblMaxBytes);
            this.gbMixMaxBytes.Controls.Add(this.cbMinBytes);
            this.gbMixMaxBytes.Controls.Add(this.lblMinBytes);
            this.gbMixMaxBytes.Location = new System.Drawing.Point(430, 10);
            this.gbMixMaxBytes.Name = "gbMixMaxBytes";
            this.gbMixMaxBytes.Size = new System.Drawing.Size(170, 135);
            this.gbMixMaxBytes.TabIndex = 11;
            this.gbMixMaxBytes.TabStop = false;
            this.gbMixMaxBytes.Text = "groupBox2";
            // 
            // cbMaxBytes
            // 
            this.cbMaxBytes.FormattingEnabled = true;
            this.cbMaxBytes.Location = new System.Drawing.Point(65, 103);
            this.cbMaxBytes.Name = "cbMaxBytes";
            this.cbMaxBytes.Size = new System.Drawing.Size(86, 24);
            this.cbMaxBytes.TabIndex = 12;
            // 
            // lblMaxBytes
            // 
            this.lblMaxBytes.AutoSize = true;
            this.lblMaxBytes.Location = new System.Drawing.Point(6, 106);
            this.lblMaxBytes.Name = "lblMaxBytes";
            this.lblMaxBytes.Size = new System.Drawing.Size(46, 17);
            this.lblMaxBytes.TabIndex = 6;
            this.lblMaxBytes.Text = "label3";
            // 
            // cbMinBytes
            // 
            this.cbMinBytes.FormattingEnabled = true;
            this.cbMinBytes.Location = new System.Drawing.Point(65, 62);
            this.cbMinBytes.Name = "cbMinBytes";
            this.cbMinBytes.Size = new System.Drawing.Size(86, 24);
            this.cbMinBytes.TabIndex = 11;
            // 
            // lblMinBytes
            // 
            this.lblMinBytes.AutoSize = true;
            this.lblMinBytes.Location = new System.Drawing.Point(6, 65);
            this.lblMinBytes.Name = "lblMinBytes";
            this.lblMinBytes.Size = new System.Drawing.Size(46, 17);
            this.lblMinBytes.TabIndex = 7;
            this.lblMinBytes.Text = "label2";
            // 
            // cMenu
            // 
            this.cMenu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.cMenu.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.copyToolStripMenuItem,
            this.pasteToolStripMenuItem});
            this.cMenu.Name = "cMenu";
            this.cMenu.Size = new System.Drawing.Size(113, 52);
            // 
            // copyToolStripMenuItem
            // 
            this.copyToolStripMenuItem.Name = "copyToolStripMenuItem";
            this.copyToolStripMenuItem.Size = new System.Drawing.Size(112, 24);
            this.copyToolStripMenuItem.Text = "Copy";
            // 
            // pasteToolStripMenuItem
            // 
            this.pasteToolStripMenuItem.Name = "pasteToolStripMenuItem";
            this.pasteToolStripMenuItem.Size = new System.Drawing.Size(112, 24);
            this.pasteToolStripMenuItem.Text = "Paste";
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(61, 4);
            // 
            // FrmToolValFinder
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.BackColor = System.Drawing.SystemColors.ActiveCaption;
            this.ClientSize = new System.Drawing.Size(832, 453);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.grid);
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "FrmToolValFinder";
            this.Padding = new System.Windows.Forms.Padding(5);
            this.ShowIcon = false;
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "FrmToolValFinder";
            this.Load += new System.EventHandler(this.FrmToolValFinder_Load);
            this.gbSearchVal.ResumeLayout(false);
            this.gbSearchVal.PerformLayout();
            this.gbSearchRange.ResumeLayout(false);
            this.gbSearchRange.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.panel2.ResumeLayout(false);
            this.gbSettings.ResumeLayout(false);
            this.gbSettings.PerformLayout();
            this.gbMixMaxBytes.ResumeLayout(false);
            this.gbMixMaxBytes.PerformLayout();
            this.cMenu.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button btnSrchVal;
        private System.Windows.Forms.GroupBox gbSearchVal;
        private System.Windows.Forms.TextBox tbValue;
        private System.Windows.Forms.Label lblVal;
        private System.Windows.Forms.GroupBox gbSearchRange;
        private System.Windows.Forms.Label lblTo;
        private System.Windows.Forms.TextBox tbValTo;
        private System.Windows.Forms.TextBox tbValFrom;
        private System.Windows.Forms.Label lblFrom;
        private System.Windows.Forms.Button btnSrchRange;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Label lblMinBytes;
        private System.Windows.Forms.ComboBox cbMinBytes;
        private System.Windows.Forms.GroupBox gbMixMaxBytes;
        private System.Windows.Forms.ComboBox cbMaxBytes;
        private System.Windows.Forms.Label lblMaxBytes;
        private System.Windows.Forms.ContextMenuStrip cMenu;
        private System.Windows.Forms.ToolStripMenuItem copyToolStripMenuItem;
        private System.Windows.Forms.ToolStripMenuItem pasteToolStripMenuItem;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.GroupBox gbSettings;
        private System.Windows.Forms.Label lblFactors;
        private System.Windows.Forms.CheckBox cbUseFactors;
        private System.Windows.Forms.TextBox tbFactors;
        private System.Windows.Forms.CheckBox cbLittleEndian;
        private System.Windows.Forms.CheckBox cbBigEndian;
        private System.Windows.Forms.CheckBox cbUseFactorMul;
        private System.Windows.Forms.CheckBox cbUseFactorDiv;
    }
}