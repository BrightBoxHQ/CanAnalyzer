using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;
using canSerialPort;

namespace canAnalyzer
{
    public partial class FrmConnect : Form
    {
        #region Fields
        private System.Windows.Forms.Timer tmrPortScan;
        private readonly FrmMain m_parent;
        private bool m_ftdi_ports_only = true;
        private bool m_is_can_silent_mode = false;
        private bool m_is_trace_autosave_en = false;
        public ConnSettingsCan Settings { get; set; }
        #endregion

        #region Constructors
        // constructor
        public FrmConnect(FrmMain parent)
        {            
            InitializeComponent();

            // set the title string
            Text = "Connection Settings";

            // fields
            m_parent = parent;
            Settings = new ConnSettingsCan();

            // callbacks
            cbPort.SelectedIndexChanged += SelectedIndexChanged;
            cbSpeed.SelectedIndexChanged += SelectedIndexChanged;

            // chekbox
            cbPortFilter.Checked = m_ftdi_ports_only;
            cbSilentMode.Checked = m_is_can_silent_mode;

            // font
            //cbPortFilter.Font = new Font("Arial", 9.0f/*, FontStyle.Italic*/);
            //cbSilentMode.Font = new Font("Consolas", 9.0f/*, FontStyle.Italic*/);

            // combo box - speed list
            List<string> speedList = canSpeedList.getSpeedList();
            cbSpeed.Items.Add("Auto");    
            foreach (string s in speedList)
                cbSpeed.Items.Add(s);
            cbSpeed.SelectedIndex = 0;

            // combo box - ports
            scanPorts();
            if (cbPort.Items.Count > 0)
                cbPort.SelectedIndex = 0;

            // scan timer
            tmrPortScan = new System.Windows.Forms.Timer();
            tmrPortScan.Tick += new EventHandler(OnTimedEvent);
            tmrPortScan.Interval = 2000;
            tmrPortScan.Enabled = true;

            // ok button
            btnOk.DialogResult = DialogResult.OK;
        }

        // destructor
        ~FrmConnect()
        {
            // stop
            tmrPortScan.Enabled = false;
            tmrPortScan.Dispose();
        }
        #endregion

        #region Static Tools
        static public List<string> scanSilent(bool filter = true)
        {
            if (!filter)
                return comPortEnumerator.getAvailablePortNames();

            const string sFtdiVid = "0403"; // ftdi vid
            const string sFtdiPid = "6001"; // ftdi pid
            serialPortManufacturer info = new serialPortManufacturer(sFtdiVid, sFtdiPid);
            return comPortEnumerator.getPortNames(info);

            /*
            // stm
            const string sStVid = "0483";
            const string sStPid = "5740";
            info = new serialPortManufacturer(sStVid, sStPid);
            List<string> ls2 = comPortEnumerator.getPortNames(info);
            return ls2;
            */
        }

        static public string scanGetPortIfOnlyOneIsAwail()
        {
            List<string> ls = scanSilent();

            if (ls.Count == 1)
                return ls[0];
            return string.Empty;
        }

        #endregion

        #region Public default settings updaters

        // set a new default CAN speed value
        public void setDefaultSpeed(string speed)
        {
            for (int i = 0; i < cbSpeed.Items.Count; i++)
            {
                if (cbSpeed.Items[i].ToString() == speed)
                {
                    cbSpeed.SelectedIndex = i;
                    break;
                }
            }
        }

        // set a new default COM port value
        public void setDefaultPort(string port)
        {
            for (int i = 0; i < cbPort.Items.Count; i++)
            {
                if (cbPort.Items[i].ToString() == port)
                {
                    cbPort.SelectedIndex = i;
                    break;
                }
            }
        }

        // set a new default CAN silent mode value
        public void setDefaultSilentMode(bool enabled)
        {
            cbSilentMode.Checked = enabled;
        }

        // set a new default CAN trace autosave value
        public void setDefaultTraceAutosaveMode(bool enabled)
        {
            cbTraceSave.Checked = enabled;
        }

        // set new default settings
        public void setDefaultSettings (ConnSettingsCan settings)
        {
            setDefaultSpeed(settings.CanSpeed);
            setDefaultPort(settings.PortName);
            setDefaultSilentMode(settings.IsSilent);
            setDefaultTraceAutosaveMode(settings.IsTraceAutoSave);
        }
        #endregion

        #region Scanner
        // port scanner
        private void scanPorts()
        {
            if (!m_parent.isCommunication())
            {
                // scan for ftdi devices  
                bool ftdiOnly = m_ftdi_ports_only;
                List<string> portList = scanSilent(ftdiOnly);

                // are there any changes?
                List<string> prevPortList = new List<string>();
                foreach (var i in cbPort.Items)
                    prevPortList.Add(i.ToString());

                if (portList.SequenceEqual(prevPortList))
                    return; // no changes

                // get selected
                string selected = null != cbPort.SelectedItem ? 
                    cbPort.SelectedItem.ToString() : String.Empty;
                // update
                cbPort.SelectedIndex = -1;
                cbPort.Items.Clear();

                foreach (string item in portList)
                {
                    cbPort.Items.Add(item);
                    if (selected == item)
                        cbPort.SelectedIndex = cbPort.Items.Count - 1;
                }

                if (cbPort.SelectedIndex < 0 && cbPort.Items.Count > 0)
                    cbPort.SelectedIndex = 0;
            }
        }
        #endregion

        #region Callbacks

        // scan timer
        private void OnTimedEvent(object source, EventArgs e)
        {
            scanPorts();
        }

        // filter
        private void cbPortFilter_CheckedChanged(object sender, EventArgs e)
        {
            m_ftdi_ports_only = cbPortFilter.Checked;

            if (null != tmrPortScan)
            {
                // stop the timer
                tmrPortScan.Enabled = false;
                // scan
                scanPorts();
                // restart the timer
                tmrPortScan.Enabled = true;
            }
        }

        //  port & speed 
        private void SelectedIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            object item = cb.SelectedItem;

            // set
            if( item != null )
            {
                string sVal = item.ToString();

                if (cb == cbPort)
                    Settings.PortName = sVal;
                else if (cb == cbSpeed)
                    Settings.CanSpeed = sVal;
            }
            // reset
            else
            {
                if (cb == cbPort)
                    Settings.clearPort();
                else if (cb == cbSpeed)
                    Settings.clearSpeed();
            }
        }
        
        // silent mode
        private void cbSilentMode_CheckedChanged(object sender, EventArgs e)
        {
            m_is_can_silent_mode = cbSilentMode.Checked;
            Settings.IsSilent = m_is_can_silent_mode;
        }
        // trace autosave
        private void cbTraceSave_CheckedChanged(object sender, EventArgs e)
        {
            m_is_trace_autosave_en = cbTraceSave.Checked;
            Settings.IsTraceAutoSave = m_is_trace_autosave_en;
        }
        #endregion
    }
}

#region Connection Settings
namespace canAnalyzer
{
    public class ConnSettingsCan
    {
        public string PortName { set; get; }
        public string CanSpeed { set; get; }
        public bool   IsSilent { set; get; }
        public bool   IsTraceAutoSave { set; get; }

        public ConnSettingsCan()
        {
            clear();
        }

        public void clearPort()
        {
            PortName = string.Empty;
        }

        public void clearSpeed()
        {
            CanSpeed = string.Empty;
        }

        public void clear()
        {
            clearPort();
            clearSpeed();
            IsSilent = false;
            IsTraceAutoSave = false;
        }

        public bool isEmpty()
        {
            return  !string.IsNullOrEmpty(PortName) && 
                    !string.IsNullOrEmpty(CanSpeed);
        }
    }
}
#endregion
