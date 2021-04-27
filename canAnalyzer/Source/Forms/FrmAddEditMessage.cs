using System;
using System.Drawing;
using System.Windows.Forms;

namespace canAnalyzer
{
    public partial class FrmAddEditMessage : Form
    {
        #region Constructor
        public FrmAddEditMessage()
        {
            InitializeComponent();

            guiConfig();
            guiSetDefaults();
        }
        #endregion

        #region Gui Config
        private void guiConfig()
        {
            // buttons
            btnOk.DialogResult = DialogResult.OK;
            btnCancel.DialogResult = DialogResult.Cancel;

            // dlc
            numDlc.Hexadecimal = true;
            numDlc.Maximum = 8;
            numDlc.Minimum = 1;

            // can id
            numId.Hexadecimal = true;
            numId.Maximum = 0x7FF;
            numId.Value = 0;

            // period
            numTrgPeriod.Minimum = 1;
            numTrgPeriod.Maximum = 1000 * 60 * 60;
            numTrgPeriod.Hexadecimal = false;

            numMsgCount.Minimum = 1;
            numMsgCount.Maximum = 9999999;
            numMsgCount.Hexadecimal = false;

            // tb (data)
            textBoxByteConfig(tbD0);
            textBoxByteConfig(tbD1);
            textBoxByteConfig(tbD2);
            textBoxByteConfig(tbD3);
            textBoxByteConfig(tbD4);
            textBoxByteConfig(tbD5);
            textBoxByteConfig(tbD6);
            textBoxByteConfig(tbD7);
            // tb (trigger)
            textBoxByteConfig(tbStartD0, "**");
            textBoxByteConfig(tbStartD1, "**");
            textBoxByteConfig(tbStartD2, "**");
            textBoxByteConfig(tbStartD3, "**");
            textBoxByteConfig(tbStartD4, "**");
            textBoxByteConfig(tbStartD5, "**");
            textBoxByteConfig(tbStartD6, "**");
            textBoxByteConfig(tbStartD7, "**");
            // numboxes
            numBoxStyleConfig(numDlc);
            numBoxStyleConfig(numId);
            numBoxStyleConfig(numTrgId);
            numBoxStyleConfig(numTrgPeriod);
            numBoxStyleConfig(numMsgCount);

            numId.Font = new Font("Consolas", 9.0f, FontStyle.Italic);
            // radio
            rbId11b.CheckedChanged += rbId_CheckedChanged;
            rbId29b.CheckedChanged += rbId_CheckedChanged;
            rbFrameData.CheckedChanged += rbFrame_CheckedChanged;
            rbFrameDtr.CheckedChanged += rbFrame_CheckedChanged;
            rbTrgData.CheckedChanged += rbStartTrig_CheckedChanged;
            rbTrgTime.CheckedChanged += rbStartTrig_CheckedChanged;
            rbCount.CheckedChanged += rbStopTrig_CheckedChanged;
            rbCountUnlim.CheckedChanged += rbStopTrig_CheckedChanged;
            rbModifyDo.CheckedChanged += rbModify_CheckedChanged;
            rbModifyStop.CheckedChanged += rbModify_CheckedChanged;
        }

        private void guiSetDefaults()
        {
            // default values
            rbId11b.Checked = true;
            rbFrameData.Checked = true;
            rbTrgTime.Checked = true;

            numDlc.Value = 8;
            numTrgPeriod.Value = 1000;

            updateNumIdMaximum();
            updateStartTriggerGroup();
            updateStopTriggerGroup();
            updateModifyGroup();
        }
        #endregion


        private void updateNumIdMaximum()
        {
            numId.Maximum = canMessage.idMax(rbId29b.Checked);
        }

        private void updateStartTriggerGroup()
        {
            bool dataEn = rbTrgData.Checked;
            tbStartD0.Enabled = dataEn;
            tbStartD1.Enabled = dataEn;
            tbStartD2.Enabled = dataEn;
            tbStartD3.Enabled = dataEn;
            tbStartD4.Enabled = dataEn;
            tbStartD5.Enabled = dataEn;
            tbStartD6.Enabled = dataEn;
            tbStartD7.Enabled = dataEn;

            numTrgId.Enabled = rbTrgData.Checked;
            numTrgPeriod.Enabled = rbTrgTime.Checked;
        }

        private void updateStopTriggerGroup()
        {
            numMsgCount.Enabled = rbCount.Checked;
            gbModify.Enabled = rbCount.Checked;
        }

        private void updateModifyGroup()
        {
            pnlIncrement.Enabled = gbModify.Enabled && rbModifyDo.Checked;
        }

        private void textBoxByteConfig(TextBox box, string defaultValue = "00")
        {
            if (defaultValue == "00")               // handle
                box.KeyPress += textBoxByteKeyPress;
            else
                box.KeyPress += textBoxByteKeyPressTrigger;

            box.MaxLength = 2;                      // max len
            box.ShortcutsEnabled = false;           // menu disable
            box.Text = defaultValue;                // default
            box.TextAlign = HorizontalAlignment.Center;

            box.Font = new Font("Consolas", 9.0f, FontStyle.Italic);
        }

        private void numBoxStyleConfig(NumericUpDown num)
        {
            num.TextAlign = HorizontalAlignment.Right;
            num.Font = new Font("Calibri", 10.0f);
        }


        private void numDlc_ValueChanged(object sender, EventArgs e)
        {
            int dlc = int.Parse(numDlc.Value.ToString());

            // set enable/disable
            tbD0.Enabled = dlc >= 1;
            tbD1.Enabled = dlc >= 2;
            tbD2.Enabled = dlc >= 3;
            tbD3.Enabled = dlc >= 4;
            tbD4.Enabled = dlc >= 5;
            tbD5.Enabled = dlc >= 6;
            tbD6.Enabled = dlc >= 7;
            tbD7.Enabled = dlc >= 8;

            tbSetTextColor(tbD0);
            tbSetTextColor(tbD1);
            tbSetTextColor(tbD2);
            tbSetTextColor(tbD3);
            tbSetTextColor(tbD4);
            tbSetTextColor(tbD5);
            tbSetTextColor(tbD6);
            tbSetTextColor(tbD7);
        }

        private void tbSetTextColor(TextBox tb)
        {
            if (tb.Enabled && string.IsNullOrEmpty(tb.Text))
                tb.Text = "00";

            if (tb.Enabled == false)
                tb.Text = string.Empty;
        }



        #region GUI to Data 

        // get can message
        private canMessage2 getMessage()
        {
            int id = (int)numId.Value;
            int dlc = (int)numDlc.Value;
            bool is29bitId = rbId29b.Checked;
            byte[] data = new byte[dlc];

            int i = 0;
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD0.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD1.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD2.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD3.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD4.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD5.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD6.Text, 16);
            if (dlc >= i + 1)
                data[i++] = Convert.ToByte(tbD7.Text, 16);

            canMessage2 msg = new canMessage2(id, is29bitId, data, 0 );

            return msg;
        }

        private int getInterval()
        {
            return (int)numTrgPeriod.Value;
        }

        private canTriggerStart getStartTrigger()
        {
            return rbTrgData.Checked == true ? canTriggerStart.data : canTriggerStart.timer;
        }

        private canTriggerStop getStopTrigger()
        {
            return rbCountUnlim.Checked == true ? canTriggerStop.doNotStop : canTriggerStop.counter;
        }

        private int getMessageCount ()
        {
            return (int)numMsgCount.Value;
        }

        private paramActAfterStop getParamAfterStop()
        {
            return rbModifyStop.Checked == true ? paramActAfterStop.stop : paramActAfterStop.modifyAndRestart;
        }

        private canMessageType geMessageType()
        {
            return rbFrameData.Checked == true ? canMessageType.data : canMessageType.rtr;
        }

        private paramModifiers getModifiers()
        {
            paramModifiers mod = new paramModifiers();

            mod.modId = cbIncId.Checked;
            mod.modB0 = cbIncB0.Checked;
            mod.modB1 = cbIncB1.Checked;
            mod.modB2 = cbIncB2.Checked;
            mod.modB3 = cbIncB3.Checked;
            mod.modB4 = cbIncB4.Checked;
            mod.modB5 = cbIncB5.Checked;
            mod.modB6 = cbIncB6.Checked;
            mod.modB7 = cbIncB7.Checked;

            return mod;
        }

        public messageSendParams getParams()
        {
            messageSendParams param = new messageSendParams();

            // message
            param.Message = getMessage();
            param.MessageCount = getMessageCount();
            // timer
            param.TimerPeriod = getInterval();
            // triggers
            param.TriggerStart = getStartTrigger();
            param.TriggerStop = getStopTrigger();

            param.PostCondition = getParamAfterStop();
            param.MessageType = geMessageType();
            param.Modifiers = getModifiers();

            return param;
        }
        #endregion

        // data text box callbacks
        private void textBoxByteKeyPress(object sender, KeyPressEventArgs e)
        {
            char ch = char.ToUpper(e.KeyChar);
            if (Tools.isHex(ch) || '\b' == ch)
                e.KeyChar = ch;
            else
                e.Handled = true;
        }

        private void textBoxByteKeyPressTrigger(object sender, KeyPressEventArgs e)
        {
            char ch = char.ToUpper(e.KeyChar);
            if (Tools.isHex(ch) || '*' == ch || '\b' == ch)
                e.KeyChar = ch;
            else
                e.Handled = true;
        }

        // radio box callbacks
        //
        private void rbFrame_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            gbData.Enabled = rb == rbFrameData;
        }

        private void rbId_CheckedChanged(object sender, EventArgs e)
        {
            updateNumIdMaximum();
        }

        private void rbStartTrig_CheckedChanged(object sender, EventArgs e)
        {
            updateStartTriggerGroup();
        }

        private void rbStopTrig_CheckedChanged(object sender, EventArgs e)
        {
            updateStopTriggerGroup();
        }

        private void rbModify_CheckedChanged(object sender, EventArgs e)
        {
            updateModifyGroup();
        }
    }






    // start trigger
    public enum canTriggerStart
    {
        timer,
        data
    }

    // stop trigger
    public enum canTriggerStop
    {
        counter,
        doNotStop
    }

    public enum canMessageType
    {
        data,
        rtr
    }

    public enum paramActAfterStop
    {
        stop,
        modifyAndRestart
    }

    public class paramModifiers
    {
        #region Fields
        public bool modId { set; get; }
        public bool modB0 { set { modB[0] = value; } get { return modB[0]; } }
        public bool modB1 { set { modB[1] = value; } get { return modB[1]; } }
        public bool modB2 { set { modB[2] = value; } get { return modB[2]; } }
        public bool modB3 { set { modB[3] = value; } get { return modB[3]; } }
        public bool modB4 { set { modB[4] = value; } get { return modB[4]; } }
        public bool modB5 { set { modB[5] = value; } get { return modB[5]; } }
        public bool modB6 { set { modB[6] = value; } get { return modB[6]; } }
        public bool modB7 { set { modB[7] = value; } get { return modB[7]; } }

        public bool[] modB { set; get; }
        #endregion

        #region Constructor
        public paramModifiers()
        {
            modB = new bool[canMessage.maxDataBytesNum()];            
            modB0 = false;
            modB1 = false;
            modB2 = false;
            modB3 = false;
            modB4 = false;
            modB5 = false;
            modB6 = false;
            modB7 = false;

            modId = false;
        }
        #endregion

        public bool enabled()
        {
            foreach (bool b in modB)
                if (b)
                    return true;

            return modId;
        }
    }

    public class messageSendParams
    {
        // triggers & conditions
        public canTriggerStart      TriggerStart { set; get; }
        public canTriggerStop       TriggerStop { set; get; }
        public paramActAfterStop    PostCondition { set; get; }

        // message
        public canMessageType       MessageType { set; get; }
        public canMessage2          Message { set; get; }

        public paramModifiers       Modifiers { set; get; } 

        public int TimerPeriod { set; get; } 
        public int MessageCount { set; get; }

        public messageSendParams()
        {
            setDefaults();
        }

        private void setDefaults()
        {
            TriggerStart = canTriggerStart.timer;
            TriggerStop = canTriggerStop.doNotStop;
            PostCondition = paramActAfterStop.stop;
            MessageType = canMessageType.data;
            Message = new canMessage2();
            Modifiers = new paramModifiers();
            TimerPeriod = 1000;
            MessageCount = 1;
        }
    }

}
