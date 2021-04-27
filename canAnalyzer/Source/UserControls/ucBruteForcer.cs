using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;
using System.Xml;
using ScintillaNET;

namespace canAnalyzer
{
    public partial class ucBruteForcer : UserControl
    {
        // tool to send CAN messages
        private CanMessageSendTool m_can_tool;
        // woerker (thread)
        private Thread m_worker;
        // request to stop the worker
        private bool m_stop_worker_req;
        // CAN RX queue
        private List<canMessage2> m_can_list_rx;
        // CAN RX queue locker
        private Mutex m_rx_lock = new Mutex();
        // wait handle
        private EventWaitHandle m_worker_wait_handle =
            new EventWaitHandle(false, EventResetMode.AutoReset);

        // expected CAN message id
        private int m_can_response_id = 0;
        // request params
        private int m_req_timeout = 500;
        private int m_req_delay = 25;
        private int m_req_att = 1;
        private bool m_req_is_29_bit = false;

        // output dir
        private string m_out_path_dir = null;

        // 'header'
        private readonly string c_str_hdr = "Hdr";

        // loaded configs
        List<brute_config> m_stored_configs = new List<brute_config>();

        // region: public methods
        #region public_methods

        // constructor
        public ucBruteForcer(CanMessageSendTool canSendTool)
        {
            InitializeComponent();
            
            // create
            m_can_tool = canSendTool;
            m_stop_worker_req = true;
            m_can_list_rx = new List<canMessage2>();

            // ui
            uiInit();
            uiUpdate();

            // config
            m_stored_configs = configLoad(getConfigPath());
            uiAddStoredConfigs(m_stored_configs);
        }

        // get a path to the car action folder 
        static public string getConfigPath()
        {
            return System.IO.Path.GetDirectoryName(Application.ExecutablePath) +
                        "\\brute\\";
        }

        // external thread killer
        public void stop()
        {
            // stop
            workerStop();
            // wait
            for (int tmo = 1000; tmo > 0 && isRunning(); tmo -= 50)
                Thread.Sleep(50);
        }

        // is a test running?
        public bool isRunning()
        {
            return workerIsRunning(true);
        }

        // add new messages
        public void addMessage(List<canMessage2> ls)
        {
            // make sure the worker is running, otherwise do nothing
            if (!workerIsRunning() || m_stop_worker_req)
                return;

            m_rx_lock.WaitOne();

            // add new messages to the list
            foreach(var item in ls)
                if (item.Id.Id == m_can_response_id)
                    m_can_list_rx.AddRange(ls);
            // report that we've got something
            if (m_can_list_rx.Count > 0)
                m_worker_wait_handle.Set();

            m_rx_lock.ReleaseMutex();
        }

        // get a path to save data reports
        public void dataPathSet(string path)
        {
            m_out_path_dir = path;
            tbReportPath.Text = m_out_path_dir;
        }

        // set a path to save data reports
        public string dataPathGet()
        {
            return m_out_path_dir;
        }

        #endregion  // public_methods

        // region: worker stuff
        #region worker

        // stop the worker
        private void workerStop()
        {
            m_stop_worker_req = true;       // set the flag
            m_worker_wait_handle.Set();     // wake up
            // wait until the worker is stopped (tmo = 1 sec)
            for (int tmo = 1000; tmo > 0 && workerIsRunning(); tmo -= 50)
                Thread.Sleep(50);
        }

        // start the worker
        private void workerStart()
        {
            // make sure the worker is not running
            if (workerIsRunning())
                return;

            m_stop_worker_req = false;
            m_worker = new Thread(workerRoutine);
            m_worker.Name = "BruteForce Worker";
            m_worker.Start();
        }

        // is the worker running?
        private bool workerIsRunning(bool strict = false)
        {
            // empty -> doesn't exits
            if (null == m_worker)
                return false;

            // stopped or going to be stopped
            return m_worker.ThreadState == System.Threading.ThreadState.Stopped ||
                  (!strict && m_stop_worker_req) ?
                  false : true;
        }

        // worker routine
        private void workerRoutine()
        {
            // get required CAN data
            int can_id_req = uiCanIdGet_request();
            int can_id_rsp = uiCanIdGet_response();
            string can_data_req = uiCanMessageStringGet("Req");
            string can_data_flow = uiCanMessageStringGet("Flow");
            string can_data_resp = uiGetResponseTemplate();
            string report_dir = uiGetReportDirectory();
            string report_name = uiGetReportFileName();

            // data to be checked
            List<int> values_to_brute = uiGetRequiredValuesList();

            // req config
            int timeout_total_ms = m_req_timeout;
            int timeout_interframes_delay = m_req_delay;
            bool is_29bit = m_req_is_29_bit;
            int att_cnt_max = (int)numAttempts.Value;

            int max_can_id = canMessageId.GetMaxId(is_29bit);

            // validate
            if (can_id_req < 1)
            {
                trace("Failed to parse: Request ID", Color.Red);
                m_stop_worker_req = true;
            }
            if (can_id_req > max_can_id)
            {
                trace(
                    string.Format("Failed to parse: Request ID. Value = 0x{0}, Max = 0x{1}",
                    can_id_req.ToString("X3"), max_can_id.ToString("X3")), 
                    Color.Red);
                m_stop_worker_req = true;
            }
            if (can_id_rsp < 1)
            {
                trace("Failed to parse: Response ID", Color.Red);
                m_stop_worker_req = true;
            }
            if (can_id_rsp > max_can_id)
            {
                trace(
                    string.Format("Failed to parse: Response ID. Value = 0x{0}, Max = 0x{1}",
                    can_id_rsp.ToString("X3"), max_can_id.ToString("X3")),
                    Color.Red);
                m_stop_worker_req = true;
            }
            if (string.IsNullOrEmpty(can_data_req))
            {
                trace("Failed to parse: Request Bytes", Color.Red);
                m_stop_worker_req = true;
            }
            if (string.IsNullOrEmpty(can_data_resp))
            {
                trace("Failed to parse: Response Bytes", Color.Red);
                m_stop_worker_req = true;
            }
            if (string.IsNullOrEmpty(can_data_flow))
            {
                trace("Failed to parse: Flow Bytes", Color.Red);
                m_stop_worker_req = true;
            }
            if (values_to_brute == null || values_to_brute.Count == 0)
            {
                trace("Failed to parse: Values", Color.Red);
                m_stop_worker_req = true;
            }
            if (timeout_total_ms < 0)
            {
                trace("Failed to parse: Timeout", Color.Red);
                m_stop_worker_req = true;
            }
            if (timeout_interframes_delay < 0)
            {
                trace("Failed to parse: Delay", Color.Red);
                m_stop_worker_req = true;
            }
            if (!Directory.Exists(report_dir))
            {
                trace("Failed to parse: Report Folder", Color.Red);
                m_stop_worker_req = true;
            }
            if (string.IsNullOrEmpty(report_name))
            {
                trace("Failed to parse: Report Name", Color.Red);
                m_stop_worker_req = true;
            }            

            DateTime dtStarted = DateTime.Now;

            // data saver
            data_saver saver = new data_saver();
            saver.set_can(can_id_req, can_id_rsp, is_29bit);
            saver.set_data_format(';', can_data_resp, can_data_req, can_data_flow);
            if (!saver.start(report_dir, report_name, dtStarted))
            {
                trace("Failed to parse: Report Name", Color.Red);
                m_stop_worker_req = true;
            }


            // create the CAN handle
            can_handle handle = new can_handle(m_can_tool, c_str_hdr);

            // wait for hi-perf mode
            if (!m_stop_worker_req)
            {
                var tmo_until = DateTimeOffset.Now.ToUnixTimeMilliseconds() + 3000;
                while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < tmo_until &&
                       !m_can_tool.IsHighPerformanceModeEnabled())
                {
                    Thread.Sleep(100);
                }

                // sleep (hi-perf mode workaround)
                //Thread.Sleep(500);
            }

            int cnt_ok = 0, cnt_negative = 0, cnt_no_rsp = 0, cnt_not_finished = 0, cnt_tot = 0;

            // do
            if (!m_stop_worker_req)
            {

                // rx message lists
                List<canMessage2> tx_list = new List<canMessage2>();
                List<canMessage2> tmp_rx_list = new List<canMessage2>();

                // simplify the strings
                can_data_flow = MyCalc.simplify(can_data_flow);
                can_data_req = MyCalc.simplify(can_data_req);
                can_data_resp = MyCalc.simplify(can_data_resp);

                // create the CAN flow control request message
                bool is_flow_can_be_changed = can_data_flow.Contains("X");
                byte[] data_flow = null;
                if (is_flow_can_be_changed)
                {
                    data_flow = canConvDataStringToBytes(can_data_flow,
                                                         values_to_brute[0].ToString());  
                } else
                {
                    data_flow = canConvDataStringToBytes(can_data_flow, null);
                }

                canMessage2 msg_flow = new canMessage2(can_id_req, is_29bit, data_flow);

                // set vars
                m_can_response_id = can_id_rsp;

                // start
                trace(
                    string.Format("Started at {0}", Tools.dt_to_string(dtStarted)), 
                    Color.Blue);

                foreach (int value in values_to_brute)
                {
                    // get a new value to be checked
                    string sval = value.ToString();
                    // create the CAN request message
                    byte[] data_req = canConvDataStringToBytes(can_data_req, sval);
                    canMessage2 req = new canMessage2(can_id_req, is_29bit, data_req);
                    // get/update the CAN flow request message
                    if (is_flow_can_be_changed)
                    {
                        data_flow = canConvDataStringToBytes(can_data_flow, sval);
                        msg_flow = new canMessage2(can_id_req, is_29bit, data_flow);
                    }
                    // response
                    string tmp_resp = can_data_resp.Replace("X", sval);

                    bool val_finished = false;

                    // update the saver
                    saver.set_value(value);

                    // do
                    for (int att = 0; 
                        att < att_cnt_max && !m_stop_worker_req && !val_finished; 
                        att++)
                    {
                        // wait and then send the next requiest
                        worker_sleep(timeout_interframes_delay);

                        // clean the rx list
                        m_rx_lock.WaitOne();
                        m_can_list_rx.Clear();
                        m_rx_lock.ReleaseMutex();

                        val_finished = false;

                        // not the 1st attempts: info
                        //if (att != 0)
                        //    trace(value, string.Format("try {0}", att + 1));

                        // send the request
                        if (!handle.start(req, msg_flow, can_id_rsp, tmp_resp))
                        {
                            // something went wrong, finish
                            trace(value, "Failed to start", Color.Red);
                            m_stop_worker_req = true;
                            break;
                        } else
                        {
                            // do it after the message sent to reduce latency
                            uiSetCurrentValue(value);
                        }

                        // waiting for RX CAN messages
                        long wait_for = DateTimeOffset.Now.ToUnixTimeMilliseconds() + timeout_total_ms;


                        const long extra_timeout_ms = 100;
                        bool extra_timeout_added = false;

                        // get messages and handle them
                        while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < wait_for
                               && !m_stop_worker_req)
                        {
                            // make a copy of the rx list and clean it
                            m_rx_lock.WaitOne();
                            tmp_rx_list.AddRange(m_can_list_rx);
                            m_can_list_rx.Clear();
                            m_rx_lock.ReleaseMutex();

                            // handle the messages
                            handle.handle(tmp_rx_list);
                            tmp_rx_list.Clear();

                            // done?
                            val_finished = handle.isFinished();
                            negative_response neg = handle.getNegative();

                            // finished?
                            if (!m_stop_worker_req)
                            {
                                // not finished: check for the 'wait' negative response code 0x78
                                if (!val_finished)
                                {
                                    // update the timeout and wait for a while
                                    if (neg.IsNegativeResponse && neg.NeedToWait)
                                    {
                                        long ts_new = handle.getNegative().WaitUntil;
                                        if (ts_new != wait_for)
                                        {
                                            trace(value, 
                                                "neg response, code 0x" + neg.Code.ToString("X2"));
                                        }
                                        wait_for = ts_new;
                                    } else if (handle.got_response() && !extra_timeout_added)
                                    {
                                        // got at least one response message, icrease timeout (just in case)
                                        extra_timeout_added = true;
                                        wait_for += extra_timeout_ms;
                                    }
                                }

                                if (val_finished)
                                {
                                    if (!neg.IsNegativeResponse)
                                    {
                                        string data = "";// "done: ";
                                        var resp_data = handle.getResponse();
                                        if (resp_data != null)
                                            foreach (var b in resp_data)
                                                data += b.ToString("X2") + " ";
                                        else
                                            data += "data = null";
                                        trace(value, data);

                                        // todo
                                        // need to repeat the request?
                                        // if (neg.NeedToRepeatRequest)
                                        // {
                                        // }
                                    }
                                    else
                                    {
                                        trace(value, "neg response, code 0x" + neg.Code.ToString("X2"));
                                    }
                                }

                                // got a response, finish
                                if (val_finished)
                                    break;
                            }

                            // wait for new messages
                            if (!m_stop_worker_req)
                                m_worker_wait_handle.WaitOne(50);
                        }

                        // we've done with this value. what happened?
                        if (m_stop_worker_req)
                        {
                            // either a error occured or a user requested for stop
                            trace(value, "Aborted");
                        } else
                        {
                            if (!handle.isFinished() && !handle.got_response())
                            {
                                // no response
                                trace(value, string.Format("No response (att {0})", att + 1));
                            } else if (handle.got_response() && !handle.isFinished())
                            {
                                trace(value, 
                                    string.Format("Not finished, rxed = {0}, exp = {1}. (att {2})", 
                                    handle.getResponse().Count, handle.get_expected_response_len(), att + 1));
                            }
                        }

                        // save
                        if (!m_stop_worker_req)
                            saver.add(handle);
                    }

                    // stop
                    if (m_stop_worker_req)
                        break;

                    // update the counters
                    cnt_tot++;
                    if (val_finished)
                    {
                        if (handle.getNegative().IsNegativeResponse)
                            cnt_negative++;
                        else
                            cnt_ok++;
                    }
                    else
                    {
                        if (handle.got_response())
                            cnt_not_finished++;
                        else
                            cnt_no_rsp++;
                    }
                }

                // save
                saver.stop();

                // stop
                m_stop_worker_req = true;
            }

            // finish
            m_stop_worker_req = true;
            uiUpdate();

            TimeSpan tsStop = DateTime.Now - dtStarted;

            trace(
                string.Format("Stop at {0}", Tools.dt_to_string(DateTime.Now)),
                Color.Blue);

            if (cnt_tot > 0) {
                trace(
                    string.Format("Elapsed time: {0}:{1}:{2}",
                        tsStop.Hours.ToString().PadLeft(2, '0'),
                        tsStop.Minutes.ToString().PadLeft(2, '0'),
                        tsStop.Seconds.ToString().PadLeft(2, '0')));

                trace(
                    string.Format("Total: {0}, Responded: {1}, Negative Resp: {2}, Not Finished: {3}, No Resp: {4}",
                    cnt_tot, cnt_ok, cnt_negative, cnt_not_finished, cnt_no_rsp));
            }
            trace(" ");
        }

        private void worker_sleep(int sleep_ms)
        {
            while (!m_stop_worker_req && sleep_ms > 0)
            {
                if (sleep_ms > 100)
                {
                    Thread.Sleep(100);
                    sleep_ms -= 100;
                }
                else
                {
                    Thread.Sleep(sleep_ms);
                    sleep_ms = 0;
                }
            }
        }

        #endregion // worker

        // region: CAN tools
        #region can_tools

        private byte[] canConvDataStringToBytes(string data, string value)
        {
            var tmp = data.Split(';');

            byte[] res = new byte[tmp.Length];
            for (int i = 0; i < res.Length; i++)
            {
                string sb = tmp[i];
                byte b = 0;
                if (sb.Contains("X"))
                {
                    sb = sb.Replace("X", value);
                    object ob = MyCalc.evaluate(sb);
                    if (ob != null)
                        //b = (byte)(Convert.ToInt32(ob) & 0xFF);
                        b = MyCalc.objToByte(ob);
                    else
                        return null;
                }
                else
                {
                    b = (byte)(Convert.ToInt32(sb) & 0xFF);
                }

                res[i] = b;
            }

            return res;
        }

        #endregion

        // user interface
        #region user_interface

        // UI initialization, call it within the constructor
        private void uiInit()
        {
            // resize
            this.Dock = DockStyle.Fill;

            // request and response
            uiCreateComboBox_Dlc(cbReqDLC);
            uiCreateComboBox_Dlc(cbFlowDLC);
            uiCreateComboBox_ResponseHeaderPos(cbResponseHeaderPos);

            // buttons
            btnStartStop.Click += event_button_start_stop_click;
            btnLoadData.Click += event_button_load_values_click;
            btnReportPath.Click += event_button_report_path_click;

            tbReportName.TextChanged += event_path_changed;
            tbReportPath.TextChanged += event_path_changed;

            // config
            numTimeout.Value = m_req_timeout;
            numTimeout.Minimum = 50;
            numTimeout.Maximum = 9999;

            numDelay.Value = m_req_delay;
            numDelay.Minimum = 0;
            numDelay.Maximum = 9999;

            numAttempts.Value = m_req_att;
            numAttempts.Minimum = 1;
            numAttempts.Maximum = 9999;

            cbIs29bit.Checked = m_req_is_29_bit;

            // values
            txtValRange.ContextMenuStrip = new ContextMenuStrip();
            txtValRange.KeyPress += event_txt_values_on_key_press;
            txtValRange.Font = new Font("Consolas", 8.5f, FontStyle.Italic);

            // default data strings
            txtValRange.Text = "0x00-0x0A, 11, 12, 13-0x0F";
            tbReportName.Text = "vehicle";

            // menu
            menu.Items.Add("TODO: Save Template");

            // allign
            for (int i = 1; i < 8; i++)
            {
                const int offset_x = 50;
                // request
                var comp = uiControlGet("txtReq", i);
                if (comp != null)
                {
                    TextBox tb_ref = txtReq0;
                    ((TextBox)(comp)).Location = new Point(
                        tb_ref.Location.X + offset_x * i,
                        tb_ref.Location.Y);
                }

                comp = uiControlGet("txtRsp", i);
                if (comp != null)
                {
                    TextBox tb_ref = txtRsp0;
                    ((TextBox)(comp)).Location = new Point(
                        tb_ref.Location.X + offset_x * i,
                        tb_ref.Location.Y);
                }

                comp = uiControlGet("txtFlow", i);
                if (comp != null)
                {
                    TextBox tb_ref = txtFlow0;
                    ((TextBox)(comp)).Location = new Point(
                        tb_ref.Location.X + offset_x * i,
                        tb_ref.Location.Y);
                }
            }

            // menu
            gbSettings.ContextMenuStrip = menu;
            gbResponse.ContextMenuStrip = gbSettings.ContextMenuStrip;
            gbRequest.ContextMenuStrip = gbSettings.ContextMenuStrip;
            gbData.ContextMenuStrip = gbSettings.ContextMenuStrip;

            // templates
            cbTemplates.SelectedIndexChanged += event_template_changed;

            // current value
            uiSetCurrentValue(0);

            // trace
            traceCreate();
        }

        // UI update
        private void uiUpdate()
        {
            if (InvokeRequired)
            {
                // add the invoke to the queue
                BeginInvoke(new Action(uiUpdate));
                return;
            }

            bool isInProcess = workerIsRunning();
            bool start_allowed = true;
            //    !string.IsNullOrEmpty(tbReportName.Text) &&
            //    Directory.Exists(tbReportPath.Text);

            // button
            btnStartStop.Text = isInProcess ? "Stop" : "Start";
            btnStartStop.Enabled = start_allowed;
            // group boxes
            gbSettings.Enabled = !isInProcess;
            gbRequest.Enabled = !isInProcess;
            gbResponse.Enabled = !isInProcess;
            // separate components
            btnLoadData.Enabled = !isInProcess;
            txtValRange.ReadOnly = isInProcess;

            lblCurValue.Enabled = isInProcess;
        }

        // update current value
        private void uiSetCurrentValue(int value)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            // invoke if required
            if (InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() => uiSetCurrentValue(value)));
                return;
            }

            lblCurValue.Text = "0x" + value.ToString("X4");
        }

        // create a UI combo box for the DLC
        private void uiCreateComboBox_Dlc(ComboBox cb)
        {
            // clear the checkbox and init it
            cb.Items.Clear();
            for (int i = 1; i <= 8; i++)
                cb.Items.Add(i.ToString());

            // event handle
            cb.SelectedIndexChanged += event_comboBox_dlc_changed;
            // update
            cb.SelectedIndex = cb.Items.Count - 1;
        }

        // create a UI combo box for the header
        private void uiCreateComboBox_ResponseHeaderPos(ComboBox cb)
        {
            // clear the checkbox and init it
            cb.Items.Clear();
            for (int i = 0; i < 7; i++)
                cb.Items.Add(i.ToString());

            // event handle
            cb.SelectedIndexChanged += event_comboBox_resp_hdr_len_changed;
            // update
            cb.SelectedIndex = 0;
        }

        // convert a string to a CAN id value
        private int uiCanIdGet_template(string text)
        {
            // invoke if required
            if (InvokeRequired)
            {
                int res = 0;
                this.Invoke(new MethodInvoker(() => res = uiCanIdGet_template(text)));
                return res;
            }

            // try to parse the string as int
            int id = -1;

            object ob = MyCalc.evaluate(text);
            if (ob != null)
                id = MyCalc.objToI32(ob);

            return id;
        }

        // get the desired CAN id for request
        private int uiCanIdGet_request()
        {
            return uiCanIdGet_template(txtReqId.Text);
        }

        // get the desired CAN id for response
        private int uiCanIdGet_response()
        {
            string text = txtRespId.Text.ToLower();

            if (text.Contains("request"))
                text = text.Replace("request", "req");

            // put request data
            if (text.Contains("req"))
            {
                var m = Regex.Match(text, @"req\[(\d)\]");
                if (m != null && m.Groups.Count > 1)
                {
                    int ui_idx = Convert.ToInt32(m.Groups[1].ToString());
                    var box = uiControlGet("txtReq", ui_idx);
                    if (box != null)
                        text = text.Replace(m.Groups[0].ToString(), "(" + ((TextBox)(box)).Text + ")");
                }
            }

            // put request id
            if (text.Contains("reqid"))
                text = text.Replace("reqid", "req_id");
            if (text.Contains("req_id"))
                 text = text.Replace("req_id", "( " + txtReqId.Text + " )");

            return uiCanIdGet_template(text);
        }

        // get the CAN message string
        private string uiCanMessageStringGet(string name, string separator = ";")
        {
            if (InvokeRequired)
            {
                string res = null;
                this.Invoke(new MethodInvoker(() => res = uiCanMessageStringGet(name, separator)));
                return res;
            }

            // get the msg dlc
            // there are 2 types of messgages here: request / flow control
            int dlc = 0;
            if (name == "Req")
            {
                dlc = cbReqDLC.SelectedIndex + 1;
            }
            else // if (name == "Flow")
            {
                dlc = cbFlowDLC.SelectedIndex + 1;
            }

            // create the data list
            List<string> data = new List<string>();
            // and fill it
            for (int i = 0; i < dlc; i++)
            {
                // try to get the UI component with the required data byte
                var comp = uiControlGet("txt" + name, i);
                bool valid = false;
                // does it exits?
                if (comp != null)
                {
                    string txt_val = ((TextBox)comp).Text;

                    // make sure we can calculate it
                    // temporary replace variable 'X' and check
                    string tmp = txt_val.Replace("X", "1");

                    object ob = MyCalc.evaluate(tmp);
                    if (ob != null)
                    {
                        valid = true;
                        data.Add(txt_val);
                    }
                }
                // something went wrong, stop
                if (!valid)
                    break;
            }

            // something went wrong, incorrect message len
            if (data.Count != dlc)
                return null;

            // finish
            return string.Join(";", data);
        }

        // get user values string
        private string uiGetRequiredValuesString()
        {
            if (InvokeRequired)
            {
                string res = string.Empty;
                this.Invoke(new MethodInvoker(() => res = uiGetRequiredValuesString()));
                return res;
            }

            string sval = txtValRange.Text;

            // remove spaces
            sval = MyCalc.removeSpaces(sval);
            // update separators
            sval = sval.Replace(",", ";");
            sval = sval.Replace(".", ";");
            // check
            var reg = new Regex("^[0-9a-fA-F\\-;x]+$");
            if (string.IsNullOrEmpty(sval) || !reg.IsMatch(sval))
                return null;

            return sval;
        }

        // get list of values to be brute forced
        private List<int> uiGetRequiredValuesList()
        {
            string sval = uiGetRequiredValuesString();
            if (string.IsNullOrEmpty(sval))
                return null;

            List<int> ls = new List<int>();

            // parse
            var items = sval.Split(';');
            foreach (string item in items)
            {
                item.Trim();
                if (string.IsNullOrEmpty(item))
                    continue;

                string[] vals = item.Split('-');

                // is this range?
                if (vals.Length == 2)
                {
                    object ob_from = MyCalc.evaluate(vals[0]);
                    object ob_to = MyCalc.evaluate(vals[1]);
                    bool parsed = false;
                    if (ob_from != null && ob_to != null)
                    {
                        int from = MyCalc.objToI32(ob_from);
                        int to = MyCalc.objToI32(ob_to);
                        if (to > from && to > 0 && from >= 0)
                        {
                            parsed = true;
                            for (int i = from; i <= to; i++)
                                ls.Add(i);
                        }
                    }
                    if (!parsed)
                        return null;
                }
                else if (vals.Length == 1)
                {
                    // just try to parse
                    object ob = MyCalc.evaluate(item);
                    if (ob == null)
                        return null;
                    int val = Convert.ToInt32(ob);
                    ls.Add(val);
                }
                else
                {
                    return null;
                }
            }

            return ls;
        }

        // get a user control by its name
        private Control uiControlGet(string name)
        {
            Control item = null;
            if (!string.IsNullOrEmpty(name))
            {
                var ctrl = this.Controls.Find(name, true);
                if (ctrl != null && ctrl.Length == 1)
                    item = ctrl[0];
            }
            return item;
        }

        // get a user control by its name and numerical index
        private Control uiControlGet(string name, int index)
        {
            return uiControlGet(name + index.ToString());
        }

        // get a response template from the User Control
        private string uiGetResponseTemplate()
        {
            if (InvokeRequired)
            {
                string res = null;
                this.Invoke(new MethodInvoker(() => res = uiGetResponseTemplate()));
                return res;
            }

            const string txt = "txtRsp";
            string txt_hdr_val = c_str_hdr;
            const string txt_any_val = "*";
            const int item_cnt = 8;

            List<string> response = new List<string>();
            int hdr_cnt = 0;

            var reg = new Regex("^[0-9a-fA-FxX+\\-*\\/><\\(\\)|&^~]+$");

            // enabled
            for (int i = 0; i < item_cnt; i++)
            {
                string sval = string.Empty;
                var ctrl = uiControlGet(txt, i);
                if (ctrl != null)
                    sval = ((TextBox)(ctrl)).Text.Trim();
                sval = sval.Replace(" ", "");

                if (sval == txt_hdr_val)
                {
                    hdr_cnt++;
                    if (hdr_cnt > 2)
                        return null;

                    response.Add(sval);
                    continue;
                }
                if (sval == txt_any_val)
                {
                    response.Add(sval);
                    continue;
                }

                // check
                if (string.IsNullOrEmpty(sval) || !reg.IsMatch(sval))
                    return null;

                // try to evaluate the test value
                string tmp_sval = sval.Replace("X", "1");
                object ob = MyCalc.evaluate(tmp_sval);
                if (ob == null)
                    return null;

                // appned
                response.Add(sval);
            }

            return string.Join(";", response);
        }

        // get report path part: directory
        private string uiGetReportDirectory()
        {
            if (InvokeRequired)
            {
                string res = null;
                this.Invoke(new MethodInvoker(() => res = uiGetReportDirectory()));
                return res;
            }

            string name = tbReportPath.Text;
            return name;
        }

        // get report path part: file name
        private string uiGetReportFileName()
        {
            if (InvokeRequired)
            {
                string res = null;
                this.Invoke(new MethodInvoker(() => res = uiGetReportFileName()));
                return res;
            }

            string name = tbReportName.Text;
            return name;
        }

        // add configs
        private void uiAddStoredConfigs(List<brute_config> cfg)
        {
            if (InvokeRequired)
            {
                this.Invoke(new MethodInvoker(() => uiAddStoredConfigs(cfg)));
                return;
            }

            cbTemplates.Items.Clear();
            foreach(var item in cfg)
            {
                if (!string.IsNullOrEmpty(item.Name))
                {
                    cbTemplates.Items.Add(item.Name);
                }
            }

            if (cbTemplates.Items.Count > 0)
                cbTemplates.SelectedIndex = 0;
        }

        #endregion // user_interface

        // region: trace
        #region trace

        private advansedTextEdior2 m_txtTrace = null;

        // create trace
        private void traceCreate()
        {
            m_txtTrace = new advansedTextEdior2(pnlTrace);
        }

        // trace a value
        private void trace(int value, string text)
        {
            trace(value, text, Color.Black);
        }

        // trace a colored value
        private void trace(int value, string text, Color color)
        {
            string str = string.Format("0x{0}: {1}", value.ToString("X4"), text);
            trace(str, color);
        }

        // strace a string
        private void trace(string text)
        {
            trace(text, Color.Black);
        }

        // trace a colored string
        private void trace(string text, Color color)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<string, Color>(trace), new object[] { text, color });
                return;
            }

            const string emptyLine = "\"\"";

            // append
            var t = m_txtTrace.TextArea;
            if (text != emptyLine)
                t.AppendText(text + Environment.NewLine);
            else
                t.AppendText(Environment.NewLine);

            // scroll
            if (t.LinesOnScreen > 0)
            {
                var diff = t.Lines.Count - t.FirstVisibleLine;
                var diff2 = diff - t.LinesOnScreen;

                // diff < 4 and there are 10+ lines here
                if (diff2 < 4 && t.Lines.Count > 10)
                    t.LineScroll(t.Lines.Count, 0);
            }
        }

        #endregion // trace

        // region: utils
        #region utils
        private string get_values_string_from_file(string[] lines)
        {
            string res = string.Empty;

            // get a value format
            string format = data_saver.get_value_format();
            List<int> list = new List<int>();

            // get a list of values
            foreach (string s in lines)
            {
                var m = Regex.Match(s, format + @"\s*([x0-9A-F]+)");
                if (m.Success)
                {
                    string sval = m.Groups[1].ToString();
                    int val = -1;
                    if (Tools.tryParseInt(sval, out val))
                    {
                        list.Add(val);
                    }
                }
            }

            // sort
            list.Sort();
            // remove dublicates
            list = list.Distinct().ToList();

            // convert the list to string
            for (int i = 0; i < list.Count;)
            {
                int j = i + 1;

                int from = list[i];
                int to = from;

                // simplify to set a range
                for (; j < list.Count;)
                {
                    int tmp = list[j];
                    if ((tmp - from) > (j - i))
                        break;
                    j++;
                    to = tmp;
                }

                // separator
                if (!string.IsNullOrEmpty(res))
                    res += ", ";

                // add either a single value or a range
                if ((to - from) > 1)
                {
                    // add a range
                    res += string.Format("0x{0}-0x{1}",
                        from.ToString("X"), to.ToString("X"));

                    i += to - from + 1;
                }
                else
                {
                    // add a single value
                    res += string.Format("0x{0}", from.ToString("X"));
                    i++;
                }

            }

            return res;
        }

        #endregion // utils

        // region: events
        #region events

        // dlc changed
        private void event_comboBox_dlc_changed(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            int last_enabled_idx = cb.SelectedIndex;

            string name = string.Empty;

            if (cb == cbReqDLC)
            {
                name = "txtReq";
            }
            else if (cb == cbFlowDLC)
            {
                name = "txtFlow";
            }

            if (!string.IsNullOrEmpty(name))
            {
                for (int i = 0; i < 8; i++)
                {
                    Control ctrl = uiControlGet(name, i);
                    if (ctrl != null)
                        ctrl.Enabled = i <= last_enabled_idx;
                }
            }
        }

        // response header position changed
        private void event_comboBox_resp_hdr_len_changed(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            int cur_idx = cb.SelectedIndex;
            
            const string txt = "txtRsp";
            string txt_hdr_val = c_str_hdr;
            const int item_cnt = 8;

            int prev_idx = -1;

            // get all the cells
            List<string> prev = new List<string>();
            for (int i = 0; i < item_cnt; i++)
            {
                var ctrl = uiControlGet(txt, i);
                string sval = string.Empty;
                if (ctrl != null)
                {
                    TextBox tb = (TextBox)ctrl;
                    sval = tb.Text;
                    if (prev_idx < 0 && !tb.Enabled)
                        prev_idx = i;
                }
                prev.Add(sval);
            }

            if (cur_idx == prev_idx)
                return;

            // first start
            if (prev_idx < 0)
            {
                string[] dflt = new string[] { txt_hdr_val, txt_hdr_val, "0x62", "X>>8", "X", "*", "*", "*" };

                for (int i = 0; i < item_cnt; i++)
                {
                    var ctrl = uiControlGet(txt, i);
                    if (ctrl != null)
                        ((TextBox)(ctrl)).Text = dflt[i];
                }
            }
            else
            {
                // shift
                // H  H 10 20
                // 10 H H  20
                for (int i = 0; i < item_cnt; i++)
                {
                    int tmp_idx_prev = prev_idx + i;
                    int tmp_idx_cur = cur_idx + i;

                    if (tmp_idx_prev >= item_cnt)
                        tmp_idx_prev -= item_cnt;

                    if (tmp_idx_cur >= item_cnt)
                        tmp_idx_cur -= item_cnt;

                    // update
                    var ctrl_cur = uiControlGet(txt, tmp_idx_cur);
                    if (ctrl_cur != null)
                    {
                        string sval = prev[tmp_idx_prev];
                        ((TextBox)(ctrl_cur)).Text = prev[tmp_idx_prev];
                    }
                }
            }

            // enabled
            for (int i = 0; i < item_cnt; i++)
            {
                var ctrl = uiControlGet(txt, i);
                if (ctrl != null)
                    ctrl.Enabled = ((TextBox)(ctrl)).Text != txt_hdr_val;
            }

        }

        // button clicked
        private void event_on_start_stop_button_clicked()
        {
            if (workerIsRunning())
            {
                workerStop();
            }
            else
            {
                if (m_can_tool.IsSendingAllowed())
                {
                    // refresh the config
                    m_req_timeout = (int)numTimeout.Value;
                    m_req_delay = (int)numDelay.Value;
                    m_req_att = (int)numAttempts.Value;

                    m_req_is_29_bit = cbIs29bit.Checked;

                    workerStart();
                }
                else
                {
                    trace("Failed to start. Sending messages is prohibited.\n" +
                          "The CAN Analyzer is not connected, or Listen Only mode is switched on.\n",
                          Color.Red);
                }
            }

            uiUpdate();
        }

        // button clicked
        private void event_button_start_stop_click(object sender, EventArgs e)
        {
            if (workerIsRunning())
            {
                workerStop();
            }
            else
            {
                if (m_can_tool.IsSendingAllowed())
                {
                    // refresh the config
                    m_req_timeout = (int)numTimeout.Value;
                    m_req_delay = (int)numDelay.Value;
                    m_req_att = (int)numAttempts.Value;

                    m_req_is_29_bit = cbIs29bit.Checked;

                    workerStart();
                }
                else
                {
                    trace("Failed to start. Sending messages is prohibited.\n" +
                          "The CAN Analyzer is not connected, or Listen Only mode is switched on.\n",
                          Color.Red);
                }
            }

            uiUpdate();
        }

        // button clicked
        private void event_button_load_values_click(object sender, EventArgs e)
        {
            // open dialog
            OpenFileDialog dlg = new OpenFileDialog();
            dlg.CheckPathExists = true;

            if (dlg.ShowDialog() == DialogResult.OK && File.Exists(dlg.FileName))
            {
                string[] lines = File.ReadAllLines(dlg.FileName);
                string values = get_values_string_from_file(lines);

                // set
                if (!string.IsNullOrEmpty(values))
                {
                    txtValRange.Text = values;
                }
                else
                {
                    txtValRange.Text = "";
                }
            }
        }

        // button clicked
        private void event_button_report_path_click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = tbReportPath.Text;

            if (DialogResult.OK == dlg.ShowDialog())
            {
                string path = dlg.SelectedPath + "\\";
                tbReportPath.Text = path;
            }
        }

        // report path text changed
        private void event_path_changed(object sender, EventArgs e)
        {
            TextBox item = (TextBox)sender;

            if (item == tbReportPath)
                m_out_path_dir = item.Text;
            //uiUpdate();
        }

        // template changed
        private void event_template_changed(object sender, EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            object item = cb.SelectedItem;
            brute_config template = null;

            // should we sip it?
            foreach (var cfg in m_stored_configs)
            {
                if (cfg.Name == item.ToString())
                {
                    template = cfg;
                    break;
                }
            }

            if (template == null)
                return;

            // update the UC: common
            cbIs29bit.Checked = template.m_common.m_can_29_bit;
            numAttempts.Value = template.m_common.m_attempts;
            numDelay.Value = template.m_common.m_delay;
            numTimeout.Value = template.m_common.m_timeout;

            // request
            txtReqId.Text = template.m_request.m_id;
            foreach(var i in cbReqDLC.Items)
            {
                if (i.ToString() == template.m_request.m_dlc)
                    cbReqDLC.SelectedItem = i;
            }

            List<string> data_list = new List<string>(template.m_request.m_data.Split(','));
            while (data_list.Count < 8)
                data_list.Add("0");
            for (int i = 0; i < 8; i++)
                ((TextBox)(uiControlGet("txtReq", i))).Text = data_list[i].Trim();
            
            // flow
            foreach (var i in cbFlowDLC.Items)
            {
                if (i.ToString() == template.m_flow.m_dlc)
                    cbFlowDLC.SelectedItem = i;
            }
            data_list = new List<string>(template.m_flow.m_data.Split(','));
            while (data_list.Count < 8)
                data_list.Add("0");
            for (int i = 0; i < 8; i++)
                ((TextBox)(uiControlGet("txtFlow", i))).Text = data_list[i].Trim();

            // response
            foreach (var i in cbResponseHeaderPos.Items)
            {
                if (i.ToString() == template.m_response.m_header_pos.ToString())
                    cbResponseHeaderPos.SelectedItem = i;
            }

            txtRespId.Text = template.m_response.m_id;

            data_list = new List<string>(template.m_response.m_data.Split(','));
            while (data_list.Count < 8)
                data_list.Add("*");
            for (int i = 0; i < 8; i++)
                ((TextBox)(uiControlGet("txtRsp", i))).Text = data_list[i].Trim();

        }

        private void event_txt_values_on_key_press(object sender, KeyPressEventArgs e)
        {
            // allowed chars: 0-9, x, A-F, a-f,   ,
            var c = e.KeyChar;

            bool valid = char.IsControl(c) ||
                        char.IsDigit(c) ||
                        (c >= 'A' && c <= 'F') ||
                        (c >= 'a' && c <= 'f') ||
                        (c == 'x') ||
                        (c == '-') ||
                        (c == ' ') ||
                        (c == ',' || c == ';');

            e.Handled = !valid;
        }
        


        #endregion

        // region: CAN parser
        #region can_parser

        // negative response class
        private class negative_response
        {
            public bool IsNegativeResponse { set; get; }
            public bool NeedToRepeatRequest { set; get; }
            public bool NeedToWait { set; get; }
            public long WaitUntil { set; get; }
            public int Code { set; get; }
            public int Service { set; get; }

            public negative_response()
            {
                clean();
            }

            public void clean()
            {
                IsNegativeResponse = false;
                NeedToRepeatRequest = false;
                NeedToWait = false;
                WaitUntil = 0;
                Code = 0;
                Service = 0;
            }
        }

        // can handle
        private class can_handle
        {
            // can message sender 
            readonly CanMessageSendTool m_sender;
            // pre-configured params
            private int resp_hdr_pos = -1;
            private int exp_can_id = -1;
            private bool exp_is_29b = false;
            private byte [] resp_pattern = null;
            private bool [] resp_any_byte = null;
            private int m_first_frame_len_min = 0;
            private canMessage2 m_flow = null;

            private readonly string c_str_hdr = "Hdr";

            // handle data
            private readonly int FrameIdxDefault = -1;
            private int m_exp_data_len;
            private int next_frame_idx;
            
            // results
            private List<byte> m_response = null;
            private bool m_finished = false;
            private negative_response m_negative = new negative_response();

            private List<canMessage2> m_resp_msgs = null;

            // get a negative response
            public negative_response getNegative()
            {
                return m_negative;
            }

            // got at least one response message
            public bool got_response()
            {
                return next_frame_idx != FrameIdxDefault;
            }

            public int get_expected_response_len()
            {
                return m_exp_data_len;
            }

            // constructor
            public can_handle(CanMessageSendTool send_tool, string header_str)
            {
                m_sender = send_tool;
                c_str_hdr = header_str;
            }

            // start the handler
            public bool start(canMessage2 req, canMessage2 flow, int resp_id, string response)
            {
                bool sent = false;

                // reset
                m_finished = false;
                resp_any_byte = null;
                resp_pattern = null;
                next_frame_idx = FrameIdxDefault;
                resp_hdr_pos = -1;
                exp_can_id = -1;
                m_first_frame_len_min = 0;
                m_exp_data_len = 0;
                m_negative.clean();

                // check
                if (!canMessage2.IsNullOrEmpry(req) && resp_id > 0 &&
                    !canMessage2.IsNullOrEmpry(flow) && !string.IsNullOrEmpty(response))
                {
                    // fill
                    var tmp = response.Split(';');
                    resp_any_byte = new bool[tmp.Length];
                    resp_pattern = new byte[tmp.Length];
                    
                    for (int i = 0; i < tmp.Length; i++)
                    {
                        // any byte
                        resp_any_byte[i] = tmp[i] == "*";
                        // header pos
                        if (resp_hdr_pos < 0 && tmp[i] == c_str_hdr)
                            resp_hdr_pos = i;

                        if (tmp[i] == c_str_hdr || tmp[i] == "*")
                            continue;

                        // data byte
                        bool has_byte = false;
                        has_byte = byte.TryParse(tmp[i], out resp_pattern[i]);
                        if (!has_byte)
                        {
                            object ob = null;
                            ob = MyCalc.evaluate(tmp[i]);

                            if (ob != null)
                            {
                                resp_pattern[i] = MyCalc.objToByte(ob);
                                has_byte = true;
                            }
                        }
                        // update the min lenght
                        if (has_byte)
                            m_first_frame_len_min = i;
                    }

                    exp_can_id = resp_id;
                    exp_is_29b = req.Id.Is29bit;
                    m_flow = flow;

                    // send the request
                    sent = m_sender.SendCanMessage(req);
                }

                return sent;
            }

            // handle new CAN messages
            public bool handle(List<canMessage2> ls)
            {
                // check the internal conditions
                if (resp_hdr_pos < 0 || resp_pattern == null || resp_any_byte == null)
                    return false;

                if (m_finished)
                    return m_finished;

                if (ls == null || ls.Count == 0)
                    return m_finished;

                // check
                foreach (var msg in ls)
                {
                    if (msg.Id.Id != exp_can_id)
                        continue;
                    if (msg.Id.Is29bit != exp_is_29b)
                        continue;

                    var rx = msg.Data;
                    int dlc = rx.Length;

                    if (dlc <= resp_hdr_pos)
                        continue;

                    byte h1 = rx[resp_hdr_pos];
                    bool need_to_send_flow = false;

                    // handle, looking for the start message
                    if (next_frame_idx == -1)
                    {
                        // check its header
                        bool is_multi_frame = false;
                        int start_data_pos = -1;
                        bool failed = false;

                        // what's the frame type?
                        if (h1 <= (7 - resp_hdr_pos))
                        {
                            // signle frame
                            m_exp_data_len = h1;
                            start_data_pos = resp_hdr_pos + 1;
                        }
                        else if ((h1 & 0xF0) == 0x10 && (dlc > (resp_hdr_pos + 1)))
                        {
                            // multiframe
                            byte h2 = rx[resp_hdr_pos + 1];
                            m_exp_data_len = ((h1 & 0x0F) << 8) | h2;
                            start_data_pos = resp_hdr_pos + 2;
                            is_multi_frame = true;
                            need_to_send_flow = true;
                        }
                        else
                        {
                            // incorrect format
                            continue;
                        }

                        bool is_probably_negative = start_data_pos < dlc && rx[start_data_pos] == 0x7F;

                        // check the min allowed frame len
                        if (!is_probably_negative && m_first_frame_len_min > dlc)
                            continue;

                        // incorrect message format, too short (DLC is lower that expected)
                        if (((m_exp_data_len + 1) > dlc) && !is_multi_frame)
                            continue;

                        // before header
                        if (resp_hdr_pos > 0)
                        {
                            for (int i = 0; i < resp_hdr_pos; i++)
                            {
                                byte b = rx[i];
                                // matched?
                                if (b != resp_pattern[i] && !resp_any_byte[i])
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }

                        // is this negative response?
                        if (!is_multi_frame)
                        {
                            m_negative.clean();
                            // it is
                            if (rx[start_data_pos] == 0x7F)
                            {
                                m_negative.IsNegativeResponse = true;

                                if (dlc > (start_data_pos + 2))
                                {
                                    m_negative.Code = rx[start_data_pos + 2];
                                    if (m_negative.Code == 0x78)
                                    {
                                        // wait for 5000 ms
                                        m_negative.NeedToWait = true;
                                        m_negative.WaitUntil = DateTimeOffset.Now.ToUnixTimeMilliseconds() + 5000;
                                    }
                                    else if (m_negative.Code == 0x21)
                                    {
                                        m_negative.NeedToRepeatRequest = true;
                                    }
                                }
                            }
                        }

                        // after header
                        if (!m_negative.IsNegativeResponse)
                        {
                            int bytes_to_check = !is_multi_frame ? rx.Length - 1 : rx.Length;
                            for (int i = start_data_pos; i < bytes_to_check; i++)
                            {
                                byte b = rx[i];
                                int pattern_pos = !is_multi_frame ? i + 1 : i;

                                // matched?
                                if (b != resp_pattern[pattern_pos] && !resp_any_byte[pattern_pos])
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }

                        // finish
                        if (!failed)
                        {
                            // append
                            m_response = new List<byte>();

                            int add_from = start_data_pos;
                            int add_cnt = 0;

                            if (is_multi_frame)
                                add_cnt = dlc - add_from;
                            else
                                add_cnt = m_exp_data_len;

                            // append the CAN message
                            m_resp_msgs = new List<canMessage2>();
                            m_resp_msgs.Add(msg);

                            // append required bytes only
                            if (add_cnt > 0)
                                for (int i = add_from; i < add_cnt + add_from && i < dlc; i++)
                                    m_response.Add(rx[i]);

                            // negative
                            if (m_negative.IsNegativeResponse)
                            {
                                if (!m_negative.NeedToWait)
                                {
                                    // do not neew to wait, finish
                                    m_finished = true;
                                    break;
                                }        
                            }
                            else
                            {
                                m_negative.clean();
                                // increment the next expected message id
                                next_frame_idx = 1;
                                if (!is_multi_frame)
                                {
                                    // finish
                                    m_finished = true;
                                    break;
                                }
                            }
                        }
                    }
                    else
                    {
                        bool failed = false;

                        // multiframe, looking for next messages
                        // int nibble = h1 & 0xF0;
                        // int start_data_pos = resp_hdr_pos + 1;
                        if ((h1 & 0xF0) != 0x20)
                            continue;

                        int frame_idx = h1 & 0x0F;
                        int start_data_pos = resp_hdr_pos + 1;

                        // is this expected frame?
                        //int exp_frame_idx = (frame_idx + 1) > 15 ? 0 : frame_idx;
                        if (next_frame_idx != frame_idx)
                            continue;

                        // do we need to append extra data?
                        //if (m_response.Count >= m_exp_data_len)
                        //    continue;

                        // check
                        // before header
                        if (resp_hdr_pos > 0)
                        {
                            for (int i = 0; i < resp_hdr_pos; i++)
                            {
                                byte b = rx[i];
                                // matched?
                                if (b != resp_pattern[i] && !resp_any_byte[i])
                                {
                                    failed = true;
                                    break;
                                }
                            }
                        }

                        if (!failed)
                        {
                            // append the CAN message
                            m_resp_msgs.Add(msg);
                            // append the data bytes
                            for (int i = start_data_pos; i < dlc && m_response.Count < m_exp_data_len; i++)
                            {
                                m_response.Add(rx[i]);
                            }
                            next_frame_idx = next_frame_idx == 15 ? 0 : next_frame_idx + 1;
                        }

                        // finish
                        if (m_response.Count >= m_exp_data_len)
                        {
                            m_finished = true;
                            break;
                        }
                    }

                    // flow control
                    if (need_to_send_flow)
                    {
                        m_sender.SendCanMessage(m_flow);
                        need_to_send_flow = false;
                    }
                }

                return m_finished;
            }

            // is finished (got a correct/negative response)
            public bool isFinished()
            {
                return m_finished;
            }

            // get a response buffer
            public List<byte> getResponse()
            {
                List<byte> res = new List<byte>();

                if (/*isFinished() && */m_response != null)
                {
                    res.AddRange(m_response);
                }

                return res;
            }

            #region test
            private bool test_negative()
            {
                int request_id = 0x7E0;
                int response_id = request_id + 8;
                bool is_29_bit = false;

                // UDS, single
                string template_uds = MyCalc.simplify(c_str_hdr + ";" + c_str_hdr + "; 0x62;0x01; 0x10; *;*;*");

                // request, flow
                canMessage2 req_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x22, 0x01, 0x10, 0, 1, 2, 3 });
                canMessage2 flow_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x30, 0, 0, 0, 1, 2, 3 });

                // correct responses
                canMessage2 resp_uds_single_ok1 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x03, 0x7F, 0x22, 0x21 });
                canMessage2 resp_uds_single_ok2 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x02, 0x7F, 0x22});

                List<canMessage2> ls_uds = new List<canMessage2>();
                List<bool> res_ok = new List<bool>();
                List<bool> res_fail = new List<bool>();

                // successful test
                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok1);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok2);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));
            
                // finish
                bool rc_ok = true;
                bool rc_fail = true;
                foreach (var item in res_ok)
                    if (item != true)
                        rc_ok = false;
                foreach (var item in res_fail)
                    if (item != false)
                        rc_fail = false;

                bool rc = rc_ok && rc_fail;
                Debug.WriteLine("Brute: Test Negative Response: " + (rc ? "OK" : "Failed"));
                return rc;
            }

            private bool test_uds_single_offset()
            {
                int request_id = 0x7E0;
                int response_id = request_id + 8;
                bool is_29_bit = false;

                // UDS, single
                string template_uds = MyCalc.simplify("0x77;H;H;0x62;0x01; 0x10; *;*");

                // request, flow
                canMessage2 req_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x77, 0x22, 0x01, 0x10, 0, 1, 2, 3 });
                canMessage2 flow_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x77, 0x30, 0, 0, 0, 1, 2, 3 });

                // correct responses
                canMessage2 resp_uds_single_ok1 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x03, 0x62, 0x01, 0x10 });
                canMessage2 resp_uds_single_ok2 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x04, 0x62, 0x01, 0x10, 0x77 });
                canMessage2 resp_uds_single_ok3 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x05, 0x62, 0x01, 0x10, 0x00, 0x01 });
                canMessage2 resp_uds_single_ok4 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x06, 0x62, 0x01, 0x10, 0xFF, 0xFF, 0xFF });

                // incorrect responses
                // incorrect sid
                canMessage2 resp_uds_single_f1 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x06, 0x62, 0x01, 0x00, 0, 1, 2 });
                // too short
                canMessage2 resp_uds_single_f2 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x02, 0x62, 0x01 });
                // incorrect header len
                canMessage2 resp_uds_single_f3 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x06, 0x62, 0x01, 0x10 });
                // incorrect header len
                canMessage2 resp_uds_single_f4 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x77, 0x02, 0x62, 0x01, 0x00, 0x00, 0x00 });

                List<canMessage2> ls_uds = new List<canMessage2>();
                List<bool> res_ok = new List<bool>();
                List<bool> res_fail = new List<bool>();

                // failed tests
                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f1);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f2);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f3);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f4);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                // successful test
                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok1);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok2);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok3);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok4);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));


                // finish
                bool rc_ok = true;
                bool rc_fail = true;
                foreach (var item in res_ok)
                    if (item != true)
                        rc_ok = false;
                foreach (var item in res_fail)
                    if (item != false)
                        rc_fail = false;

                bool rc = rc_ok && rc_fail;
                Debug.WriteLine("Brute: Test Single UDS offseted: " + (rc ? "OK" : "Failed"));
                return rc;
            }

            private bool test_uds_single()
            {
                int request_id = 0x7E0;
                int response_id = request_id + 8;
                bool is_29_bit = false;
            
                // UDS, single
                string template_uds = MyCalc.simplify(c_str_hdr + ";" + c_str_hdr + "; 0x62;0x01; 0x10; *;*;*");

                // request, flow
                canMessage2 req_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x22, 0x01, 0x10, 0, 1, 2, 3 });
                canMessage2 flow_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x30, 0, 0, 0, 1, 2, 3 });
                
                // correct responses
                canMessage2 resp_uds_single_ok1 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x03, 0x62, 0x01, 0x10});
                canMessage2 resp_uds_single_ok2 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x04, 0x62, 0x01, 0x10, 0x77});
                canMessage2 resp_uds_single_ok3 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x05, 0x62, 0x01, 0x10, 0x00, 0x01});
                canMessage2 resp_uds_single_ok4 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x06, 0x62, 0x01, 0x10, 0xFF, 0xFF, 0xFF});
                canMessage2 resp_uds_single_ok5 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x07, 0x62, 0x01, 0x10, 0xFF, 0xFF, 0xFF, 0xFF });

                // incorrect responses
                // incorrect sid
                canMessage2 resp_uds_single_f1 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x06, 0x62, 0x01, 0x00, 0, 1, 2, 3 });
                // too short
                canMessage2 resp_uds_single_f2 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x02, 0x62, 0x01 });
                // incorrect header len
                canMessage2 resp_uds_single_f3 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x06, 0x62, 0x01, 0x10 });
                // incorrect header len
                canMessage2 resp_uds_single_f4 = new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x02, 0x62, 0x01, 0x00, 0x00, 0x00});

                List<canMessage2> ls_uds = new List<canMessage2>();
                List<bool> res_ok = new List<bool>();
                List<bool> res_fail = new List<bool>();

                // failed tests
                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f1);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f2);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f3);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_f4);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_fail.Add(handle(ls_uds));

                // successful test
                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok1);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok2);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok3);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok4);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));

                ls_uds.Clear();
                ls_uds.Add(resp_uds_single_ok5);
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(ls_uds));


                // finish
                bool rc_ok = true;
                bool rc_fail = true;
                foreach (var item in res_ok)
                    if (item != true)
                        rc_ok = false;
                foreach (var item in res_fail)
                    if (item != false)
                        rc_fail = false;

                bool rc = rc_ok && rc_fail;
                Debug.WriteLine("Brute: Test Single UDS: " + (rc ? "OK" : "Failed"));
                return rc;
            }

            private List<canMessage2> create_can_message(int resp, bool is29b, int len, byte svc, byte pid1, byte pid2)
            {
                List<canMessage2> ls = new List<canMessage2>();

                int len_b_1 = ((len >> 8) & 0xFF);
                int len_b2 = len & 0xFF;

                // initial
                ls.Add(new canMessage2(resp, is29b,
                    new byte[] {Convert.ToByte(0x10 + len_b_1), (byte)len_b2,
                        (byte)((int)svc + 0x40), pid1, pid2, 0, 1, 2 }));
                // other
                len -= 6;
                int msg_idx = 1;
                byte cnt = 0;
                while (len > 0)
                {   
                    byte hdr = (byte)(0x20 + msg_idx);
                    ls.Add(new canMessage2(resp, is29b,
                        new byte[] { hdr, cnt, 1, 2, 3, 4, 5, 6 }));

                    len -= 7;
                    msg_idx++;
                    if (msg_idx > 15)
                        msg_idx = 0;
                    cnt++;
                }

                return ls;
            }

            private List<canMessage2> create_can_message_offset(int resp, bool is29b, int len, byte svc, byte pid1, byte pid2, byte offset1)
            {
                List<canMessage2> ls = new List<canMessage2>();

                int len_b_1 = ((len >> 8) & 0xFF);
                int len_b2 = len & 0xFF;

                // initial
                ls.Add(new canMessage2(resp, is29b,
                    new byte[] {offset1, Convert.ToByte(0x10 + len_b_1), (byte)len_b2,
                        (byte)((int)svc + 0x40), pid1, pid2, 0, 1}));
                // other
                len -= 5;
                int msg_idx = 1;
                byte cnt = 0;
                while (len > 0)
                {
                    byte hdr = (byte)(0x20 + msg_idx);
                    ls.Add(new canMessage2(resp, is29b,
                        new byte[] { offset1, hdr, cnt, 1, 2, 3, 4, 5 }));

                    len -= 6;
                    msg_idx++;
                    if (msg_idx > 15)
                        msg_idx = 0;
                    cnt++;
                }

                return ls;
            }

            private bool test_uds_multi()
            {
                int request_id = 0x7E0;
                int response_id = request_id + 8;
                bool is_29_bit = false;

                // UDS, single
                string template_uds = MyCalc.simplify(c_str_hdr + ";" + c_str_hdr + "H; 0x62; 0x33; 0x77; *;*;*");

                // request, flow
                canMessage2 req_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x22, 0x33, 0x77, 0, 1, 2, 3 });
                canMessage2 flow_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x30, 0, 0, 0, 1, 2, 3 });

                // correct responses
                List<canMessage2> res_uds_ok_1 = new List<canMessage2>();
                res_uds_ok_1.Add(new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x10, 0x0F, 0x62, 0x33, 0x77, 0, 1, 2 }));
                res_uds_ok_1.Add(new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x21, 3, 4, 5, 6, 7, 8, 9 }));
                res_uds_ok_1.Add(new canMessage2(response_id, is_29_bit,
                    new byte[] { 0x22, 10, 11, 12, 13, 14, 15, 16 }));

                List<canMessage2> res_uds_ok_2 =
                    create_can_message(response_id, is_29_bit, 30, 0x22, 0x33, 0x77);
                List<canMessage2> res_uds_ok_3 =
                    create_can_message(response_id, is_29_bit, 100, 0x22, 0x33, 0x77);
                List<canMessage2> res_uds_ok_4 =
                    create_can_message(response_id, is_29_bit, 300, 0x22, 0x33, 0x77);

                List<bool> res_ok = new List<bool>();
                List<bool> res_fail = new List<bool>();

                // successful tests
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_1));
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_2));
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_3));
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_4));

                // finish
                bool rc_ok = true;
                bool rc_fail = true;
                foreach (var item in res_ok)
                    if (item != true)
                        rc_ok = false;
                foreach (var item in res_fail)
                    if (item != false)
                        rc_fail = false;

                bool rc = rc_ok && rc_fail;
                Debug.WriteLine("Brute: Test Multi UDS: " + (rc ? "OK" : "Failed"));
                return rc;
            }

            private bool test_uds_multi_ofset()
            {
                int request_id = 0x7E0;
                int response_id = request_id + 8;
                bool is_29_bit = false;

                byte ofset = 0x99;
                // UDS, single
                string template_uds = MyCalc.simplify("0x99;H;H;0x62; 0x33; 0x77; *;*");

                // request, flow
                canMessage2 req_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x33, 0x22, 0x33, 0x77, 0, 1, 2});
                canMessage2 flow_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x33, 0x30, 0, 0, 0, 1, 2});

                // correct responses
                List<canMessage2> res_uds_ok_1 = new List<canMessage2>();
                res_uds_ok_1.Add(new canMessage2(response_id, is_29_bit,
                    new byte[] { ofset, 0x10, 0x0F, 0x62, 0x33, 0x77, 0, 1}));
                res_uds_ok_1.Add(new canMessage2(response_id, is_29_bit,
                    new byte[] { ofset, 0x21, 3, 4, 5, 6, 7, 8 }));
                res_uds_ok_1.Add(new canMessage2(response_id, is_29_bit,
                    new byte[] { ofset, 0x22, 10, 11, 12, 13, 14, 15 }));

                List<canMessage2> res_uds_ok_2 =
                    create_can_message_offset(response_id, is_29_bit, 30, 0x22, 0x33, 0x77, ofset);
                List<canMessage2> res_uds_ok_3 =
                    create_can_message_offset(response_id, is_29_bit, 100, 0x22, 0x33, 0x77, ofset);
                List<canMessage2> res_uds_ok_4 =
                    create_can_message_offset(response_id, is_29_bit, 300, 0x22, 0x33, 0x77, ofset);

                List<bool> res_ok = new List<bool>();
                List<bool> res_fail = new List<bool>();

                // successful tests
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_1));
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_2));
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_3));
                start(req_uds, flow_uds, 0x7e8, template_uds);
                res_ok.Add(handle(res_uds_ok_4));

                // finish
                bool rc_ok = true;
                bool rc_fail = true;
                foreach (var item in res_ok)
                    if (item != true)
                        rc_ok = false;
                foreach (var item in res_fail)
                    if (item != false)
                        rc_fail = false;

                bool rc = rc_ok && rc_fail;
                Debug.WriteLine("Brute: Test Multi Offset UDS: " + (rc ? "OK" : "Failed"));
                return rc;
            }

            private void test_config_performance()
            {
                int request_id = 0x7E0;
                int response_id = request_id + 8;
                bool is_29_bit = false;

                // UDS, single
                string template_uds = MyCalc.simplify(c_str_hdr + ";" + c_str_hdr + "; 0x62;0x01;0x10;*;*;*");

                // request, flow
                canMessage2 req_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x22, 0x01, 0x10, 0, 1, 2, 3 });
                canMessage2 flow_uds = new canMessage2(request_id, is_29_bit,
                    new byte[] { 0x30, 0, 0, 0, 1, 2, 3 });


                Stopwatch sw = Stopwatch.StartNew();

                for (int i = 0; i < 100 * 1000; i++)
                {
                    start(req_uds, flow_uds, 0x7e8, template_uds);
                }
                long ts = sw.ElapsedMilliseconds;

                Debug.WriteLine("Brute: 100k tests took " + ts.ToString() + " ms.");

            }

            public void test()
            {

                test_negative();
                test_uds_multi_ofset();
                test_uds_multi();

                bool rc1 = test_uds_single();
                bool rc2 = test_uds_single_offset();


                test_config_performance();
            }
            #endregion
        }

        #endregion // can_parser


        // save 2 data files:
        // 1. with responded data only
        // 2. debug: full reponses, messages
        private class data_saver
        {
            // data path: directory and file name
            private string m_path_full = null;
            private string m_path_brief = null;

            // data strings
            private List<string> m_str_report_brief;
            private List<string> m_str_report_full;

            // write data conditions:
            // every 60 seconds
            // on close
            private long m_autosave_at = 0;
            private readonly long saveInterval_ms = 60000;

            // CAN data
            private bool m_is_29bit = false;
            private int m_req_id = 0;
            private int m_resp_id = 0;
            private int m_can_value = 0;

            private string m_format_tx = null;
            private string m_format_rx = null;
            private string m_format_flow = null;
            private char m_format_separator = ';';

            // constructor
            public data_saver()
            {
                m_str_report_brief = new List<string>();
                m_str_report_full = new List<string>();

                m_autosave_at = 0;
            }

            // data format
            public void set_data_format(char separator, string rx, string tx, string flow)
            {
                m_format_separator = separator;
                m_format_tx = tx;
                m_format_rx = rx;
                m_format_flow = flow;
            }

            // start
            public bool start(string directory, string name, DateTime dtCur)
            {
                bool res = Directory.Exists(directory);

                m_str_report_brief.Clear();
                m_str_report_full.Clear();

                if (res)
                {
                    const string fname1 = "_responded";
                    const string fname2 = "_failed";
                    const string ext = ".txt";

                    m_path_brief = Path.Combine(directory, name + fname1 + ext);
                    m_path_full = Path.Combine(directory,  name + fname2 + ext);

                     for (int i = 1; 
                         i < 1000 && (File.Exists(m_path_brief) || File.Exists(m_path_full)); 
                         i++)
                     {
                        m_path_brief = Path.Combine(directory,
                            string.Format("{0}{1}_{2}{3}", name, fname1, i, ext));
                        m_path_full = Path.Combine(directory,
                            string.Format("{0}{1}_{2}{3}", name, fname2, i, ext));
                    }

                     m_str_report_brief.Add(get_file_title_string(dtCur));
                     m_str_report_full.Add(get_file_title_string(dtCur));
                }
                else
                {
                    m_path_brief = null;
                    m_path_full = null;
                }

                return res;
            }

            // get a title string
            private string get_file_title_string(DateTime dtCur)
            {
                string fmt = string.Empty;

                if (!string.IsNullOrEmpty(m_format_tx))
                {
                    string s_id_req  = "0x" + canMessageId.GetIdAsString(m_req_id, m_is_29bit)  + "   ";
                    string s_id_resp = "0x" + canMessageId.GetIdAsString(m_resp_id, m_is_29bit) + "   ";

                    if (dtCur == null)
                        dtCur = DateTime.Now;

                    fmt = string.Format(
                            "Started at   {0}" + Environment.NewLine +
                            "TX format:   {1}" + Environment.NewLine +
                            "RX format:   {2}" + Environment.NewLine +
                            "Flow format: {3}" + Environment.NewLine,
                             Tools.dt_to_string(dtCur),
                             s_id_req  + dec_to_hex(m_format_tx, m_format_separator),
                             s_id_resp + dec_to_hex(m_format_rx, m_format_separator),
                             s_id_req  + dec_to_hex(m_format_flow, m_format_separator)
                         );
                }

                return fmt;
            }

            // dec to hex
            private string dec_to_hex(string fmt, char separator)
            {
                if (string.IsNullOrEmpty(fmt))
                    return "Null";

                var ls = fmt.Split(separator);
                string res = string.Empty;

                List<string> vals = new List<string>();

                foreach (var str in ls)
                {
                    int tmp = 0;
                    if (Tools.tryParseInt(str, out tmp))
                    {
                        vals.Add("0x" + tmp.ToString("X2"));
                    }
                    else
                    {
                        vals.Add(str);
                    }
                }

                return string.Join(", ", vals.ToArray());

            }

            // update the current value
            public void set_value(int value)
            {
                m_can_value = value;
            }

            // update the CAN config
            public void set_can(int req_id, int resp_id, bool is_29bit)
            {
                m_is_29bit = is_29bit;
                m_req_id = req_id;
                m_resp_id = resp_id;
            }

            // get string format to find a value
            static public string get_value_format()
            {
                return "Value: ";
            }

            // make a title string
            static private string make_title_brief(int value, int req = 0, int resp = 0, bool is29bit = false)
            {
                return string.Format(
                    @"{0} 0x{1}",
                    get_value_format(), value.ToString("X4"));
            }

            // add an entry (info log)
            private void add_info(can_handle handle)
            {
                string title = make_title_brief(m_can_value, m_req_id, m_resp_id, m_is_29bit);
                string str_brief = string.Empty;
                var payload = handle.getResponse();
                const int bytes_per_line = 8;

                if (handle.isFinished() && !handle.getNegative().IsNegativeResponse)
                {
                    // succesfully finished, no negative
                    str_brief = string.Format(
                                @" {0}   Len: {1}" + Environment.NewLine +
                                "{2}" + Environment.NewLine + Environment.NewLine,
                                title,
                                payload.Count,
                                payload_to_string(null, payload, bytes_per_line, true));
                }
                else if (!handle.isFinished() && handle.got_response())
                {
                    // started but not finished
                    str_brief = string.Format(
                                @"{0}    Not finished, Exp len = {1}" + Environment.NewLine +
                                "{2}" + Environment.NewLine + Environment.NewLine,
                                title,
                                handle.get_expected_response_len(),
                                payload_to_string(null, payload, bytes_per_line, true));
                }
                /*
                else if (handle.getNegative().IsNegativeResponse)
                {
                    // header   Negative response: 0x7F, 0x22, 0x31
                    str_brief = string.Format(
                                @"{0}    Negative: {1}" + Environment.NewLine,
                                title,
                                payload_to_string(null, payload, 0, false));
                }
                else if (!handle.got_response())
                {
                    // header    No response
                    str_brief = string.Format(
                                @"{0}    No response" + Environment.NewLine,
                                title);
                }
                else
                {
                    str_brief = string.Format(
                                @"{0}    Unknown Error" + Environment.NewLine +
                                "{1}" + Environment.NewLine + Environment.NewLine,
                                 title,
                                payload_to_string("", payload, 0, true));
                }
                */

                // append
                if (!string.IsNullOrEmpty(str_brief))
                    m_str_report_brief.Add(str_brief);
            }

            // add an entry (full log)
            private void add_err(can_handle handle)
            {
                string title = make_title_brief(m_can_value, m_req_id, m_resp_id, m_is_29bit);
                string data = string.Empty;
                var payload = handle.getResponse();

                if (handle.getNegative().IsNegativeResponse)
                {
                    // header   Negative response: payload
                    data = string.Format(
                           @"{0}    Negative: {1}",
                           title,
                           payload_to_string(null, payload, 0, false));
                }
                else if (!handle.got_response())
                {
                    // header    No response
                    data = string.Format(
                           @"{0}    No response",
                           title);
                }

                // append
                if (!string.IsNullOrEmpty(data))
                    m_str_report_full.Add(data);
            }

            // add an entry
            public bool add(can_handle handle)
            {
                bool res = true;

                // save at
                if (m_autosave_at == 0)
                    update_autosave_interval();

                // brief
                add_info(handle);
                // full
                add_err(handle);

                // save
                if (now() >= m_autosave_at)
                {
                    // update time interval
                    update_autosave_interval();

                    // save
                    save();
                }

                return res;
            }

            // finish
            public void stop()
            {
                save();
            }

            // save data and clean containers
            private void save()
            {
                save(m_path_brief, m_str_report_brief);
                save(m_path_full, m_str_report_full);
            }

            // save a data list
            private void save(string fname, List<string> data)
            {
                // create
                if (!File.Exists(fname))
                    File.CreateText(fname).Close();

                // save
                if (File.Exists(fname))
                {
                    
                    using (StreamWriter w = File.AppendText(fname))
                    {
                        foreach (var s in data)
                            w.WriteLine(s);
                    }
                }

                // clear
                data.Clear();
            }

            // get current time
            private long now()
            {
                return DateTimeOffset.Now.ToUnixTimeMilliseconds();
            }

            // update autosave interval
            private long update_autosave_interval()
            {
                m_autosave_at = now() + saveInterval_ms;
                return m_autosave_at;
            }

            // convert a byte list to string
            // format:
            // title:  0x54, 0x45, 0x53, 0x54, 0x20, 0x53, 0x54, 0x52       TEST STR
            //         0x49, 0x4e, 0x47, 0x20, 0x31, 0x32, 0x33             ING 123
            private string payload_to_string(string title, List<byte> data, int bytes_per_line = 0, bool add_ascii = false)
            {
                string res = string.IsNullOrEmpty(title) ? "" : title + ":   ";
                int offset = res.Length;

                if (bytes_per_line == 0)
                    bytes_per_line = data.Count;

                int pad_len = bytes_per_line * 6 /*0xAB, */ + 10;

                for (int i = 0; i < data.Count; )
                {
                    string vals = "", ascii = "";

                    // add 'N' byte per a single line
                    for (int j = 0; j < bytes_per_line && i < data.Count; i++, j++)
                    {
                        byte b = data[i];
                        bool sep = (j == (bytes_per_line-1)) || (i + 1) >= data.Count;

                        vals += "0x" + b.ToString("X2") + (sep ? "" : ", ");
                        ascii += (char)b >= ' ' && (char)b <= '~' ? (char)b : '.';
                    }

                    // append data bytes
                    res += vals.PadRight(pad_len);
                    // append ascii transcription
                    if (add_ascii)
                        res += ascii;

                    // new line
                    if ((i + 1) < data.Count)
                        res += Environment.NewLine + new string(' ', offset);
                }

                return res;
            }

        }

        #region config

        // load brute force template configs
        private List<brute_config> configLoad(string path)
        {
            List<brute_config> res = new List<brute_config>();

            if (!Directory.Exists(path))
                return res;


            const string file_extention = ".xml";

            // get file list
            string[] entries = Directory.GetFileSystemEntries(
                path, "*" + file_extention,
                SearchOption.AllDirectories);

            // parse it
            foreach (string fpath in entries)
            {
                string fname = Path.GetFileName(fpath);
                string error = "Unknown error";

                brute_config item = new brute_config(fpath);
                item.read();

                // report
                if (string.IsNullOrEmpty(item.Name))
                {
                    uiMessageTool.ShowMessage(
                        "File " + fname + " cannot be added" +
                        Environment.NewLine + Environment.NewLine + error,
                        "Brute Forcer");
                } else
                {
                    res.Add(item);
                }
            }

            return res;
        }

        // config tools
        private class config_tools
        {
            public static string replace_text_value(string sin)
            {
                string sout = sin;

                // and -> &
                sout = sin.Replace("and", "&");

                return sout;
            }
        }

        // a single config field
        private class config_field
        {
            public string name;
            public Dictionary<string, string> values;

            public config_field(string sname)
            {
                name = sname;
                values = new Dictionary<string, string>();
            }
        }

        // brute config, section = flow control
        private class config_data_flow_control
        {
            /*
	         *  <flow>
             *      <dlc>8</dlc>
             *      <data> 0x30, 0, 0, 0,  0, 0, 0, 0</data>
	         *  </flow>
             */
            public string m_dlc = string.Empty;
            public string m_data = string.Empty;

            static private readonly string name = "flow";
            private readonly string name_dlc = "dlc";
            private readonly string name_data = "data";

            public static bool is_field(config_field field)
            {
                return field != null && field.name == name;
            }

            // from
            public config_data_flow_control(config_field field)
            {
                if (!is_field(field))
                    return;

                m_dlc = string.Empty;
                m_data = string.Empty;

                foreach (var i in field.values)
                {
                    string sValue = config_tools.replace_text_value(i.Value);

                    // strings
                    if (i.Key == name_data)
                        m_data = sValue;
                    if (i.Key == name_dlc)
                        m_dlc = sValue;
                }


                validate();
            }

            private void validate()
            {
                if (m_dlc == null)
                    m_dlc = string.Empty;
                if (m_data == null)
                    m_data = string.Empty;

                var ob = MyCalc.evaluate(m_dlc);
                if (ob != null)
                {
                    int dlc = MyCalc.objToI32(ob);
                    if (dlc < 1 || dlc > 8)
                        m_dlc = string.Empty;
                }
            }

            // to
            public config_field convert()
            {
                validate();

                config_field field = new config_field(name);

                field.values.Add(name_dlc, m_dlc);
                field.values.Add(name_data, m_data);

                return field;
            }
        }

        // brute config, section = request
        private class config_data_request
        {
            /*
	         *  <request>
		     *      <id>0x7E0</id>
             *      <dlc>8</dlc>
             *      <data> 0x03, 0x22, X >> 8, X, 0, 0, 0, 0</data>
	         *  </request>
             */
            public string m_id = string.Empty, m_dlc = string.Empty;
            public string m_data = string.Empty;

            static private readonly string name = "request";
            private readonly string name_id = "id";
            private readonly string name_dlc = "dlc";
            private readonly string name_data = "data";

            public static bool is_field(config_field field)
            {
                return field != null && field.name == name;
            }

            // from
            public config_data_request(config_field field)
            {
                if (!is_field(field))
                    return;

                m_id = string.Empty;
                m_dlc = string.Empty;
                m_data = string.Empty;

                foreach (var i in field.values)
                {
                    string sValue = config_tools.replace_text_value(i.Value);

                    // strings
                    if (i.Key == name_data)
                        m_data = sValue;
                    if (i.Key == name_id)
                        m_id = sValue;
                    if (i.Key == name_dlc)
                        m_dlc = sValue;
                }

                validate();
            }

            private void validate()
            {
                if (m_id == null)
                    m_id = string.Empty;
                if (m_dlc == null)
                    m_dlc = string.Empty;
                if (m_data == null)
                    m_data = string.Empty;

                object ob = MyCalc.evaluate(m_id);
                if (ob != null)
                {
                    int id = MyCalc.objToI32(ob);
                    if (id < 1)
                        m_id = string.Empty;
                }

                ob = MyCalc.evaluate(m_dlc);
                if (ob != null)
                {
                    int dlc = MyCalc.objToI32(ob);
                    if (dlc < 1 || dlc > 8)
                        m_dlc = string.Empty;
                }
            }

            // to
            public config_field convert()
            {
                validate();

                config_field field = new config_field(name);

                field.values.Add(name_id, m_id);
                field.values.Add(name_dlc, m_dlc);
                field.values.Add(name_data, m_data);

                return field;
            }
        }

        // brute config, section = response
        private class config_data_response
        {
            /*
	         *  <response>
		     *      <id>Request + 8</id>
             *      <header_pos>0</header_pos>
             *      <data> Hdr, Hdr, 0x62, X >> 8,   X, *, *, *</data>
	         *  </response>
             */

            public string m_id = string.Empty;
            public string m_data = string.Empty;
            public int m_header_pos = 0;

            static private readonly string name = "response";
            private readonly string name_id = "id";
            private readonly string name_data = "data";
            private readonly string name_header_pos = "header_pos";

            public static bool is_field(config_field field)
            {
                return field != null && field.name == name;
            }

            // from
            public config_data_response(config_field field)
            {
                if (!is_field(field))
                    return;

                m_id = string.Empty;
                m_data = string.Empty;
                m_header_pos = 0;

                foreach (var i in field.values)
                {
                    string sValue = config_tools.replace_text_value(i.Value);

                    // strings
                    if (i.Key == name_data)
                        m_data = sValue;
                    if (i.Key == name_id)
                        m_id = sValue;

                    // data
                    if (i.Key == name_header_pos)
                    {
                        var ob = MyCalc.evaluate(sValue);
                        if (ob != null)
                        {
                            int val = MyCalc.objToI32(ob);
                            m_header_pos = val;
                        }
                    }
                }

                validate();
            }

            private void validate()
            {
                if (m_id == null)
                    m_id = string.Empty;
                if (m_data == null)
                    m_data = string.Empty;


                // comment, we can use 'Request + 8'
                /*
                 * object ob = null;
                ob = MyCalc.evaluate(m_id);
                if (ob != null)
                {
                    int id = MyCalc.objToI32(ob);
                    if (id < 1)
                        m_id = string.Empty;
                }
                */


                if (m_header_pos < 0 || m_header_pos > 6)
                    m_header_pos = 0;
            }

            // to
            public config_field convert()
            {
                validate();

                config_field field = new config_field(name);

                field.values.Add(name_id, m_id);
                field.values.Add(name_header_pos, m_header_pos.ToString());
                field.values.Add(name_data, m_data);

                return field;
            }
        }

        // brute config, section = common settings
        private class config_data_common
        {
            /*
             * 	<settings>
		     *      <timeout> 5000 </timeout>
             *      <delay> 100 </delay>
             *      <can_29> 0 </can_29>   
             *      <attempts> 1 </attempts> 
	         *  </settings>
            */

            public int m_timeout = 500, m_delay = 25, m_attempts = 1;
            public bool m_can_29_bit = false;

            static private readonly string name = "settings";
            private readonly string name_timeout = "timeout";
            private readonly string name_delay = "delay";
            private readonly string name_att = "attempts";
            private readonly string name_can_29 = "can_29";

            public static bool is_field(config_field field)
            {
                return field != null && field.name == name;
            }

            // from
            public config_data_common(config_field field)
            {
                if (!is_field(field))
                    return;

                m_timeout = 0;
                m_delay = 0;
                m_attempts = 0;
                m_can_29_bit = false;

                foreach (var i in field.values)
                {
                    string sValue = config_tools.replace_text_value(i.Value);

                    // evaluate
                    object ob = MyCalc.evaluate(sValue);

                    if (i.Key == name_timeout && ob != null)
                        m_timeout = MyCalc.objToI32(ob);
                    if (i.Key == name_delay && ob != null)
                        m_delay = MyCalc.objToI32(ob);
                    if (i.Key == name_att && ob != null)
                        m_attempts = MyCalc.objToI32(ob);
                    if (i.Key == name_can_29)
                        m_can_29_bit = MyCalc.objToI32(ob) > 0;

                }


                validate();
            }

            // to
            public config_field convert()
            {
                validate();

                config_field field = new config_field(name);

                field.values.Add(name_timeout, m_timeout.ToString());
                field.values.Add(name_delay, m_delay.ToString());
                field.values.Add(name_att, m_attempts.ToString());
                field.values.Add(name_can_29, Convert.ToInt32(m_can_29_bit).ToString());

                return field;
            }

            private void validate()
            {
                if (m_timeout < 1)
                    m_timeout = 1;
                if (m_delay < 0)
                    m_delay = 0;
                if (m_attempts < 1)
                    m_attempts = 1;
            }
        }

        // brute config, section = header
        private class config_data_header
        {
            /*  <header>
             *      <name>UDS</name>  
             *  </header>
             */

            public string m_config_name = string.Empty;

            static private readonly string name = "header";
            private readonly string name_name = "name";

            // is this field correct?
            public static bool is_field(config_field field)
            {
                return field != null && field.name == name;
            }

            // constructor
            public config_data_header(config_field field)
            {
                if (!is_field(field))
                    return;

                m_config_name = string.Empty;

                foreach (var i in field.values)
                {
                    if (i.Key == name_name)
                        m_config_name = i.Value;

                }

                validate();
            }

            // to
            public config_field convert()
            {
                validate();

                config_field field = new config_field(name);

                field.values.Add(name_name, m_config_name);

                return field;
            }

            // validate
            private void validate()
            {
                if (m_config_name == null)
                    m_config_name = string.Empty;
            }
        }
     
        // bruteforce configuration
        private class brute_config
        {
            public string Name { get { return m_name; } }
            public string FilePath { get { return m_path; } }

            private string m_name = string.Empty;
            private string m_path = string.Empty;

            public config_data_request m_request = null;
            public config_data_response m_response = null;
            public config_data_common m_common = null;
            public config_data_flow_control m_flow = null;
            public config_data_header m_header = null;

            // consturctor
            public brute_config(string path)
            {
                m_path = path;
            }

            // open the xls file by path
            private XmlElement open_xls(string fpath)
            {
                // is the file exists?
                if (!File.Exists(fpath))
                    return null;

                string fname = Path.GetFileName(fpath);

                // xml
                XmlDocument xDoc = new XmlDocument();
                try
                {
                    xDoc.Load(fpath);
                }
                catch (Exception ex)
                {
                    uiMessageTool.ShowMessage(
                        string.Format("Failed to parse {0}.\n" +
                        "Description: {1}", fname, ex.Message),
                        "CA Test");
                }

                XmlElement xRoot = xDoc.DocumentElement;
                if (xRoot == null)
                    return null;

                return xRoot;
            }

            // read the xls file by its path
            private List<config_field> read_xls(string fpath)
            {
                XmlElement xRoot = open_xls(fpath);
                if (xRoot == null || xRoot.Name != "brute_template")
                    return null;

                List<config_field> res = new List<config_field>();

                foreach (XmlNode node in xRoot)
                {
                    if (node.ChildNodes != null)
                    {
                        string node_name = node.Name;
                        config_field fields = new config_field(node_name);

                        // get nodes
                        foreach (XmlNode subNode in node.ChildNodes)
                        {
                            string item_name = subNode.Name;
                            string item_value = subNode.InnerText;
                            if (item_name == "#comment")
                                continue;
                            try
                            {
                                fields.values.Add(item_name, item_value);
                            }
                            catch (Exception ex)
                            {
                                uiMessageTool.ShowMessage(
                                    string.Format("Failed to parse {0}." + Environment.NewLine +
                                    "Node Name = {2}, Item name = {3}, Value = {4}" +
                                    Environment.NewLine + Environment.NewLine +
                                    "Description: {1}",
                                    Path.GetFileName(fpath), ex.Message, node_name, item_name, item_value),
                                    "Brute Forcer");

                                return null;
                            }
                        }

                        // add 
                        res.Add(fields);
                    }
                }

                return res;
            }

            // read file
            public void read()
            {
                List<config_field> fields = read_xls(m_path);

                if (fields == null)
                    return;

                foreach (config_field item in fields)
                {
                    if (config_data_request.is_field(item))
                        m_request = new config_data_request(item);

                    if (config_data_response.is_field(item))
                        m_response = new config_data_response(item);

                    if (config_data_common.is_field(item))
                        m_common = new config_data_common(item);

                    if (config_data_flow_control.is_field(item))
                        m_flow = new config_data_flow_control(item);

                    if (config_data_header.is_field(item))
                        m_header = new config_data_header(item);
                }

                if (m_header != null)
                    m_name = m_header.m_config_name;
            }

            // save file
            public string convert_to_string()
            {
                return null;
            }
        }

        #endregion




        private class advansedTextEdior2
        {
            public ScintillaNET.Scintilla TextArea;

            // test
            public string Text {
                get { return TextArea.Text; }
                set { TextArea.Text = value; }
            }

            // context
            public ContextMenuStrip contextMenu {
                get { return TextArea.ContextMenuStrip; }
                set { TextArea.ContextMenuStrip = value; }
            }

            // get text
            public string getText()
            {
                return TextArea.Text;
            }

            // constructor
            public advansedTextEdior2(Panel pnl)
            {
                TextArea = new Scintilla();
                pnl.Controls.Add(TextArea);
                TextArea.Dock = System.Windows.Forms.DockStyle.Fill;

                // INITIAL VIEW CONFIG
                TextArea.WrapMode = WrapMode.None;
                TextArea.IndentationGuides = IndentView.LookBoth;

                InitColors();
                InitSyntaxColoring();
                InitNumberMargin();
            }

            private void InitColors()
            {
                TextArea.SetSelectionBackColor(true, IntToColor(0x1663C9));
                TextArea.CaretForeColor = Color.LightGray;
            }

            /// <summary>
            /// the background color of the text area
            /// </summary>
            private const int BACK_COLOR = 0xFFFFFF;

            /// <summary>
            /// default text color of the text area
            /// </summary>
            private const int FORE_COLOR = 0x2A211C;// ;

            /// <summary>
            /// change this to whatever margin you want the line numbers to show in
            /// </summary>
            private const int NUMBER_MARGIN = 1;

            private void InitNumberMargin()
            {
                // disable
                var nums = TextArea.Margins[NUMBER_MARGIN];
                nums.Width = 0;
            }

            private void InitSyntaxColoring()
            {
                // Configure the default style
                TextArea.StyleResetDefault();
                TextArea.Styles[Style.Default].Font = "Consolas";
                TextArea.Styles[Style.Default].Size = 9;
                TextArea.Styles[Style.Default].BackColor = IntToColor(BACK_COLOR);
                TextArea.Styles[Style.Default].ForeColor = IntToColor(FORE_COLOR);
                TextArea.StyleClearAll();
                
                TextArea.Lexer = Lexer.Null;
            }

            private static Color IntToColor(int rgb)
            {
                return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
            }
        }
    }

}
