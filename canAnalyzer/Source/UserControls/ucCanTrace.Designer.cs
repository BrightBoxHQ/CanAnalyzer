namespace canAnalyzer
{
    partial class UcCanTrace
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
            this.menu = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.grid = new System.Windows.Forms.DataGridView();
            this.tableLayoutPanel1 = new System.Windows.Forms.TableLayoutPanel();
            this.panel1 = new System.Windows.Forms.Panel();
            this.btnClear = new System.Windows.Forms.Button();
            this.cbTraceMode = new System.Windows.Forms.ComboBox();
            this.btnTrace = new System.Windows.Forms.Button();
            this.lblTotalMsgs = new System.Windows.Forms.Label();
            this.lblSendTo = new System.Windows.Forms.Label();
            this.tbPlayTo = new System.Windows.Forms.TextBox();
            this.lblSendFrom = new System.Windows.Forms.Label();
            this.tbPlayFrom = new System.Windows.Forms.TextBox();
            this.btnSendRange = new System.Windows.Forms.Button();
            this.btnSendStep = new System.Windows.Forms.Button();
            this.btnPlay = new System.Windows.Forms.Button();
            ((System.ComponentModel.ISupportInitialize)(this.grid)).BeginInit();
            this.tableLayoutPanel1.SuspendLayout();
            this.panel1.SuspendLayout();
            this.SuspendLayout();
            // 
            // menu
            // 
            this.menu.ImageScalingSize = new System.Drawing.Size(20, 20);
            this.menu.Name = "menu";
            this.menu.Size = new System.Drawing.Size(61, 4);
            this.menu.ItemClicked += new System.Windows.Forms.ToolStripItemClickedEventHandler(this.onContextMenuClicked);
            // 
            // grid
            // 
            this.grid.ColumnHeadersHeightSizeMode = System.Windows.Forms.DataGridViewColumnHeadersHeightSizeMode.AutoSize;
            this.grid.Dock = System.Windows.Forms.DockStyle.Fill;
            this.grid.Location = new System.Drawing.Point(3, 38);
            this.grid.Name = "grid";
            this.grid.RowTemplate.Height = 24;
            this.grid.Size = new System.Drawing.Size(550, 265);
            this.grid.TabIndex = 2;
            this.grid.CellContentClick += new System.Windows.Forms.DataGridViewCellEventHandler(this.grid_CellContentClick);
            // 
            // tableLayoutPanel1
            // 
            this.tableLayoutPanel1.ColumnCount = 1;
            this.tableLayoutPanel1.ColumnStyles.Add(new System.Windows.Forms.ColumnStyle(System.Windows.Forms.SizeType.Percent, 100F));
            this.tableLayoutPanel1.Controls.Add(this.panel1, 0, 0);
            this.tableLayoutPanel1.Controls.Add(this.grid, 0, 1);
            this.tableLayoutPanel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.tableLayoutPanel1.Location = new System.Drawing.Point(0, 0);
            this.tableLayoutPanel1.Margin = new System.Windows.Forms.Padding(0);
            this.tableLayoutPanel1.Name = "tableLayoutPanel1";
            this.tableLayoutPanel1.RowCount = 2;
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle(System.Windows.Forms.SizeType.Absolute, 35F));
            this.tableLayoutPanel1.RowStyles.Add(new System.Windows.Forms.RowStyle());
            this.tableLayoutPanel1.Size = new System.Drawing.Size(556, 306);
            this.tableLayoutPanel1.TabIndex = 3;
            this.tableLayoutPanel1.Paint += new System.Windows.Forms.PaintEventHandler(this.tableLayoutPanel1_Paint);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.ControlLightLight;
            this.panel1.Controls.Add(this.btnClear);
            this.panel1.Controls.Add(this.cbTraceMode);
            this.panel1.Controls.Add(this.btnTrace);
            this.panel1.Controls.Add(this.lblTotalMsgs);
            this.panel1.Controls.Add(this.lblSendTo);
            this.panel1.Controls.Add(this.tbPlayTo);
            this.panel1.Controls.Add(this.lblSendFrom);
            this.panel1.Controls.Add(this.tbPlayFrom);
            this.panel1.Controls.Add(this.btnSendRange);
            this.panel1.Controls.Add(this.btnSendStep);
            this.panel1.Controls.Add(this.btnPlay);
            this.panel1.Dock = System.Windows.Forms.DockStyle.Fill;
            this.panel1.Location = new System.Drawing.Point(0, 0);
            this.panel1.Margin = new System.Windows.Forms.Padding(0);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(556, 35);
            this.panel1.TabIndex = 3;
            // 
            // btnClear
            // 
            this.btnClear.BackColor = System.Drawing.SystemColors.Control;
            this.btnClear.Location = new System.Drawing.Point(225, 3);
            this.btnClear.Name = "btnClear";
            this.btnClear.Size = new System.Drawing.Size(28, 28);
            this.btnClear.TabIndex = 4;
            this.btnClear.UseVisualStyleBackColor = false;
            this.btnClear.Click += new System.EventHandler(this.btnClear_Click);
            // 
            // cbTraceMode
            // 
            this.cbTraceMode.FormattingEnabled = true;
            this.cbTraceMode.Location = new System.Drawing.Point(3, 5);
            this.cbTraceMode.Name = "cbTraceMode";
            this.cbTraceMode.Size = new System.Drawing.Size(85, 24);
            this.cbTraceMode.TabIndex = 0;
            this.cbTraceMode.SelectedIndexChanged += new System.EventHandler(this.cbTraceMode_SelectedIndexChanged);
            // 
            // btnTrace
            // 
            this.btnTrace.BackColor = System.Drawing.SystemColors.Menu;
            this.btnTrace.Cursor = System.Windows.Forms.Cursors.Default;
            this.btnTrace.Location = new System.Drawing.Point(95, 3);
            this.btnTrace.Margin = new System.Windows.Forms.Padding(0);
            this.btnTrace.Name = "btnTrace";
            this.btnTrace.RightToLeft = System.Windows.Forms.RightToLeft.No;
            this.btnTrace.Size = new System.Drawing.Size(28, 28);
            this.btnTrace.TabIndex = 1;
            this.btnTrace.UseVisualStyleBackColor = false;
            this.btnTrace.Click += new System.EventHandler(this.btnTraceAll_Click);
            // 
            // lblTotalMsgs
            // 
            this.lblTotalMsgs.AutoSize = true;
            this.lblTotalMsgs.Location = new System.Drawing.Point(490, 12);
            this.lblTotalMsgs.Name = "lblTotalMsgs";
            this.lblTotalMsgs.Size = new System.Drawing.Size(46, 17);
            this.lblTotalMsgs.TabIndex = 0;
            this.lblTotalMsgs.Text = "label2";
            // 
            // lblSendTo
            // 
            this.lblSendTo.AutoSize = true;
            this.lblSendTo.Location = new System.Drawing.Point(355, 12);
            this.lblSendTo.Name = "lblSendTo";
            this.lblSendTo.Size = new System.Drawing.Size(25, 17);
            this.lblSendTo.TabIndex = 0;
            this.lblSendTo.Text = "To";
            // 
            // tbPlayTo
            // 
            this.tbPlayTo.Location = new System.Drawing.Point(385, 8);
            this.tbPlayTo.Name = "tbPlayTo";
            this.tbPlayTo.Size = new System.Drawing.Size(48, 22);
            this.tbPlayTo.TabIndex = 6;
            // 
            // lblSendFrom
            // 
            this.lblSendFrom.AutoSize = true;
            this.lblSendFrom.Location = new System.Drawing.Point(260, 12);
            this.lblSendFrom.Name = "lblSendFrom";
            this.lblSendFrom.Size = new System.Drawing.Size(40, 17);
            this.lblSendFrom.TabIndex = 0;
            this.lblSendFrom.Text = "From";
            // 
            // tbPlayFrom
            // 
            this.tbPlayFrom.Location = new System.Drawing.Point(305, 8);
            this.tbPlayFrom.Name = "tbPlayFrom";
            this.tbPlayFrom.Size = new System.Drawing.Size(48, 22);
            this.tbPlayFrom.TabIndex = 5;
            // 
            // btnSendRange
            // 
            this.btnSendRange.Location = new System.Drawing.Point(440, 7);
            this.btnSendRange.Name = "btnSendRange";
            this.btnSendRange.Size = new System.Drawing.Size(50, 24);
            this.btnSendRange.TabIndex = 7;
            this.btnSendRange.Text = "Send";
            this.btnSendRange.UseVisualStyleBackColor = true;
            this.btnSendRange.Click += new System.EventHandler(this.btnSendRange_Click);
            // 
            // btnSendStep
            // 
            this.btnSendStep.BackColor = System.Drawing.SystemColors.Control;
            this.btnSendStep.Location = new System.Drawing.Point(185, 3);
            this.btnSendStep.Margin = new System.Windows.Forms.Padding(0);
            this.btnSendStep.Name = "btnSendStep";
            this.btnSendStep.Size = new System.Drawing.Size(28, 28);
            this.btnSendStep.TabIndex = 3;
            this.btnSendStep.UseVisualStyleBackColor = false;
            this.btnSendStep.Click += new System.EventHandler(this.btnSendStep_Click);
            // 
            // btnPlay
            // 
            this.btnPlay.Location = new System.Drawing.Point(145, 3);
            this.btnPlay.Margin = new System.Windows.Forms.Padding(0);
            this.btnPlay.Name = "btnPlay";
            this.btnPlay.Size = new System.Drawing.Size(28, 28);
            this.btnPlay.TabIndex = 2;
            this.btnPlay.UseVisualStyleBackColor = true;
            this.btnPlay.Click += new System.EventHandler(this.btnPlay_Click);
            // 
            // UcCanTrace
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(8F, 16F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.Controls.Add(this.tableLayoutPanel1);
            this.Name = "UcCanTrace";
            this.Size = new System.Drawing.Size(556, 306);
            ((System.ComponentModel.ISupportInitialize)(this.grid)).EndInit();
            this.tableLayoutPanel1.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.panel1.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.ContextMenuStrip menu;
        private System.Windows.Forms.DataGridView grid;
        private System.Windows.Forms.TableLayoutPanel tableLayoutPanel1;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Label lblSendTo;
        private System.Windows.Forms.TextBox tbPlayTo;
        private System.Windows.Forms.Label lblSendFrom;
        private System.Windows.Forms.TextBox tbPlayFrom;
        private System.Windows.Forms.Button btnSendRange;
        private System.Windows.Forms.Button btnSendStep;
        private System.Windows.Forms.Button btnPlay;
        private System.Windows.Forms.Label lblTotalMsgs;
        private System.Windows.Forms.Button btnTrace;
        private System.Windows.Forms.ComboBox cbTraceMode;
        private System.Windows.Forms.Button btnClear;
    }
}
