using System;
using System.Collections.Generic;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Windows.Forms;
using System.Xml;
using System.Threading;
using System.IO;
using System.Text.RegularExpressions;
using System.Diagnostics;


namespace canAnalyzer
{
    public partial class ucTestCA : UserControl
    {
        // CAN tool
        private CanMessageSendTool m_can_tool;
        // main worker
        private Thread m_worker;
        private bool m_stop_worker_req;
        private EventWaitHandle m_worker_wait_handle = 
            new EventWaitHandle(false, EventResetMode.AutoReset);
        // RX list
        private List<canMessage2> m_can_list_rx;
        private Mutex m_rx_lock = new Mutex();
        // data 
        private ca_item m_ca_item = null;
        private List<ca_item> car_action_list = new List<ca_item>();
        private Mutex m_data_lock = new Mutex();
        // ui
        private readonly int m_width_flow_initial = 0;


        // constructor
        public ucTestCA(CanMessageSendTool canSendTool)
        {
            InitializeComponent();
            // init
            m_can_tool = canSendTool;
            m_stop_worker_req = true;
            m_can_list_rx = new List<canMessage2>();
            m_width_flow_initial = panel2.ClientSize.Width;
            // car list
            carListInit();
            // ui
            uiInit();
            // stop
            processStartStop(false);
        }


        // public methods, such as: add messages, stop, etc.
        #region public_methods

        // external killer
        public void stop()
        {
            // stop
            workerStop();
            // wait
            for (int tmo = 1000; tmo > 0 && workerIsRunning(); tmo -= 50)
                Thread.Sleep(50);
        }

        // is a test running?
        public bool isRunning()
        {
            return workerIsRunning();
        }
        
        // add new messages
        public void addMessageList(List<canMessage2> ls)
        {
            // is the worker running?
            if (!workerIsRunning())
                return;

            m_rx_lock.WaitOne();

            // todo: check message IDs

            // add to the list
            m_can_list_rx.AddRange(ls);
            // indicate
            if (m_can_list_rx.Count > 0)
                m_worker_wait_handle.Set();

            m_rx_lock.ReleaseMutex();
        }

        // can the control be created?
        static public bool canBeCreated()
        {
            // check
            return Directory.Exists(carListPathGet());
        }
        #endregion

        // init methods
        #region initialization

        // init the CA list selector
        private void carListSelectorInit(string[] list)
        {
            cbCarActionList.SelectedIndex = -1;
            cbCarActionList.Items.Clear();
            if (list.Length > 0)
            {
                cbCarActionList.Items.AddRange(list);
                cbCarActionList.SelectedIndex = 0;
            }
        }

        // get a path to the car action folder 
        static private string carListPathGet()
        {
            return System.IO.Path.GetDirectoryName(Application.ExecutablePath) +
                        "\\can_ca\\";
        }

        // load car list
        private void carListInit()
        {
            cbCarActionList.SelectedIndexChanged += eventCarIndexChanged;

            const string file_extention = ".xml";
            string ca_dir = carListPathGet();

            // check
            if (!Directory.Exists(ca_dir))
                return;

            // get file list
            string[] entries = Directory.GetFileSystemEntries(
                ca_dir, "*" + file_extention, 
                SearchOption.AllDirectories);
       
            // parse it
            foreach (string fpath in entries)
            {
                string fname = Path.GetFileName(fpath);
                string error = "Unknown error";

                ca_item item = ca_convert.from_xml(fpath);

                // check
                if (item != null)
                {
                    error = item.get_error_string();
                    if (error != null && error != string.Empty)
                        item = null;
                    else
                        car_action_list.Add(item);
                }

                // report
                if (item == null)
                {
                    uiMessageTool.ShowMessage(
                        "File " + fname + " cannot be added" + 
                        Environment.NewLine + Environment.NewLine + error,
                        "CA Test");
                }
            }

            // sort the list
            car_action_list = car_action_list.OrderBy(o => o.get_name()).ToList();

            // fill selector in
            List<string> ca_names = new List<string>();
            foreach (var item in car_action_list)
            {
                ca_names.Add(item.get_name());
            }
            carListSelectorInit(ca_names.ToArray());
        }

        // UI initialization, call it within the constructor
        private void uiInit()
        {
            // resize
            this.Dock = DockStyle.Fill;
        }
        #endregion

        // workers
        #region workers
        // stop the worker
        private void workerStop()
        {
            m_stop_worker_req = true;
            m_worker_wait_handle.Set();
        }

        // start the worker
        private void workerStart()
        {
            if (workerIsRunning())
                return;
            if (m_ca_item == null)
                return;

            m_stop_worker_req = false;
            m_worker = new Thread(workerRoutine);
            m_worker.Name = "Test CA Worker";
            m_worker.Start();
        }

        // is the worker running?
        private bool workerIsRunning()
        {
            if (null == m_worker)
                return false;

            return m_worker.ThreadState ==
                System.Threading.ThreadState.Stopped/* || m_stop_worker_req */? false : true;
        }

        // worker routine
        private void workerRoutine()
        {
            // clean the rx list
            m_rx_lock.WaitOne();
            m_can_list_rx.Clear();
            m_rx_lock.ReleaseMutex();

            long last_sensors_upadte = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            const int update_ms = 500;

            List<canMessage2> tx_list = new List<canMessage2>();
            List<canMessage2> tmp_rx_list = new List<canMessage2>();

            Thread ui_updater = new Thread(uiWorkerRoutine);
            ui_updater.Name = "CA Worker UI";
            ui_updater.Start();

            //Stopwatch stopWatch, stopWatch_parse;
            //int test_cnt = 0;
            // do
            while (!m_stop_worker_req)
            {
                // debug
                //stopWatch = Stopwatch.StartNew();

                // make a copy of the rx list and clean it
                m_rx_lock.WaitOne();
                tmp_rx_list.AddRange(m_can_list_rx);
                m_can_list_rx.Clear();
                m_rx_lock.ReleaseMutex();

                //stopWatch_parse = Stopwatch.StartNew();

                // lock the data
                m_data_lock.WaitOne();

                //long ts_lock = stopWatch_parse.ElapsedMilliseconds;

                // handle requests
                tx_list.AddRange(m_ca_item.handle(tmp_rx_list));
                /*
                foreach (var msg in tmp_rx_list)
                {
                    List<canMessage2> tmp = m_ca_item.handle(msg);
                    if (tmp != null)
                        tx_list.AddRange(tmp);
                }*/
                //stopWatch_parse.Stop();

                // handle flow
                List <canMessage2> flow_msgs = m_ca_item.m_flow.handle(m_ca_item.m_sensors);
                if (flow_msgs != null)
                    tx_list.AddRange(flow_msgs);

                // release the data
                m_data_lock.ReleaseMutex();

                // send
                if (tx_list.Count > 0)
                {
                    m_can_tool.SendCanMessage(tx_list);
                    tx_list.Clear();
                }

                /*
                // debug
                stopWatch.Stop();
                long ts = stopWatch.ElapsedMilliseconds;
                if (ts >= 3)
                {
                    Debug.WriteLine("Test took {0},  cnt = {1}, no_err = {2}, parse_ms = {3}, lock_ms = {4}",
                        ts, tmp_rx_list.Count, test_cnt, stopWatch_parse.ElapsedMilliseconds, ts_lock);
                    test_cnt = 0;
                } else
                {
                    test_cnt++;
                }
                */

                // clear the internal rx list
                tmp_rx_list.Clear();

                // make sure we have no new messages
                if (m_can_list_rx.Count != 0)
                {
                    Debug.WriteLine("restart");
                    continue;
                }
                
                // sleep 
                int sleep_ms = m_ca_item.m_flow.next_update_in_ms();
                sleep_ms = sleep_ms > 0 && sleep_ms < update_ms ? sleep_ms : update_ms;

                if (!m_stop_worker_req)
                    m_worker_wait_handle.WaitOne(sleep_ms);
            }

            // finish
            m_stop_worker_req = true;

            // wait for UI
            while (ui_updater.ThreadState != System.Threading.ThreadState.Stopped)
            {
                Thread.Sleep(10);
            }
            ui_updater = null;
        }

        private sensor_updater updater = new sensor_updater();
        // ui worker routine
        private void uiWorkerRoutine()
        {
            updater = new sensor_updater();
            List<sensor_update_item> updt_items = getUiSensorInfoList(m_ca_item.m_sensors);

            // prepare
            foreach (var itm in updt_items)
                updater.add(itm);

            // routine
            while (!m_stop_worker_req)
            {
                int tmo = 500;
                int step = 50;
                // wait
                while (tmo > 0 && !m_stop_worker_req)
                {
                    Thread.Sleep(step);
                    tmo -= step;
                }

                // update
                if (!m_stop_worker_req)
                {
                    bool is_ui_update_required = false;

                    // update sensors
                    updt_items = getUiSensorInfoList(m_ca_item.m_sensors);

                    // lock 
                    m_data_lock.WaitOne();
                    // update sensor values
                    foreach (var updt_itm in updt_items)
                        updater.update_data(updt_itm);
                    is_ui_update_required = updater.refresh_sensors();
                    // release
                    m_data_lock.ReleaseMutex();

                    // update GUI sensor values
                    if (is_ui_update_required)
                        uiUpdateSensorValues(m_ca_item.m_sensors);
                }
            }
        }

        #endregion


        private void uiUpdateCaInfo(int sensor_num, int request_num, int flow_num)
        {
            lblCaInfo.Text = string.Format(
                "Sensors: {0}  Requests: {1}  Flow = {2}",
                sensor_num, request_num, flow_num);
        }

        // event - car index changed
        private void eventCarIndexChanged(object sender, System.EventArgs e)
        {
            ComboBox cb = (ComboBox)sender;
            object item = cb.SelectedItem;

            // should we sip it?
            if (item != null && m_ca_item != null)
            {
                if (m_ca_item.get_name() == item.ToString())
                    return;
            }

            // lock
            m_data_lock.WaitOne();

            // clear
            uiSenorsClear();

            // set
            if (item != null)
            {
                int num_sens = 0, num_req = 0, num_flow = 0;
                // find
                foreach (var ca in car_action_list)
                {
                    if (ca.get_name() == item.ToString())
                    {
                        // create
                        m_ca_item = ca;
                        // pause
                        panel2.SuspendLayout();
                        foreach (var sns in m_ca_item.m_sensors.m_list)
                            uiSensorAdd(sns);
                        // restore
                        panel2.ResumeLayout();

                        num_sens = ca.get_number_sensors();
                        num_req = ca.m_requests.Count;
                        num_flow = ca.m_flow.Count;

                        break;
                    }
                }
                uiUpdateCaInfo(num_sens, num_req, num_flow);

            }

            // release
            m_data_lock.ReleaseMutex();
        }

        // event - sensor's checkbox state changed
        private void eventSensorEnableStateChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb != null)
            {
                // lock
                m_data_lock.WaitOne();

                // get an index by its name
                int idx = m_ca_item.m_sensors.get_index_by_name(cb.Text);
                // update 'enabled' state
                if (idx >= 0)
                    m_ca_item.m_sensors.m_list[idx].Enabled = cb.Checked;

                // release
                m_data_lock.ReleaseMutex();
            }
        }

        // event - string sensor value changed
        private void eventSensorValueChanged(object sender, EventArgs e)
        {
            Type type = sender.GetType();
            string str_new_val = null;
            string str_name = null;
            int idx = -1;

            // lock
            m_data_lock.WaitOne();

            // is this a string?
            if (type.Name == "Button")
            {
                Button item = (Button)sender;
                str_name = item.Name.ToString();

                // try to get an index
                idx = getUiSensorIndex(str_name);

                TextBox textbox = (TextBox)getUiComponent(ui_value_text_prefix, idx);
                if (textbox != null)
                {
                    str_new_val = textbox.Text;
                }
            }
            // is this a number?
            else if (type.Name == "NumericUpDown")
            {
                NumericUpDown item = (NumericUpDown)sender;
                str_new_val = item.Value.ToString();
                str_name = item.Name.ToString();
            }

            // try to get an index
            if (idx < 0)
                idx = getUiSensorIndex(str_name);

            if (idx >= 0)
            {
                object item = getUiComponent(ui_sensor_name, idx);
                if (item != null)
                {
                    string sens_name = ((CheckBox)item).Text;
                    ca_sensor sensor = m_ca_item.m_sensors.get_by_name(sens_name);
                    if (sensor != null)
                        sensor.set_string(str_new_val);
                } 
                    //
                    //m_ca_item.m_sensors.m_list[idx].set_string(str_new_val);
            }

            // release
            m_data_lock.ReleaseMutex();
        }

        private void evtMouseWheelDummy(object sender, MouseEventArgs e)
        {
            // do nothing
            ((HandledMouseEventArgs)e).Handled = true;
        }


        private readonly string ui_value_text_prefix = "val_txt_";
        private readonly string ui_value_num_prefix = "val_num_";
        private readonly string ui_sensor_name = "name_";
        private readonly string ui_sensor_mode_name = "cb_mode_";
        private readonly string ui_sensor_num_from = "val_num_from_";
        private readonly string ui_sensor_num_to = "val_num_to_";
        private readonly string ui_sensor_num_step = "val_num_step_";
        private readonly string ui_sensor_num_interval = "num_interval_";


        private string getUiSensorPrefix(string item_name_full)
        {
            int start_pos_tmp = item_name_full.LastIndexOf('_');
            if (start_pos_tmp > 0)
            {
                return item_name_full.Substring(0, start_pos_tmp + 1);
            }
            return string.Empty;
        }

        private int getUiSensorIndex(string item_name_full)
        {
            int start_pos_tmp = item_name_full.LastIndexOf('_');
            if (start_pos_tmp > 0)
            {
                int idx = Convert.ToInt32(item_name_full.Substring(start_pos_tmp + 1));
                return idx;
            }
            return -1;
        }

        private object getUiComponent(string str_name_full)
        {
            int idx = getUiSensorIndex(str_name_full);
            string str_name_prefix = getUiSensorPrefix(str_name_full);

            return getUiComponent(str_name_prefix, idx);
        }

        private object getUiComponent(string str_name_prefix, int ui_index)
        {
            if (str_name_prefix == null || str_name_prefix.Length == 0 || ui_index < 0)
                return null;

            if (m_ui_panels.Count > ui_index)
            {
                Panel p = m_ui_panels[ui_index];
                var ctrls = p.Controls.Find(str_name_prefix + ui_index.ToString(), true);
                if (ctrls != null && ctrls.Length == 1)
                {
                    return ctrls[0];
                }
            }
            return null;
        }

        private void uiUpdateSensorValues(ca_sensor_list sns_list)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new MethodInvoker(() => uiUpdateSensorValues(sns_list)));
                return;
            }

            // test
            panel2.SuspendLayout();

            int ui_items_cnt = m_ui_panels.Count;

            for (int ui_idx = 0; ui_idx < ui_items_cnt; ui_idx++)
            {
                // get an UI item
                object item = getUiComponent(ui_sensor_name, ui_idx);
                if (item == null)
                    continue;

                // find its sensor
                string ui_name = ((CheckBox)item).Text;
                ca_sensor sensor = sns_list.get_by_name(ui_name);
                if (sensor == null)
                    continue;

                // is this text?
                if (sensor.is_string())
                    continue;

                // get an UI value
                item = getUiComponent(ui_value_num_prefix, ui_idx);
                if (item != null)
                {
                    int ui_value = (int)((NumericUpDown)item).Value;
                    int sens_value = sensor.get_int();
                    // check and update
                    if (sens_value != ui_value)
                    {
                        ((NumericUpDown)item).ValueChanged -= eventSensorValueChanged;
                        ((NumericUpDown)item).Value = (decimal)sens_value;
                        ((NumericUpDown)item).ValueChanged += eventSensorValueChanged;
                    }
                }
            }
            // test
            panel2.ResumeLayout();
        }

        private List<sensor_update_item> getUiSensorInfoList(ca_sensor_list sns_list, bool init = false)
        {
            // mode from to step interval
            List<sensor_update_item> res = new List<sensor_update_item>();

           // if (InvokeRequired)
          //  {
          //      this.Invoke(new MethodInvoker(() => res = getUiSensorInfoList(sns_list, init)));
         //       return res;
         //   }

            //Stopwatch sw = Stopwatch.StartNew();


            int ui_items_cnt = m_ui_panels.Count;

            for (int ui_idx = 0; ui_idx < ui_items_cnt; ui_idx++)
            {
                // get an UI item
                object item = getUiComponent(ui_sensor_name, ui_idx);
                if (item == null)
                    continue;

                // find its sensor
                string ui_name = ((CheckBox)item).Text;
                ca_sensor sensor = sns_list.get_by_name(ui_name);
                if (sensor == null)
                    continue;

                // is this text?
                if (sensor.is_string())
                {
                    res.Add(new sensor_update_item(sensor));
                    continue;
                }

                int mode = 0;
                int from = 0, to = 0, step = 0; // can be decimal
                int interval_ms = 0;

                // get a mode (init only)
                // item = getUiComponent(ui_sensor_mode_name, ui_idx);
                // if (item != null)
                //     mode = ((ComboBox)item).SelectedIndex;

                if (m_dict_ui_sensor_mode.ContainsKey(ui_idx))
                    mode = m_dict_ui_sensor_mode[ui_idx];

                // value
                item = getUiComponent(ui_value_num_prefix, ui_idx);
                if (item != null)
                {
                    int val = (int)((NumericUpDown)item).Value;
                    sensor.set_int(val);
                }
                
                // from
                item = getUiComponent(ui_sensor_num_from, ui_idx);
                if (item != null)
                    from = (int)((NumericUpDown)item).Value;
                // to
                item = getUiComponent(ui_sensor_num_to, ui_idx);
                if (item != null)
                    to = (int)((NumericUpDown)item).Value;
                // step
                item = getUiComponent(ui_sensor_num_step, ui_idx);
                if (item != null)
                    step = (int)((NumericUpDown)item).Value;
                // interval
                item = getUiComponent(ui_sensor_num_interval, ui_idx);
                if (item != null)
                    interval_ms = 1000 * (int)((NumericUpDown)item).Value;

                res.Add(new sensor_update_item(sensor, (sensor_update_mode)mode,
                    from, to, step, interval_ms));

            }

            //sw.Stop();
            //long ms = sw.ElapsedMilliseconds;

            //Debug.WriteLine("Update took {0}ms", ms);

            return res;
        }

        Dictionary<int, int> m_dict_ui_sensor_mode = new Dictionary<int, int>();

        private void eventSensorModeChanged(object sender, EventArgs e)
        {
            ComboBox box = (ComboBox)sender;
            int sensor_idx = getUiSensorIndex(box.Name);
            int mode = box.SelectedIndex;
            bool is_manual = false;

            if (mode == 0)
            {
                // manual
                is_manual = true;
            }

            // get a value item
            NumericUpDown num_item = (NumericUpDown)getUiComponent(ui_value_num_prefix, sensor_idx);
            if (num_item != null)
            {
                // enable/disable
                num_item.Enabled = is_manual;
            }

            // update the mode
            if (m_dict_ui_sensor_mode.ContainsKey(sensor_idx))
                m_dict_ui_sensor_mode[sensor_idx] = mode;
            else
                m_dict_ui_sensor_mode.Add(sensor_idx, mode);
        }


        private void uiSenorsClear()
        {
            panel2.Controls.Clear();
            m_ui_panels.Clear();
            m_ui_panel_pos_x = 0;
            m_dict_ui_sensor_mode.Clear();
        }

        private List<Panel> m_ui_panels = new List<Panel>();
        private int m_ui_panel_pos_x = 0;

        private bool uiSensorAdd(ca_sensor sensor)
        {
            int idx_num = panel2.Controls.Count + 0;
            int cur_pos = 0;
            int offset = 10;
            int pos_y_top = 3;
            int pos_y_bot = 30;

            int height_max = 60;
            int height_min = 32;

            // skip invisible sensors
            if (!sensor.Visible)
                return false;

            Font font_num = new Font("Consolas", 8.3f, FontStyle.Italic);
            Font font_name = new Font("Arial", 8.0f, FontStyle.Bold);
            
            panel2.AutoScroll = true;
            // create a panel
            Panel p = new Panel();
            p.Name = "panel" + idx_num;
            p.Size = new Size(m_width_flow_initial - 80, height_max); // todo review
            p.Location = new Point(5, m_ui_panel_pos_x);
            p.BorderStyle = BorderStyle.FixedSingle;
           // p.BackColor = Color.FromArgb(206, 206, 206);
            m_ui_panels.Add(p);

            // 1. checkbox
            CheckBox cb_name = new CheckBox();
            cb_name.Name = ui_sensor_name + idx_num;
            cb_name.Text = sensor.get_name();
            cb_name.Location = new Point(5, pos_y_top);
            cb_name.Width = 120;
            cb_name.Checked = sensor.Enabled;
            cb_name.CheckedChanged += eventSensorEnableStateChanged;
            cb_name.Font = font_name;
            p.Controls.Add(cb_name);
            cur_pos = cb_name.Location.X + cb_name.Width + offset;

            if (sensor.is_string())
            {
                p.Height = height_min;

                // string: just add a textbox field
                TextBox tb = new TextBox();
                tb.Name = ui_value_text_prefix + idx_num;
                tb.Text = sensor.get_string();
                tb.Location = new Point(cur_pos, pos_y_top);
                tb.Width = 200;
                tb.Font = font_num;
                p.Controls.Add(tb);
                cur_pos += tb.Width + offset;

                // add the 'set' button
                Button btn = new Button();
                btn.Name = "btn_set_" + idx_num;
                btn.Text = "Set";
                btn.Location = new Point(cur_pos, pos_y_top);
                btn.Width = 60;
                btn.Click += eventSensorValueChanged;
                p.Controls.Add(btn);
                cur_pos += btn.Width + offset;
            }
            if (sensor.is_int())
            {
                int max_char_num = 6;
                if (sensor.str_max != null)
                    max_char_num = sensor.str_max.Length;
                
                // numerical value
                NumericUpDown num = new NumericUpDown();
                num.Name = ui_value_num_prefix + idx_num;
                num.Minimum = Convert.ToDecimal(sensor.str_min);
                num.Maximum = Convert.ToDecimal(sensor.str_max);
                num.Value = Convert.ToDecimal(sensor.get_int());
                num.Location = new Point(cur_pos, pos_y_top);
                num.Width = 80;
                //num.ValueChanged += eventSensorValueChanged;
                num.Font = font_num;
                num.MouseWheel += evtMouseWheelDummy;
                num.DecimalPlaces = 0;
                p.Controls.Add(num);
                cur_pos += num.Width + 5;// + offset;// + 10;

                // mode label
                Label lbl_mode = new Label();
                lbl_mode.Name = "lbl_mode_" + idx_num;
                lbl_mode.AutoSize = true;
                lbl_mode.Width = 30;
                lbl_mode.Location = new Point(cur_pos + 10, 10);
                lbl_mode.Text = "mode";
                //lbl_mode.Font = font_num;
                p.Controls.Add(lbl_mode);
                //cur_pos += lbl_mode.Width + 2;

                // mode combobox
                ComboBox cb_mode = new ComboBox();
                cb_mode.Name = ui_sensor_mode_name + idx_num;
                cb_mode.Width = 110;
                cb_mode.Location = new Point(cur_pos, pos_y_bot);
                cb_mode.Font = font_num;
                cb_mode.Items.Add("Manual");
                cb_mode.Items.Add("Sine");
                cb_mode.Items.Add("Random");
                cb_mode.Items.Add("Up Restart");
                cb_mode.SelectedIndex = 0;
                cb_mode.SelectedIndexChanged += eventSensorModeChanged;
                cb_mode.MouseWheel += evtMouseWheelDummy;
                p.Controls.Add(cb_mode);
                cur_pos += cb_mode.Width + offset;

                // numericals from and to
                NumericUpDown num_from = new NumericUpDown();
                num_from.Name = ui_sensor_num_from + idx_num;
                num_from.Minimum = Convert.ToDecimal(sensor.str_min);
                num_from.Maximum = Convert.ToDecimal(sensor.str_max);
                num_from.Value = Convert.ToDecimal(sensor.str_min);
                num_from.Location = new Point(cur_pos, pos_y_top);
                num_from.Width = 80;
                num_from.Font = font_num;
                num_from.DecimalPlaces = 0;
                num_from.MouseWheel += evtMouseWheelDummy;
                p.Controls.Add(num_from);

                NumericUpDown num_to = new NumericUpDown();
                num_to.Name = ui_sensor_num_to + idx_num;
                num_to.Minimum = Convert.ToDecimal(sensor.str_min);
                num_to.Maximum = Convert.ToDecimal(sensor.str_max);
                num_to.Value = Convert.ToDecimal(sensor.str_max);
                num_to.Location = new Point(cur_pos, pos_y_bot);
                num_to.Width = 80;
                num_to.Font = font_num;
                num_to.DecimalPlaces = 0;
                num_to.MouseWheel += evtMouseWheelDummy;
                p.Controls.Add(num_to);
                cur_pos += num_to.Width + offset;

                // step (label)
                Label lbl_step = new Label();
                lbl_step.Name = "lbl_step_" + idx_num;
                lbl_step.AutoSize = true;
                lbl_step.Width = 30;
                lbl_step.Location = new Point(cur_pos+10, 10);
                lbl_step.Text = "step";
                p.Controls.Add(lbl_step);

                // step (num)
                NumericUpDown num_step = new NumericUpDown();
                num_step.Name = ui_sensor_num_step + idx_num;
                num_step.Minimum = 0;// Convert.ToDecimal(sensor.str_min);
                num_step.Maximum = Convert.ToDecimal(sensor.str_max);
                num_step.Value = 1;
                num_step.Location = new Point(cur_pos, pos_y_bot);
                num_step.Width = 60;
                num_step.Font = font_num;
                num_step.DecimalPlaces = 0;
                num_step.MouseWheel += evtMouseWheelDummy;
                p.Controls.Add(num_step);
                cur_pos += num_step.Width + offset;

                // interval (label)
                Label lbl_interval = new Label();
                lbl_interval.Name = "lbl_interval_" + idx_num;
                lbl_interval.AutoSize = true;
                lbl_interval.Width = 30;
                lbl_interval.Location = new Point(cur_pos, 10);
                lbl_interval.Text = "sec";
                p.Controls.Add(lbl_interval);

             
                NumericUpDown num_interval = new NumericUpDown();
                num_interval.Name = ui_sensor_num_interval + idx_num;
                num_interval.Minimum = 1;
                num_interval.Maximum = 999;
                num_interval.Value = 1;
                num_interval.Location = new Point(cur_pos, pos_y_bot);
                num_interval.Width = 50;
                num_interval.Font = font_num;
                num_interval.DecimalPlaces = 0;
                num_interval.MouseWheel += evtMouseWheelDummy;
                p.Controls.Add(num_interval);
                cur_pos += num_interval.Width + offset;
                
            }

            panel2.Controls.Add(p);
            panel2.Invalidate();


            m_ui_panel_pos_x += p.Height + 5;

            return false;
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            Button item = (Button)sender;

            bool start = item.Text == "Start";
            processStartStop(start);
        }

        private void processStartStop(bool start)
        {
            while (workerIsRunning())
            {
                workerStop();
                Thread.Sleep(10);
            }

            if (start && m_can_tool.IsSendingAllowed())
            {
                workerStart();
                btnStartStop.Text = "Stop";
            } else
            {
                btnStartStop.Text = "Start";
            }

            cbCarActionList.Enabled = btnStartStop.Text == "Start";
        }
    }
}

// tools
namespace canAnalyzer
{
    public class ca_xml_item
    {
        public Dictionary<string, string> attributes = null;
        public Dictionary<string, string> children = null;
        public string name = string.Empty;

        public ca_xml_item()
        {
            name = string.Empty;
            attributes = null;
            children = null;
        }

        public void parse(XmlNode node)
        {
            name = node.Name;
            attributes = new Dictionary<string, string>();
            children = new Dictionary<string, string>();
            if (node.Attributes != null)
            {
                // get attributes
                foreach (XmlNode attr in node.Attributes)
                {
                    attributes.Add(attr.Name, attr.Value);
                }
            }
            if (node.ChildNodes != null)
            {
                // get nodes
                foreach (XmlNode subNode in node.ChildNodes)
                {
                    children.Add(subNode.Name, subNode.InnerText);
                }
            }
        }
    }

    public class ca_convert
    {

        public static string simplify_byte_expression(string str_expression)
        {
            if (str_expression == null)
                return null;

            // make a copy
            string expression = string.Copy(str_expression);
            // prepare
            expression = MyCalc.simplify(expression);

            // split
            string[] list = expression.Split(',');
            string res = string.Empty;

            for (int i = 0; i < list.Length; i++)
            {
                string s = list[i];
                object obj = MyCalc.evaluate(s);
                if (obj != null)
                {
                    // convert
                    //UInt32 u32 = Convert.ToUInt32(obj);
                    // to byte
                    //byte b = (byte)(u32);
                    byte b = MyCalc.objToByte(obj);
                    // update
                    s = b.ToString();
                }
                else
                {
                    // update sensors (for better simplification)
                    var re = Regex.Matches(s, "(\"\\S+\")");
                    foreach (Match m in re)
                    {
                        if (s != m.Groups[0].ToString())
                        {
                            string pattern = m.Groups[1].ToString();
                            s = s.Replace(pattern, "(" + pattern + ")");
                        }
                    }
                }
                list[i] = s;
            }
            // restore
            res = string.Join(",", list);
            // just in case
            res = MyCalc.simplify(res);

            return res;
        }

        public static ca_item from_xml(string fpath)
        {
            // is the file exists?
            if (!File.Exists(fpath))
                return null;

            string str_ca_name = string.Empty;
            ca_sensor_list sens_list = new ca_sensor_list();
            List<ca_request> req_list = new List<ca_request>();
            ca_flow_list flow_list = new ca_flow_list();
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

            foreach (XmlNode xnode in xRoot)
            {
                string name = string.Empty;
                string str_value = string.Empty;
                string templateDelay = string.Empty;

                // get vehicle name
                if (xnode.Name == "title")
                {
                    str_ca_name = xnode.InnerText;
                }
                // parse sensors
                else if (xnode.Name == "sensorList")
                {
                    foreach (XmlNode sensorNode in xnode.ChildNodes)
                    {
                        ca_xml_item item = new ca_xml_item();
                        item.parse(sensorNode);
                        if (item.name == "sensor")
                        {
                            string sns_name;
                            string sns_type;
                            string sns_val;
                            string sns_min, sns_max;
                            string sns_step;
                            string sns_visible;
                            item.attributes.TryGetValue("name", out sns_name);
                            item.children.TryGetValue("type", out sns_type);
                            item.children.TryGetValue("min", out sns_min);
                            item.children.TryGetValue("max", out sns_max);
                            item.children.TryGetValue("value", out sns_val);
                            item.children.TryGetValue("step", out sns_step);
                            item.children.TryGetValue("visible", out sns_visible);

                            // create
                            ca_sensor sns = new ca_sensor(sns_name, sns_type, sns_val, sns_min, sns_max);
                            // check and append
                            if (sns.is_valid())
                            {
                                // change visible
                                if (sns_visible != null)
                                {
                                    object ob = MyCalc.evaluate(sns_visible);
                                    if (ob != null)
                                        sns.Visible = Convert.ToBoolean(ob);
                                }
                                // add
                                sens_list.add(sns);
                            }
                            else
                            {
                                uiMessageTool.ShowMessage(
                                    string.Format("Failed to parse sensor '{0}' in {1}.\n",
                                    sns.str_name, fname),
                                    "CA Test");
                            }
                        }
                    }
                }
                // parse requests 
                else if (xnode.Name == "requestList")
                {
                    foreach (XmlNode reqNode in xnode.ChildNodes)
                    {
                        ca_xml_item item = new ca_xml_item();
                        item.parse(reqNode);
                        if (item.name == "request")
                        {
                            string req_name;
                            string req_protocol, req_id, req_data, resp_id, resp_data;
                            string resp_len, resp_empty_byte;

                            item.attributes.TryGetValue("name", out req_name);
                            item.children.TryGetValue("protocol", out req_protocol);
                            item.children.TryGetValue("request_id", out req_id);
                            item.children.TryGetValue("request_data", out req_data);
                            item.children.TryGetValue("response_id", out resp_id);
                            item.children.TryGetValue("response_data", out resp_data);
                            item.children.TryGetValue("response_len_exact", out resp_len);
                            item.children.TryGetValue("response_empty_byte", out resp_empty_byte);
                            
                            // simplify strings for better calculation performance
                            if (req_data != null)
                                req_data = simplify_byte_expression(req_data);
                            if (resp_data != null)
                                resp_data = simplify_byte_expression(resp_data);

                            if (req_id != null)
                            {
                                // request list
                                List<string> req_ls = new List<string>(req_id.Split(','));
                                ca_request req = new ca_request(req_protocol,
                                    req_ls, req_data, resp_id, resp_data);
                                // limitations
                                req.set_response_limitations(resp_len, resp_empty_byte);
                                // push
                                req_list.Add(req);
                            }
                        }
                    }
                }
                // parse flow
                else if (xnode.Name == "flowList")
                {
                    foreach (XmlNode sensorNode in xnode.ChildNodes)
                    {
                        ca_xml_item item = new ca_xml_item();
                        item.parse(sensorNode);
                        if (item.name == "flowMessage")
                        {
                            string flow_name;
                            string flow_data;
                            string flow_id;
                            string flow_is29bit;
                            string flow_interval;
                            item.attributes.TryGetValue("name", out flow_name);
                            item.children.TryGetValue("id", out flow_id);
                            item.children.TryGetValue("data", out flow_data);
                            item.children.TryGetValue("interval", out flow_interval);
                            item.children.TryGetValue("is_29_bit", out flow_is29bit);

                            // prepare
                            if (flow_data != null)
                                flow_data = simplify_byte_expression(flow_data);

                            object obj;
                            int can_id = 0;
                            int interval = 0;
                            bool is_29bit = false;

                            // get can id
                            obj = MyCalc.evaluate(flow_id);
                            if (obj != null)
                                can_id = Convert.ToInt32(obj);
                            // get interval
                            obj = MyCalc.evaluate(flow_interval);
                            if (obj != null)
                                interval = Convert.ToInt32(obj);
                            // get can format
                            obj = MyCalc.evaluate(flow_is29bit);
                            if (obj != null)
                                is_29bit = Convert.ToBoolean(obj);
                            // create
                            ca_flow flow = new ca_flow(flow_name, can_id, is_29bit, flow_data, interval);
                            // is valid?
                            if (flow != null && flow.IsValid)
                                flow_list.add(flow);
                        }
                    }
                }
            }

            // create the item
            if (str_ca_name != string.Empty)
            {    
                ca_item res = new ca_item(str_ca_name, sens_list, req_list, flow_list);
                return res;
            }

            return null;
        }
    }

}

namespace canAnalyzer
{
    // Car Action item class
    public class ca_item
    {
        // name
        private readonly string str_name;
        // sensors
        public ca_sensor_list m_sensors;
        // requests
        public List<ca_request> m_requests;
        // flow
        public ca_flow_list m_flow;

        // constructor
        public ca_item(string name, ca_sensor_list sensors, List<ca_request> requests, ca_flow_list flow)
        {
            str_name = string.Copy(name);
            m_sensors = sensors;
            m_requests = requests;
            m_flow = flow;
        }

        // handle a message
        public List<canMessage2> handle(canMessage2 rx)
        {
            List<canMessage2> res = new List<canMessage2>();
            
            foreach (var req in m_requests)
            {
                List<canMessage2> tmp = req.handle(rx, m_sensors);
                if (tmp != null)
                    res.AddRange(tmp);
            }

            return res;
        }

        // handle a message list
        public List<canMessage2> handle(List<canMessage2> list)
        {
            List<canMessage2> res = new List<canMessage2>();

            foreach (var req in m_requests)
            {
                List<canMessage2> tmp = req.handle(list, m_sensors);
                if (tmp != null)
                    res.AddRange(tmp);
            }

            return res;
        }

        // get a CA name
        public string get_name()
        {
            return str_name;
        }

        // get a number of sensors
        public int get_number_sensors()
        {
            return m_sensors.count();
        }

        // check request and flow sensors
        public string get_error_string()
        {
            string error = string.Empty;
            string str_replace = "0";
            string summary = string.Empty;

            // unite
            foreach (var item in m_requests)
                summary += item.Response() + ",";
            foreach (var item in m_flow.get_reponses())
                summary += item + ",";
            // remove the last comma
            if (summary.LastIndexOf(',') == (summary.Length - 1))
                summary = summary.Remove(summary.Length - 1, 1);

            // make a copy
            string summary_std = string.Copy(summary);

            // remove hex
            summary = MyCalc.simplify(summary);

            // make sure there is no unknown sensors
            // remove ints
            foreach (var item in m_sensors.m_list)
            {
                if (item.is_int())
                    summary = summary.Replace("\"" + item.get_name() + "\"", str_replace);
                if (item.is_string())
                    summary = summary.Replace("\"" + item.get_name() + "[...]\"", str_replace);        
            }
            // remove strings (range)
            var reg_m = Regex.Matches(summary, "\"([0-9a-zA-Z_]+)\\[(\\d+\\-\\d+)\\]\"");
            foreach (Match v in reg_m)
            {
                string sens_name = v.Groups[1].ToString();
                string data = v.Groups[2].ToString();
                // exists?
                if (null != m_sensors.get_by_name(sens_name))
                        summary = summary.Replace(v.Groups[0].ToString(), str_replace);
            }
            // remove strings (full and char)
            reg_m = Regex.Matches(summary, "\"([0-9a-zA-Z_]+)\\[(\\d+)\\]\"");
            foreach (Match v in reg_m)
            {
                string sens_name = v.Groups[1].ToString();
                string data = v.Groups[2].ToString();
                // exists?
                if (null != m_sensors.get_by_name(sens_name))
                {
                    bool remove = true;
                    int tmp = 0;
                    remove = Tools.tryParseInt(data, out tmp);

                    if (remove)
                        summary = summary.Replace(v.Groups[0].ToString(), str_replace);
                }        
            }

            // add them
            List<string> sum_updated = new List<string>(summary.Split(',') );
            List<string> sum_origin = new List<string>(summary_std.Split(','));

            for (int i = 0; i < sum_updated.Count; i++)
            {
                string s = sum_updated[i];
                object ob = MyCalc.evaluate(s);
                if (ob == null)
                    error += "Failed to parse: " + sum_origin[i] + Environment.NewLine;
                else if (ca_sensor_list.has_unsupported_chars(s))
                    error += "Failed to parse: " + sum_origin[i] + Environment.NewLine;
            }


            // bracket
            /*
            int opening_bracket_cnt = 0, closing_bracket_cnt = 0;
            foreach (var ch in summary_std)
            {
                if (ch == '(')
                    opening_bracket_cnt++;
                if (ch == ')')
                    closing_bracket_cnt++;
            }
            */

            if (!string.IsNullOrEmpty(error))
            {
                /* debug */
                error = null;
            }

            return error;
        }
    }

    // sensor class
    public class ca_sensor
    {
        readonly public string str_name;
        readonly public string str_type;
        readonly public string str_min;
        readonly public string str_max;

        private string str_value;
        private string str_value_prev;

        readonly private string str_type_int = "int";
        readonly private string str_type_string = "string";

        public bool Enabled { set; get; }
        public bool Visible { set; get; }

        // constructor
        public ca_sensor(string name, string type, string value, string min = null, string max = null)
        {
            str_name = name;
            str_type = type;

            Enabled = true;
            str_value_prev = string.Empty;
            Visible = true;

            // reset
            str_value = value;
            str_min = null;
            str_max = null;

            // check
            if (str_value == null || str_value == string.Empty)
                return;

            // try to evaluate
            if (str_type_int == type)
            {
                int int_val = 0, int_min = 0, int_max = 0;
                // value
                object ob = MyCalc.evaluate(value);
                if (ob != null)
                {
                    str_value = ob.ToString();
                    int_val = Convert.ToInt32(ob);
                }
                // min
                ob = MyCalc.evaluate(min);
                if (ob != null)
                {
                    str_min = ob.ToString();
                    int_min = Convert.ToInt32(ob);
                }
                // max
                ob = MyCalc.evaluate(max);
                if (ob != null)
                { 
                    str_max = ob.ToString();
                    int_max = Convert.ToInt32(ob);
                }

                // check min and max
                if (str_max != null && str_min != null)
                {
                    if (int_max < int_min)
                    {
                        // swap
                        Tools.swap<string>(ref str_min, ref str_max);
                        Tools.swap<int>(ref int_min, ref int_max);
                    }
                }

                // check the borders
                if (int_val > int_max && !string.IsNullOrEmpty(str_max))
                {
                    int_val = int_max;
                    str_value = string.Copy(str_max);
                }
                if (int_val < int_min && !string.IsNullOrEmpty(str_min))
                {
                    int_val = int_min;
                    str_value = string.Copy(str_min);
                }
            }
        }

        // check is the sensor valid?
        public bool is_valid()
        {
            bool is_valid = true;
            is_valid = str_name != string.Empty && str_name != null &&
                    str_type != string.Empty && str_type != null &&
                    str_value != string.Empty && str_value != null;
            
            if (is_int())
            {
                // min and max
                if (str_min == null || str_max == null)
                    is_valid = false;
            }


            return is_valid;
        }

        // get sensor name
        public string get_name()
        {
            return str_name;
        }

        // is integer?
        public bool is_int()
        {
            return str_type_int == str_type;
        }

        // is string?
        public bool is_string()
        {
            return str_type_string == str_type;
        }

        // get its value as string (for all types)
        public string get_string()
        {
            return str_value;
        }

        // set a new value (for all types)
        public void set_string(string new_val)
        {
            str_value_prev = string.Copy(str_value);
            str_value = string.Copy(new_val);
        }

        // get a value as integer
        public int get_int()
        {
            if (str_type == str_type_int)
            {
                int res;
                if (string_to_int(str_value, out res))
                    return res;
            }
            return 12345;
        }

        // set a value as interger
        public bool set_int(string str_new_val)
        {
            int new_val = 0;
            if (string_to_int(str_new_val, out new_val))
                return set_int(new_val);

            return false;
        }

        public bool set_int(int new_val)
        {
            // check its type
            if (str_type == str_type_int)
            {
                int min = 0, max = 0;
                bool has_min = false, has_max = false, is_ok = true;

                // check the borders
                has_min = str_min != null && string_to_int(str_min, out min);
                has_max = str_max != null && string_to_int(str_max, out max);

                if (has_min && new_val < min)
                    is_ok = false;
                if (has_max && new_val > max)
                    is_ok = false;

                // set
                if (is_ok)
                {
                    set_string(new_val.ToString());
                }

                return is_ok;

            }
            return false;
        }

        private bool string_to_int(string str_in, out int int_out)
        {
            int res = Convert.ToInt32(str_in);
            int_out = res;

            return true;
        }

    };

    // sensor list class
    public class ca_sensor_list
    {
        public List<ca_sensor> m_list = new List<ca_sensor>();

        // clear the list
        public void clear()
        {
            m_list.Clear();
        }

        // add an item to the list
        public bool add(ca_sensor sensor)
        {
            // check
            if (!sensor.is_valid())
                return false;
            // is exist?
            if (get_by_name(sensor.get_name()) != null)
                return false;
            // add
            m_list.Add(sensor);
            return true;
        }

        // get count of items
        public int count()
        {
            return m_list.Count;
        }

        public ca_sensor get_by_index(int index)
        {
            if (index >= 0 && index < count())
                return m_list[index];
            return null;
        }

        // get a sensors using its name, null othwerwise
        public ca_sensor get_by_name(string name)
        {
            foreach (var sns in m_list)
            {
                if (sns.get_name() == name)
                    return sns;
            }
            return null;
        }

        // get a sensor index using its name, -1 otherwise
        public int get_index_by_name(string name)
        {
            int i = 0;
            foreach (var sns in m_list)
            {
                if (sns.get_name() == name)
                    return i;
                i++;
            }
            return -1;
        }

        // convert an ascii string to a byte array string
        private string ascii_to_int(string str, int from, int len)
        {
            string res = null;
            if (from >= 0 && len >= 1 && (from + len) < str.Length)
            {
                char[] charValues = str.ToCharArray();
                res = " " + ((int)charValues[from]).ToString();

                for (int i = from + 1; i <= len; i++)
                {
                    res += "," + ((int)charValues[i]).ToString();
                }
            }
            return res;
        }


        Dictionary<string, byte> cache = new Dictionary<string, byte>();

        static public bool has_unsupported_chars(string expression)
        {
            // check for unsupported characters
            var reg = new Regex("^[a-fA-F0-9, x+\\-*\\/&^|><\\(\\)]+$");
            if (!reg.IsMatch(expression))
                return true;

            return false;
        }

        // covert payload string to a byte list, add all the sensor values. null if error
        public List<byte> convert_payload_string(string str_payload)
        {
            if (str_payload == null || str_payload.Length == 0)
                return null;

            //const string dead_sensor_str = "DEAD_SENSOR";

            string expression = string.Copy(str_payload);

            // replace integers
            var reg_m = Regex.Matches(expression, "\"([a-zA-Z0-9|_]+)\"");
            foreach (Match v in reg_m)
            {
                string sens_name = v.Groups[1].ToString();

                ca_sensor sens = get_by_name(sens_name);
                if (sens != null && sens.is_valid() && sens.Enabled && sens.is_int())
                {
                    // replace
                    string sens_val = sens.get_int().ToString();
                    expression = expression.Replace(sens_name, sens_val);
                }
            }
            // replace strings
            reg_m = Regex.Matches(expression, "\"([a-zA-Z0-9|_]+)\\[([\\d|\\.|-]+)\\]\"");
            foreach (Match v in reg_m)
            {
                string match = v.Groups[0].ToString();
                string sens_name = v.Groups[1].ToString();
                ca_sensor sens = get_by_name(sens_name);
                if (sens != null && sens.is_valid() && sens.is_string())
                {
                    string sens_arg = v.Groups[2].ToString();
                    // there are 3 ways: single, range, all
                    // range
                    if (sens_arg.Contains('-'))
                    {
                        int from = 0, to = 0;
                        string[] from_to = sens_arg.Split('-');
                        if (from_to.Length == 2)
                        {
                            if (Tools.tryParseInt(from_to[0], out from) &&
                                Tools.tryParseInt(from_to[1], out to))
                            {
                                if (from >= 0 && to > from && to < sens.get_string().Length)
                                {
                                    string str_range = ascii_to_int(sens.get_string(), from, to - from);
                                    expression = expression.Replace(match, str_range);
                                }
                            }
                        }
                    }
                    // all
                    else if (sens_arg == "...")
                    {
                        string str_range = ascii_to_int(sens.get_string(), 0, sens.get_string().Length - 1);
                        expression = expression.Replace(match, str_range);
                    }
                    // single
                    else
                    {
                        int from_to = 0;
                        if (Tools.tryParseInt(sens_arg, out from_to))
                        {
                            string str_range = ascii_to_int(sens.get_string(), from_to, from_to);
                            expression = expression.Replace(match, str_range);
                        }
                    }
                }
            }

            // replace hex with dec
            // expression = Tools.strReplaceHexToDec(expression);
            // remove "
            expression = expression.Replace('\"', ' ');
            // remove spaces
            expression = Tools.removeSpaces(expression);

            // check for unsupported characters
            if (has_unsupported_chars(expression))
                return null;

            List<byte> res = new List<byte>();
            string[] str_byte_expr = expression.Split(',');

            foreach (string sval in str_byte_expr)
            {
                byte byte_val = 0;
                // evaluate the string expression
                object eval = MyCalc.evaluate(sval); //Tools.tryCalcString(sval);
                if (eval != null)
                {
                    decimal db = Convert.ToDecimal(eval);
                    if (db < 0)
                        db = 0;
                    UInt32 tmp = (UInt32)db;
                    if (tmp > 0xFF)
                         tmp &= 0xFF;

                    byte_val = Convert.ToByte(tmp);
                            //cache.Add(sval, byte_val);
                            // print
                            //if ((cache.Count % 50) == 0)
                            //    Debug.WriteLine("Cache len = {0}", cache.Count);
                }

                res.Add(byte_val);
            }

            return res;
        }
    }

    // tools class
    public class ca_tools
    {
        // null otherwise
        static public byte[] get_payload_if_str_constant(string str)
        {
            byte[] res = null;

            // get a tmp string
            string tmp_data_str = str != null ? string.Copy(str) : string.Empty;
            // check for unsupported simbols
            if (tmp_data_str.Contains('\"'))
                return null;
            // to dec
            tmp_data_str = Tools.strReplaceHexToDec(tmp_data_str);
            // remove spaces
            tmp_data_str = Tools.removeSpaces(tmp_data_str);
            // split 
            string[] str_b = tmp_data_str.Split(',');
            if (str_b.Length > 0)
            {
                List<byte> list = new List<byte>();
                foreach (var sb in str_b)
                {
                    object obj = MyCalc.evaluate(sb);
                    if (obj != null)
                    {
                        byte b = Convert.ToByte(obj);
                        list.Add(b);
                    }
                    else
                    {
                        // failed
                        return null;
                    }
                }

                res = list.ToArray();
            }

            return res;
        }


        static public byte[] convert_str_to_obd_bytes(string req)
        {
            byte[] res = null;
            int pos = 1;
            string[] str_req_b = req.Split(',');

            // check
            if (str_req_b.Length == 0)
                return null;

            // allocate
            res = new byte[str_req_b.Length + 1];
            // copy payload
            foreach (string sb in str_req_b)
            {
                object ob = MyCalc.evaluate(sb);
                if (ob != null)
                    res[pos] = Convert.ToByte(ob);
                pos++;
            }
            // payload len at the 1st pos
            res[0] = (byte)(pos - 1);
            return res;
        }
    }

    // request class
    public class ca_request
    {
        // OBD requests handling
        private class ca_request_obd
        {
            private bool m_is_waiting_for_flow = false;
            private bool m_is_broadcast_req = false;
            private long m_response_sent_at = 0;

            private readonly bool m_is_29_bit_mode = false;
            private readonly byte[] m_exp_req_bytes = null;
            private readonly int m_resp_id = 0;
            private readonly int m_req_id_start = 0, m_req_id_post_flow = 0;
            private readonly string m_str_response = string.Empty;

            private readonly bool m_is_valid = false;

            public string Response { get { return m_str_response; } }
            public bool IsValid { get { return m_is_valid; } }

            private readonly bool m_is_constant = false;
            private readonly List<byte> m_resp_buff_const = null;

            private int m_limit_resp_len = -1;
            private byte m_limit_resp_empty_byte = 0;

            // calculation optimisations
            private List<byte> m_tx_postflow = new List<byte>();

            public void set_reponse_limit_exact_len(int len)
            {
                m_limit_resp_len = len;
            }
            public void set_reponse_limit_empty_byte(byte b)
            {
                m_limit_resp_empty_byte = b;
            }

            public ca_request_obd(bool is_29_bit, int req_id, int resp_id, string str_request, string str_response)
            {
                m_is_29_bit_mode = is_29_bit;
                m_exp_req_bytes = ca_tools.convert_str_to_obd_bytes(str_request);
                m_req_id_start = req_id;
                m_resp_id = resp_id;
                m_str_response = str_response;

                // check the response, is it constant?
                byte[] const_payload = ca_tools.get_payload_if_str_constant(m_str_response);
                if (const_payload != null)
                {
                    m_is_constant = true;
                    m_resp_buff_const = new List<byte>(const_payload);
                }

                m_is_broadcast_req = false;
                if (!is_29_bit && m_req_id_start == 0x7DF)
                    m_is_broadcast_req = true;
                if (is_29_bit && m_req_id_start == 0x18DB33F1)
                    m_is_broadcast_req = true;
                if (m_req_id_start == 0)
                    m_is_broadcast_req = true;

                if (m_is_broadcast_req) {
                    // expected request if
                    if (m_req_id_start == 0)
                        m_req_id_start = is_29_bit ? 0x18DB33F1 : 0x7DF;
                    // expected response id (2)
                    m_req_id_post_flow = !is_29_bit ?
                        (m_resp_id - 8) : (0x18DA00F1 |
                        ((m_resp_id & 0xFF) << 8));
                } else
                {
                    m_req_id_post_flow = m_req_id_start;
                }

                m_is_valid = m_exp_req_bytes.Length > 0 &&
                    m_req_id_start != 0 &&
                    m_req_id_post_flow != 0 &&
                    m_resp_id != 0 &&
                    m_str_response != string.Empty && m_str_response != null;
            }

            public List<canMessage2> handle(canMessage2 msg, ca_sensor_list sensors)
            {
                List<canMessage2> res = null;

                // is valid?
                if (!m_is_valid)
                    return null;

                // check request id
                if (msg.Id.Id != m_req_id_start && msg.Id.Id != m_req_id_post_flow)
                    return null;
                // dlc
                if (msg.Id.Dlc != canMessageId.MaxDlc)
                    return null;

                bool is_multiframe = false;

                //Stopwatch stopWatch = null;
                //stopWatch = Stopwatch.StartNew();

                // check flow waiting state
                if (m_is_waiting_for_flow)
                {
                    long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if ((milliseconds - m_response_sent_at) > 100)
                    {
                        m_is_waiting_for_flow = false;
                        m_tx_postflow.Clear();
                    }
                }

                // looks like response, check it
                if (!m_is_waiting_for_flow && msg.Id.Id == m_req_id_start)
                {
                    if (m_exp_req_bytes.Length == msg.Data[0] + 1)
                    {
                        // compare
                        bool diff = false;
                        for (int i = 0; i < m_exp_req_bytes.Length; i++)
                        {
                            if (m_exp_req_bytes[i] != msg.Data[i])
                            {
                                diff = true;
                                break;
                            }
                        }

                        // is the expected request?
                        if (!diff)
                        {
                            // prepare the response
                            List<byte> tx = m_is_constant ?
                                m_resp_buff_const :
                                sensors.convert_payload_string(m_str_response);
                            res = bytes_to_messages(tx, !m_is_waiting_for_flow, m_resp_id, m_is_29_bit_mode, ref is_multiframe);
                            if (res != null && res.Count > 0)
                            {
                                if (is_multiframe)
                                {
                                    m_is_waiting_for_flow = true;
                                    m_response_sent_at = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                } else
                                {
                                    m_is_waiting_for_flow = false;
                                    m_response_sent_at = 0;
                                }
                            }

                            if (m_is_waiting_for_flow)
                            {
                                m_tx_postflow = tx;
                            }
                        }
                    }
                } else if (m_is_waiting_for_flow && msg.Id.Id == m_req_id_post_flow)
                {
                    if (msg.Data[0] == 0x30)
                    {
                        // prepare the response
                        List<byte> tx = m_tx_postflow;
                            /*m_is_constant ?
                            m_resp_buff_const :
                            sensors.convert_payload_string(m_str_response);
                            */
                        res = bytes_to_messages(tx, !m_is_waiting_for_flow, m_resp_id, m_is_29_bit_mode, ref is_multiframe);

                        if (res != null && res.Count > 0)
                        {
                            m_is_waiting_for_flow = false;
                            m_response_sent_at = 0;
                        }
                    }
                }

                // stopWatch.Stop();
                // long ts = stopWatch.ElapsedMilliseconds;
                // Debug.WriteLine("OBD Parsed: {0} ms", ts);

                return res;
            }

            private List<canMessage2> bytes_to_messages(List<byte> data, bool is_1st_response, int resp_id, bool is_29bit, ref bool multiframe)
            {
                if (data == null || data.Count == 0)
                    return null;
                if (resp_id == 0)
                    return null;

                List<canMessage2> res = new List<canMessage2>();

                multiframe = true;  // will be reset later

                // check for data limitations
                if (m_limit_resp_len != data.Count && m_limit_resp_len > 0)
                {
                    byte b = m_limit_resp_empty_byte;

                    // remove
                    if (data.Count > m_limit_resp_len)
                        data.RemoveRange(m_limit_resp_len, m_limit_resp_len - data.Count);
                    // add
                    while (data.Count < m_limit_resp_len)
                    {
                        data.Add(b);
                    }
                }

                // small frame
                if (data.Count <= 7)
                {
                    byte[] payload = new byte[8];
                    // clean
                    for (int i = 1; i < payload.Length; i++)
                        payload[i] = 0;
                    // frame len
                    payload[0] = (byte)data.Count;
                    // payload
                    for (int i = 0; i < data.Count; i++)
                    {
                        payload[i + 1] = (byte)data[i];
                    }

                    canMessage2 msg = new canMessage2(resp_id, is_29bit, payload, 0);
                    res.Add(msg);
                    multiframe = false;
                }
                else if (is_1st_response)
                {
                    // 0x10, len, data
                    int offset = 2;
                    byte[] payload = new byte[8];
                    // frame header byte
                    payload[0] = 0x10;
                    // frame len
                    payload[0] |= (byte)(data.Count >> 8);
                    payload[1] = (byte)(data.Count & 0xFF);
                    // payload
                    for (int i = offset; i < payload.Length; i++)
                    {
                        payload[i] = (byte)data[i - offset];
                    }

                    canMessage2 msg = new canMessage2(resp_id, is_29bit, payload, 0);
                    res.Add(msg);
                } else
                {
                    // 0x21, data
                    // 0x22, data
                    int offset = 6; // 6 data bytes has been already sent
                    int cur_data_pos = offset;
                    byte header_byte = 0x21;

                    while (cur_data_pos < data.Count)
                    {
                        byte[] payload = new byte[8];
                        // clean
                        for (int i = 1; i < payload.Length; i++)
                            payload[i] = 0;
                        // header
                        payload[0] = header_byte;
                        // data
                        for (int i = 1; i < payload.Length && cur_data_pos < data.Count; i++)
                        {
                            byte b = (byte)data[cur_data_pos++];
                            payload[i] = b;
                        }
                        canMessage2 msg = new canMessage2(resp_id, is_29bit, payload, 0);
                        res.Add(msg);

                        header_byte += 1;
                        if (header_byte >= 0x30)
                            header_byte = 0x20;
                    }
                }

                return res;
            }
        }

        private class ca_request_bmw
        {
            /*
                0.079 s.   0x6F1       5    0x10, 0x03, 0x22, 0xF1, 0x90
                0.079 s.   0x610       8    0xF1, 0x10, 0x14, 0x62, 0xF1, 0x90, 0x57, 0x42
                0.079 s.   0x6F1       4    0x10, 0x30, 0x00, 0x00
                0.080 s.   0x610       8    0xF1, 0x21, 0x41, 0x35, 0x56, 0x37, 0x31, 0x30
                0.080 s.   0x610       8    0xF1, 0x22, 0x32, 0x30, 0x41, 0x48, 0x38, 0x38
                0.080 s.   0x610       8    0xF1, 0x23, 0x39, 0x36, 0x36, 0xFF, 0xFF, 0xFF
            */

            private readonly int m_resp_id = 0;
            private readonly int m_req_id = 0;

            private bool m_is_waiting_for_flow = false;
            private long m_response_sent_at = 0;
            private readonly byte[] m_exp_req_bytes = null;
            private readonly string m_str_response = string.Empty;



            private readonly bool m_is_valid = false;

            private readonly bool m_is_constant = false;
            private readonly List<byte> m_resp_buff_const = null;

            private readonly byte exp_req_b0 = 0;

            public string Response { get { return m_str_response; } }
            public bool IsValid { get { return m_is_valid; } }

            public ca_request_bmw(bool is_29_bit, int req_id, int resp_id, string str_request, string str_response)
            {
                // reques address is always 0x6F1 (gateway)
                if (req_id == 0 || req_id == 0x6F1)
                    m_req_id = 0x6F1;
                else
                    m_req_id = -1;

                // response address should be between 0x600 and 0x6F1
                m_resp_id = resp_id;
                if (m_resp_id < 0x600 || m_resp_id >= 0x6F1)
                    m_resp_id = 0;
                else
                    exp_req_b0 = (byte)(m_resp_id - 0x600);

                // response string
                m_str_response = str_response;

                // expected request
                byte[] tmp_req = ca_tools.convert_str_to_obd_bytes(str_request);
                if (tmp_req != null && tmp_req.Length <= 6)
                {
                    m_exp_req_bytes = new byte[tmp_req.Length + 1]; // + address
                    m_exp_req_bytes[0] = exp_req_b0;
                    for (int i = 0; i < tmp_req.Length; i++)
                        m_exp_req_bytes[1 + i] = tmp_req[i];

                    tmp_req = null;
                }

                // check the response, is it constant?
                byte[] const_payload = ca_tools.get_payload_if_str_constant(m_str_response);
                if (const_payload != null)
                {
                    m_is_constant = true;
                    m_resp_buff_const = new List<byte>(const_payload);
                    const_payload = null;
                }
                /*
                // check the response, is it constant?
                byte[] const_payload = ca_tools.get_payload_if_str_constant(m_str_response);
                if (const_payload != null)
                {
                    m_is_constant = true;
                    m_resp_buff_const = new List<byte>();
                    m_resp_buff_const.Add(exp_req_b0);
                    m_resp_buff_const.AddRange(const_payload);
                }
                */
                m_is_valid = m_exp_req_bytes.Length > 0 &&
                    m_req_id > 0 &&
                    m_resp_id > 0 &&
                    m_str_response != string.Empty && m_str_response != null;
            }

            public List<canMessage2> handle(canMessage2 msg, ca_sensor_list sensors)
            {
                List<canMessage2> res = null;

                // is valid?
                if (!m_is_valid)
                    return null;

                // check request id
                if (msg.Id.Id != m_req_id)
                    return null;
                // check its destination
                if (msg.Data[0] != exp_req_b0)
                    return null;
                // dlc (min: address, mode)
                if (msg.Id.Dlc < 2 || msg.Id.Dlc > canMessageId.MaxDlc)
                    return null;

                // check flow waiting state
                if (m_is_waiting_for_flow)
                {
                    long milliseconds = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                    if ((milliseconds - m_response_sent_at) > 100)
                        m_is_waiting_for_flow = false;
                }

                bool is_multiframe = false;


                // looks like response, check it
                if (!m_is_waiting_for_flow)
                {
                    if (m_exp_req_bytes.Length <= msg.Id.Dlc)
                    {
                        // compare
                        bool diff = false;
                        for (int i = 0; i < m_exp_req_bytes.Length; i++)
                        {
                            if (m_exp_req_bytes[i] != msg.Data[i])
                            {
                                diff = true;
                                break;
                            }
                        }

                        // is the expected request?
                        if (!diff)
                        {
                            // prepare the response
                            List<byte> tx = m_is_constant ?
                                m_resp_buff_const :
                                sensors.convert_payload_string(m_str_response);
                            res = bytes_to_response(tx, !m_is_waiting_for_flow, m_resp_id, ref is_multiframe);
                            if (res != null && res.Count > 0)
                            {
                                if (is_multiframe)
                                {
                                    m_is_waiting_for_flow = true;
                                    m_response_sent_at = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                }
                                else
                                {
                                    m_is_waiting_for_flow = false;
                                    m_response_sent_at = 0;
                                }
                            }
                        }
                    }
                }
                else if (m_is_waiting_for_flow)
                {
                    if (msg.Data[1] == 0x30)
                    {
                        // prepare the response
                        List<byte> tx = m_is_constant ?
                            m_resp_buff_const :
                            sensors.convert_payload_string(m_str_response);

                        res = bytes_to_response(tx, !m_is_waiting_for_flow, m_resp_id, ref is_multiframe);

                        if (res != null && res.Count > 0)
                        {
                            m_is_waiting_for_flow = false;
                            m_response_sent_at = 0;
                        }
                    }
                }

                return res;
            }

            // can id       ecu     len     data
            // 0x6F1:       0x10,   0x03,   0x22, 0xF1, 0x90
            // 0x610:       0xF1,   0x10,   0x14, 0x62, 0xF1, 0x90, 0x57, 0x42
            // 0x6F1:       0x10,   0x30,   0x00, 0x00
            // 0x610:       0xF1,   0x21,   0x41, 0x38, 0x58, 0x35, 0x31, 0x30
            // 0x601:       0xF1,   0x22,   ...
            private List<canMessage2> bytes_to_response(List<byte> data, bool is_1st_response, int resp_id, ref bool multiframe)
            {
                if (data == null || data.Count == 0)
                    return null;
                if (resp_id == 0)
                    return null;
                if (resp_id < 0x600 || resp_id >= 0x6F1)
                    return null;

                const bool is29bit = false;
                byte resp_addr_byte = 0xF1;

                List<canMessage2> res = new List<canMessage2>();

                multiframe = true;  // will be reset later

                // small frame
                if (data.Count <= 6)
                {
                    byte[] payload = new byte[data.Count + 2];
                    // clean
                    for (int i = 1; i < payload.Length; i++)
                        payload[i] = 0;
                    // address
                    payload[0] = resp_addr_byte;
                    // frame len
                    payload[1] = (byte)data.Count;
                    // payload
                    for (int i = 0; i < data.Count; i++)
                    {
                        payload[i + 2] = (byte)data[i];
                    }

                    canMessage2 msg = new canMessage2(resp_id, is29bit, payload, 0);
                    res.Add(msg);
                    multiframe = false;
                }
                else if (is_1st_response)
                {
                    // 0x10, len, data
                    int offset = 3;
                    byte[] payload = new byte[8];
                    // address
                    payload[0] = resp_addr_byte;
                    // frame header byte
                    payload[1] = 0x10;
                    // frame len
                    payload[1] |= (byte)(data.Count >> 8);
                    payload[2] = (byte)(data.Count & 0xFF);
                    // payload
                    for (int i = offset; i < payload.Length; i++)
                    {
                        payload[i] = (byte)data[i - offset];
                    }

                    canMessage2 msg = new canMessage2(resp_id, is29bit, payload, 0);
                    res.Add(msg);
                }
                else
                {
                    // 0x21, data
                    // 0x22, data
                    int offset = 5; // 5 data bytes has been already sent
                    int cur_data_pos = offset;
                    byte header_byte = 0x21;

                    byte[] payload = new byte[8];

                    // address
                    payload[0] = resp_addr_byte;
                    while (cur_data_pos < data.Count)
                    {
                        // clean (skip address and header bytes)
                        for (int i = 2; i < payload.Length; i++)
                            payload[i] = 0xFF;
                        // header
                        payload[1] = header_byte;
                        // data
                        for (int i = 2; i < payload.Length && cur_data_pos < data.Count; i++)
                        {
                            byte b = (byte)data[cur_data_pos++];
                            payload[i] = b;
                        }
                        canMessage2 msg = new canMessage2(resp_id, is29bit, payload, 0);
                        res.Add(msg);
                        // update
                        header_byte += 1;
                        if (header_byte >= 0x30)
                            header_byte = 0x20;
                    }
                }

                return res;
            }
        }

        private List<ca_request_obd> m_req_obd_hndl = null;
        private ca_request_bmw m_req_bmw_hndl = null;

        public string Response()
        {
            string res = null;
            if (res == null && m_req_obd_hndl != null && m_req_obd_hndl.Count > 0)
            {
                foreach (var item in m_req_obd_hndl)
                {
                    if (item.IsValid)
                    {
                        res = item.Response;
                        break;
                    }
                }
            }
            if (res == null && m_req_bmw_hndl != null)
            {
                if (m_req_bmw_hndl.IsValid)
                    res = m_req_bmw_hndl.Response;
            }

            return res;
        }

        public void set_response_limitations(string resp_len, string resp_empty_byte)
        {
            object o_len = MyCalc.evaluate(resp_len);
            object o_empty_byte = MyCalc.evaluate(resp_empty_byte);

            byte limit_byte = (o_empty_byte == null) ? 
                (byte)(0) : Convert.ToByte(o_empty_byte);
            int limit_len = o_len == null ? -1 : Convert.ToInt32(o_len);

            // set
            if (m_req_obd_hndl != null)
            {
                foreach(var itm in m_req_obd_hndl)
                {
                    itm.set_reponse_limit_exact_len(limit_len);
                    itm.set_reponse_limit_empty_byte(limit_byte);
                }
            }
        }

        public ca_request(string proto, List<string> req_id, string req, string resp_id, string resp)
        {
            int tmp_resp_id = 0;

            m_req_obd_hndl = new List<ca_request_obd>();

            // hex to int
            string s_req = Tools.strReplaceHexToDec(req);
            string s_resp = Tools.strReplaceHexToDec(resp);
            object obj = null;

            // get response id
            obj = MyCalc.evaluate(resp_id);
            if (obj != null)
                tmp_resp_id = MyCalc.objToI32(obj);//Convert.ToInt32(obj);

            foreach(var item in req_id)
            {
                // try to get a request id
                obj = MyCalc.evaluate(item);
                if (obj != null)
                {
                    int new_req_id = Convert.ToInt32(obj);
                    if (proto == "OBD_11" || proto == "UDS_11")
                    {
                        m_req_obd_hndl.Add(
                            new ca_request_obd(false, new_req_id, tmp_resp_id, s_req, s_resp));
                    }
                    if (proto == "OBD_29" || proto == "UDS_29")
                    {
                        m_req_obd_hndl.Add(
                            new ca_request_obd(true, new_req_id, tmp_resp_id, s_req, s_resp));
                    }
                    if (proto == "BMW")
                    {
                        m_req_bmw_hndl = new ca_request_bmw(false, new_req_id, tmp_resp_id, s_req, s_resp);
                    }
                }
            }
        }

        public List<canMessage2> handle(canMessage2 rx, ca_sensor_list sensors)
        {
            List<canMessage2> tx_list = null;

            if (m_req_obd_hndl != null)
            {
                foreach (var item in m_req_obd_hndl)
                {
                    List <canMessage2> tmp_ls = item.handle(rx, sensors);
                    if (tmp_ls != null)
                    {
                        if (tx_list == null)
                            tx_list = new List<canMessage2>();
                        tx_list.AddRange(tmp_ls);
                    }
                    tmp_ls = null;
                }
            }

            if (m_req_bmw_hndl != null)
            {
                List<canMessage2> tmp_ls = null;
                tmp_ls = m_req_bmw_hndl.handle(rx, sensors);
                if (tmp_ls != null)
                {
                    if (tx_list == null)
                        tx_list = new List<canMessage2>();
                    tx_list.AddRange(tmp_ls);
                }
                tmp_ls = null;
            }

            return tx_list;
        }

        public List<canMessage2> handle(List<canMessage2> list, ca_sensor_list sensors)
        {
            List<canMessage2> tx_list = null;

            if (m_req_obd_hndl != null)
            {
                foreach (var item in m_req_obd_hndl)
                {
                    foreach (var rx in list)
                    {
                        List<canMessage2> tmp = null;
                        tmp = item.handle(rx, sensors);
                        if (tmp != null)
                        {
                            if (tx_list == null)
                                tx_list = new List<canMessage2>();
                            tx_list.AddRange(tmp);
                            tmp = null;
                        };
                    }
                }
            }

            if (m_req_bmw_hndl != null)
            {
                foreach (var rx in list)
                {
                    List<canMessage2> tmp = null;
                    tmp = m_req_bmw_hndl.handle(rx, sensors);
                    if (tmp != null)
                    {
                        if (tx_list == null)
                            tx_list = new List<canMessage2>();
                        tx_list.AddRange(tmp);
                        tmp = null;
                    }
                }
            }

            return tx_list;
        }

    };

    public class ca_flow
    {
        // common
        private readonly string name = string.Empty;
        public string Name { get { return name; } }
        private bool is_valid = false;
        // update properties
        private readonly long interval_ms = 0;
        public long NextUpdateMs { get { return update_at_ms; } }
        private long update_at_ms = 0;
        // message
        private readonly string str_data = string.Empty;
        private readonly int can_id = 0;
        private readonly bool is_29bit = false;
        public string Response { get { return str_data; } }
        // constant
        private byte[] data;
        private canMessage2 const_msg;
        private readonly bool is_constant;

        public bool IsValid { get { return is_valid; } }

        public ca_flow(string name_, int can_id_, bool is_29bit_, string str_data_, int interval_ms_)
        {
            name = name_;

            interval_ms = interval_ms_;
            update_at_ms = 0;
            can_id = can_id_;
            str_data = str_data_;
            is_constant = false;
            is_29bit = is_29bit_;

            int dlc = 0;
            // get a tmp string
            string tmp_data_str = str_data != null ? string.Copy(str_data) : string.Empty;
            // is this a constant value?
            byte[] const_payload = ca_tools.get_payload_if_str_constant(tmp_data_str);
            if (const_payload != null)
            {
                dlc = const_payload.Length;
                data = const_payload;
                is_constant = true;
            } else
            {
                // to dec
                tmp_data_str = Tools.strReplaceHexToDec(tmp_data_str);
                // split 
                string[] str_b = tmp_data_str.Split(',');
                dlc = str_b.Length;
            }

            // check
            if (dlc > 0 && dlc <= 8 && can_id_ > 0)
            {
                if (is_constant)
                {
                    // done
                    const_msg = new canMessage2(can_id_, is_29bit_, data);
                    is_valid = const_msg != null;
                }
                else
                {
                    is_valid = true;
                }
            }

            // finish
            is_valid = is_valid && interval_ms > 0 && Name != string.Empty && Name != null;
        }

        public canMessage2 handle (long now_ms, ca_sensor_list sensors)
        {
            canMessage2 res = null;

            // check
            if (!is_valid)
                return null;

            // is this time?
            if (now_ms >= update_at_ms)
            {
                // update ticks
                update_at_ms = now_ms + interval_ms;
                // prepare data
                if (is_constant)
                {
                    res = const_msg;
                }
                else
                {
                    // convert and create
                    List<byte> tx = sensors.convert_payload_string(str_data);
                    if (tx != null && tx.Count > 0 && tx.Count <= canMessageId.MaxDlc)
                    {
                        res = new canMessage2(can_id, is_29bit, tx.ToArray());
                    } else
                    {
                        // something went wrong, mark as invalid
                        //is_valid = false;
                    }
                }
            }

            return res;
        }

    }

    public class ca_flow_list
    {
        private List<ca_flow> m_list;

        public int Count { get { return m_list.Count; } }

        public ca_flow_list()
        {
            m_list = new List<ca_flow>();
        }

        public List<string> get_reponses()
        {
            List<string> res = new List<string>();

            foreach(var item in m_list)
            {
                res.Add(item.Response);
            }
            return res;
        }

        public void clear()
        {
            m_list.Clear();
        }

        public void add(ca_flow item)
        {
            if (get_index(item) < 0)
                m_list.Add(item);
        }

        private long get_now_ms(bool offset = false)
        {
            return 
                DateTimeOffset.Now.ToUnixTimeMilliseconds() + 
                (offset ? 5 : 0); // + 5 for better performance
        }

        public List<canMessage2> handle(ca_sensor_list sensors)
        {
            long now = get_now_ms(true);
            List<canMessage2> res = new List<canMessage2>();

            foreach (var item in m_list)
            {
                canMessage2 msg = item.handle(now, sensors);
                if (msg != null)
                    res.Add(msg);
            }

            return res;

        }

        public int next_update_in_ms()
        {
            long now = get_now_ms();
            long min = 0;

            foreach (var item in m_list)
            {
                long tmp = item.NextUpdateMs;
                if (tmp > 0)
                {
                    if (min == 0)
                        min = tmp;
                    else
                        min = tmp < min ? tmp : min;
                }  
            }

            long diff = min - now;
            return diff > 0 ? (int)diff : 0;
        }

        private int get_index(ca_flow item)
        {
            for (int i = 0; i < m_list.Count; i++)
                if (item.Name == m_list[i].Name)
                    return i;

            return -1;
        }
    }

    public enum sensor_update_mode
    {
        off,
        sine,
        random,
        up_then_restart,
    }

    public class sensor_update_item
    {
        public ca_sensor Sensor { set; get; }
        public sensor_update_mode Mode { set; get; }
        public int From { set; get; }
        public int To   { set; get; }
        public int Step { set; get; }
        public int IntervalMs { set; get; }
        public long LastUpdateAt { set; get; }
        public bool IsEditbale { get; }
        public bool Direction { set; get; } // 0 - up, 1 - down

        // constructor for an editable item
        public sensor_update_item(ca_sensor sensor, sensor_update_mode mode, int from, int to, int step, int interval_ms)
        {
            Sensor = sensor;
            Mode = mode;
            From = from;
            To = to;
            Step = step;
            IntervalMs = interval_ms;
            LastUpdateAt = 0;
            IsEditbale = true;
        }

        // constructor for a NON-editable item
        public sensor_update_item(ca_sensor sensor)
        {
            Sensor = sensor;
            Mode = sensor_update_mode.off;
            IsEditbale = false;
        }

        // reset
        public void reset()
        {
            LastUpdateAt = 0;
        }

        // update
        public void update(sensor_update_item item)
        {
            if (Sensor.get_name() != item.Sensor.get_name())
                return;

            Mode = item.Mode;
            From = item.From;
            To = item.To;
            Step = item.Step;
            IntervalMs = item.IntervalMs;
        }
    }

    // sensor updater class
    public class sensor_updater
    {
        private List<sensor_update_item> m_update_list;
        private Random m_rnd;

        // constructor
        public sensor_updater()
        {
            m_update_list = new List<sensor_update_item>();
            m_rnd = new Random();
        }

        // get an item index
        private int get_index(string sensor_name)
        {
            for (int i = 0; i < m_update_list.Count; i++)
                if (m_update_list[i].Sensor.get_name() == sensor_name)
                    return i;

            return -1;
        }

        // get an item index
        private int get_index(sensor_update_item item)
        {
            return get_index(item.Sensor.get_name());
        }

        // is the item exist?
        public bool is_exist(sensor_update_item item)
        {
            return get_index(item) >= 0;
        }

        // add a new item
        public void add(sensor_update_item item)
        {
            // is already exist?
            if (is_exist(item))
                return;
            // add
            m_update_list.Add(item);
        }

        // update the item
        public void update_data(sensor_update_item new_item)
        {
            int idx = get_index(new_item);
            if (idx >= 0)
            {
                m_update_list[idx].update(new_item);
            }
        }

        // update sensor values
        public bool refresh_sensors()
        {
            long now_ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            bool updated = false;

            for (int i = 0; i < m_update_list.Count; i++)
            {
                // is refresh required?
                var item = m_update_list[i];
                if (item.Mode == sensor_update_mode.off)
                {
                    item.reset();
                    continue;
                }

                // is timeout expired?
                long diff = now_ms - item.LastUpdateAt;
                if (item.LastUpdateAt != 0 && diff < item.IntervalMs)
                    continue;

                // check it
                var sensor = item.Sensor;
                if (!sensor.is_valid() || !sensor.Enabled)
                    continue;

                // update
                if (item.Mode == sensor_update_mode.random)
                {
                    if (sensor.is_int())
                    {
                        int val = m_rnd.Next(item.From, item.To + 1);
                        sensor.set_int(val);
                    }
                }
                else if (item.Mode == sensor_update_mode.sine)
                {
                    int val = sensor.get_int();

                    if (item.Direction == false)
                        val += item.Step;
                    else
                        val -= item.Step;

                    if (val > item.To)
                    {
                        val = item.To;
                        item.Direction = !item.Direction;
                    } else if (val < item.From)
                    {
                        val = item.From;
                        item.Direction = !item.Direction;
                    }

                    sensor.set_int(val);
                }
                else if (item.Mode == sensor_update_mode.up_then_restart)
                {
                    item.Direction = false;
                    int val = sensor.get_int();
                    val += item.Step;
                    if (val > item.To)
                        val = item.From;
                    sensor.set_int(val);
                }

                // finish
                item.LastUpdateAt = now_ms;
                updated = true;
            }
            return updated;
        }
    }
}