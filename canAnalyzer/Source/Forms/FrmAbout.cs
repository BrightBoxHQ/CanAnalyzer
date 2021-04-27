using System;
using System.Drawing;
using System.Windows.Forms;

/* About form */

namespace canAnalyzer
{
    public partial class FrmAbout : Form
    {
        // my email
        readonly string email = "nc@bright-box.com";

        // constuctor
        public FrmAbout()
        {
            InitializeComponent();

            // title
            Text = "About";

            // about
            txtAppName.Text = Application.ProductName;          
            txtVersion.Text = "version " + Application.ProductVersion;
            txtEmail.Text = email;
            txtCopyRight.Text = "2021";
            txtReason.Text = /*"Developed for the PoC Team"; */"From the Adoption team";
            txtReason2.Text = "with love";
            // align
            textConfig(txtAppName);
            textConfig(txtVersion);
            textConfig(txtReason);
            textConfig(txtReason2);
            textConfig(txtEmail);
            textConfig(txtCopyRight);

            txtReason.Height = 30;
        }

        // config a label
        private void textConfig (Label lbl)
        {
            const int magicNumber = 15;

            lbl.AutoSize = false;
            lbl.Margin = new Padding(0);
            lbl.TextAlign = ContentAlignment.MiddleCenter;
            lbl.Width = Width - lbl.Margin.Left - lbl.Margin.Right - magicNumber;
            lbl.Location = new Point(0, lbl.Location.Y);
        }

        // on email label clicked
        private void txtEmail_Click(object sender, EventArgs e)
        {
            //Process.Start("mailto:" + email);
            Clipboard.SetText(email);
        }

        // on key pressed
        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // close the form
            if (keyData == (Keys.Escape))
            {
                Close();
                return true;
            }
            return base.ProcessCmdKey(ref msg, keyData);
        }
    }
}
