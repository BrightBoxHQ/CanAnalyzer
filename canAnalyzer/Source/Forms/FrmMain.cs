using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using canSerialPort;
using System.Collections.Concurrent;
using System.Threading;
using System.Configuration;
using System.IO;
using System.Diagnostics;
using Microsoft.Win32;

namespace canAnalyzer
{
    public partial class FrmMain : Form
    {
        // default number of script tabs, use the app config file if you want to use your own value
        private int scriptNum = 7;

        #region Fields

        // CAN sending tool
        private CanMessageSendTool      canTool;

        // user controls (gui)  
        private UcCanReadGrid           ucReadGrid;
        private UcCanTrace              ucReadTrace;
        private UcSendMessage           ucSendGrid;
        private ucActivationCodeSearcher ucCanAct;
        private List<ucScript>          ucScriptList;
        private ucGenericTest           ucGenericTest = null;
        private ucTestCA                ucCaractionTest = null;
        private ucBruteForcer           ucBruteForceTool = null;

        // forms
        private FrmConnect          frmConnect;
        private ConnSettingsCan     connSettings;
        
        // can worker
        private canWorker m_worker;
        // can message queue
        private ConcurrentBag<canMessage2> m_queueMsgs;

        // status tool
        private uiStatus m_statusTool;

        // is resizing
        private bool resizing = false;

        // UI update worker (thread) and its flag
        private Thread guiUpdateWorker = null;
        private bool gui_update_stop_req = false;

        // unique CAN message list
        canMessageList2 m_list = new canMessageList2();
        // filter
        canFilter CanFilter { get; set; }
        // mask
        canMessageMaskFilter maskFilter = new canMessageMaskFilter();

        #endregion

        // constructor
        public FrmMain()
        {
            // std initializer
            InitializeComponent();

            // app title
            Text = string.Format("{0} {1}",
                Application.ProductName, Application.ProductVersion);

            // create an empty software CAN filter
            CanFilter = new canFilter();
            // connection settings
            connSettings = new ConnSettingsCan();
            connSettings = AppSettings.LoadConnSettings();

            // create the CAN worker
            m_queueMsgs = new ConcurrentBag<canMessage2>();
            m_worker = new canWorker(ref m_queueMsgs);
            m_worker.CanFilter = CanFilter;  

            // CAN message sending tool for extarnal modules
            canTool = new CanMessageSendTool(m_worker);

            // create the CAN message grid
            ucReadGrid = new UcCanReadGrid(canTool);        // create
            tableLayoutData.Controls.Add(ucReadGrid);
            ucReadGrid.Show();
            ucReadGrid.CanFilter = CanFilter;               // set the filter

            // UI: tab items
            addTabItems();

            // UI: status tool
            m_statusTool = new uiStatus(ref this.m_status);
            m_statusTool.setPort(connSettings.PortName);
            m_statusTool.setSpeed(connSettings.CanSpeed);
            m_statusTool.setSilentMode(connSettings.IsSilent);
            m_statusTool.setAutosaveTrace(connSettings.IsTraceAutoSave);
            m_statusTool.showBusErrors(AppSettings.LoadCanBusErrorMonitorConfig());

            menuStrip.BackColor = System.Drawing.SystemColors.ActiveCaption;

            // ???
            TableLayoutColumnStyleCollection columnStyles = tableLayoutData.ColumnStyles;
            columnStyles[0].SizeType = SizeType.Absolute;
            columnStyles[0].Width = ucReadGrid.getNecessaryWidth();
            
            // disable mask for a while
            maskToolStripMenuItem.Enabled = true;

            // UI update thread
            guiUpdateWorker = new Thread(onGuiUpdateWorker);
            guiUpdateWorker.Name = "Main UI Updater";
            guiUpdateWorker.Start();

            // start the new CAN worker
            newCanWorkerStart();

            // OS power management
            SystemEvents.PowerModeChanged += OnPowerModeChange;

            toolsToolStripMenuItem.DropDownItemClicked += ToolsToolStripMenuItem_DropDownItemClicked;

            // manual UI update
            guiControl();
        }

        // create user controls and add them as tab items
        private void addTabItems()
        {
            // trace
            ucReadTrace = new UcCanTrace(canTool);
            ucReadTrace.Dock = DockStyle.Fill;
            TabPage tabTrace = new TabPage("Trace");
            tabTrace.Controls.Add(ucReadTrace);
            tabControl.Controls.Add(tabTrace);

            // send
            ucSendGrid = new UcSendMessage(canTool);
            TabPage tabSend = new TabPage("Transmit");
            tabSend.Controls.Add(ucSendGrid);
            tabControl.Controls.Add(tabSend);

            // test
            ucGenericTest = new ucGenericTest(canTool);
            TabPage tabGeneric = new TabPage("Scan Car");
            tabGeneric.Controls.Add(ucGenericTest);
            tabControl.Controls.Add(tabGeneric);
            // generic data path
            if (ucGenericTest != null)
                ucGenericTest.dataPathSet(AppSettings.LoadGenericDataPath());

            // CA tester
            if (ucTestCA.canBeCreated())
            {
                ucCaractionTest = new ucTestCA(canTool);
                //ucCaractionTest.setMessageTool(new uiMessageTool(this));
                TabPage tabTestCA = new TabPage("Test CA");
                tabTestCA.Controls.Add(ucCaractionTest);
                tabControl.Controls.Add(tabTestCA);
            }
            else
            {
                ucCaractionTest = null;
            }

            // Brute forcer
            ucBruteForceTool = new ucBruteForcer(canTool);
            TabPage tabBruteForcer = new TabPage("Brute Forcer");
            tabBruteForcer.Controls.Add(ucBruteForceTool);
            tabControl.Controls.Add(tabBruteForcer);
            if (ucBruteForceTool != null)
                ucBruteForceTool.dataPathSet(AppSettings.LoadBrutforcerDataPath());

            // scripts
            scriptNum = AppSettings.LoadScriptNumber();
            if (scriptNum > 0)
                ucScriptList = new List<ucScript>();
            for (int i = 0; i < scriptNum; i++)
            {
                string tabName = string.Format("Script {0}", i + 1);
                ucScriptList.Add(new ucScript(canTool));
                TabPage tab = new TabPage(tabName);
                tab.Controls.Add(ucScriptList.Last());
                tabControl.Controls.Add(tab);
            }
            // config
            
            if (ucScriptList != null)
            {
                var scriptTraceLinesLimit = AppSettings.LoadScriptTraceLimitLines();
                var scriptUdsTmo1_ms = AppSettings.LoadScriptUdsRxTimeout1();
                var scriptUdsTmo2_ms = AppSettings.LoadScriptUdsRxTimeout2();
            
                for (int i = 0; i < ucScriptList.Count; i++)
                {
                    ucScriptList[i].setTraceLineLimit(scriptTraceLinesLimit);
                    ucScriptList[i].setUdsTimeouts(scriptUdsTmo1_ms, scriptUdsTmo2_ms);
                }
            }

            // CAN activation (wake-up)
            ucCanAct = new ucActivationCodeSearcher(canTool);
            ucCanAct.Dock = DockStyle.Fill;
            TabPage tabActCodes = new TabPage("Bus Activation");
            tabActCodes.Controls.Add(ucCanAct);
            tabControl.Controls.Add(tabActCodes);
        }

        // UI update stuff
        #region ui_updater

        // UI updater wait handle
        private EventWaitHandle m_gui_update_wait_handle = 
            new EventWaitHandle(false, EventResetMode.AutoReset);

        // force UI update
        public void guiUpdateForce()
        {
            m_gui_update_wait_handle.Set();
        }

        // UI update thread
        private void onGuiUpdateWorker()
        {
            while (!gui_update_stop_req)
            {
                // wait
                m_gui_update_wait_handle.WaitOne(1000);
                // update
                if (!gui_update_stop_req)
                    guiControl();
            }
        }

        // Form UI updater
        public void guiControl()
        {
            if (this.Disposing || this.IsDisposed)
                return;

            // invoke
            if (InvokeRequired)
            {
                /*Invoke*/
                BeginInvoke(new Action(guiControl));
                return;
            }

            // prepare
            bool is_communication = isCommunication();
            string str_conn = isCommunication() ? "Disconnect" : "Connect";
            bool tmp = false;

            // connect
            tmp = !string.IsNullOrEmpty(connSettings.PortName) &&
                  !string.IsNullOrEmpty(connSettings.CanSpeed);
            if (connectToolStripMenuItem.Enabled != tmp)
                connectToolStripMenuItem.Enabled = tmp;

            // settings
            tmp = !isCommunication();
            if (settingsToolStripMenuItem.Enabled != tmp)
                settingsToolStripMenuItem.Enabled = tmp;

            // reset
            tmp = null == m_list || !m_list.isEmpty();
            if (resetToolStripMenuItem.Enabled != tmp)
                resetToolStripMenuItem.Enabled = tmp;

            // has been just disconnected
            if (str_conn != connectToolStripMenuItem.Text && !is_communication)
                communicationStop();

            // connect
            if (connectToolStripMenuItem.Text != str_conn)
                connectToolStripMenuItem.Text = str_conn;
        }

        #endregion

        // communication
        #region communication

        // start the new communication
        private bool communcationStart()
        {
            // is running?
            if (isCommunication())
                return false;

            if (!comPortEnumerator.isPortAvailable(connSettings.PortName))
            {
                // selected port is not available
                // try to do rescan and use a new one (if only one port is available)
                string newPort = FrmConnect.scanGetPortIfOnlyOneIsAwail();
                if (!string.IsNullOrEmpty(newPort))
                {
                    connSettings.PortName = newPort;
                }
                else
                {
                    string caption = "Communication Error";

                    MessageBox.Show(this, 
                        "Selected port is not available.\r\n" + 
                        "Please make sure the device is connected.",
                        caption);

                    return false;
                }
            }

            bool isSpeedAuto = connSettings.CanSpeed.ToLower().Contains("auto");

            // try to establish the connection
            bool res = false;
            if (m_worker.setComPort(connSettings.PortName) && 
                m_worker.setCanSpeed(connSettings.CanSpeed) && 
                m_worker.setSilentMode(connSettings.IsSilent))
                res = m_worker.start();

            if (res)
            {
                // get speed
                connSettings.CanSpeed = m_worker.getCanSpeed();
                //connSettings.PortName = port;

                // the main performance issue is FTDI latency. 
                // It can be updated using the Windows regisrty
                // check it and then try to update
                if (!m_worker.isLatencyCorrect())
                {
                    string caption = "COM port driver";

                    if (!m_worker.ftdiLatencyUpdate())
                    {
                        // failed to update, the registry cannot be updated under a regular user
                        MessageBox.Show("Warning." + Environment.NewLine + 
                            "The FTDI VCP driver* latency value is too high. " +
                            "This can lead to performance issues, but you still can use basic features." + 
                            Environment.NewLine + Environment.NewLine +
                            "How to fix:" + Environment.NewLine +
                            " 1. Restart the application as Administrator." + Environment.NewLine +
                            " 2. Establish the connection to your CAN device (click the 'Connect' button)." +
                            Environment.NewLine +
                            " 3. Wait for the 'FTDI Latency updated' message." + Environment.NewLine +
                            " 4. Restart the application." + Environment.NewLine + Environment.NewLine +
                            "* FTDI driver is a Virtual COM Port (VCP) driver we use for our CAN Analyzer hardware. " +
                            "The driver has an RX latency config. It is strongly recommended to update this value " +
                            "(by following the steps given above) to provide the best app performance. " +
                            "Otherwise, it can lead to incorrect operation of the CAN requests (Car Scan, Scripts, etc.) ",
                            caption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Warning);
                    } else
                    {
                        MessageBox.Show("FTDI Driver Latency updated. Please restart the application.",
                            caption,
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Exclamation);
                    }
                }
            }
            else
            {
                string caption = "Connection failed";
                string message = "";
           
                if (isSpeedAuto)
                {
                    message = string.Format(
                        "Selected port '{0}' cannot be openned.\n\n" +
                        "Possible reasons:\n\n" +
                        "1. the COM port is not available/busy\n\n" +
                        "2. Speed=Auto. It is possible that the correct speed value " +
                        "cannot be detected automatically.\n\n" +
                        "Info: Our speed detection algorithm uses the existing CAN flow data. " +
                        "In case of lack of messages on the CAN bus, the speed value cannot be detected.",
                        connSettings.PortName);
                }
                else
                {
                    message = string.Format(
                        "Selected port '{0}' cannot be openned. Make sure it's available and not busy.",
                        connSettings.PortName);
                }
                MessageBox.Show(message, caption,
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }

            // update the status
            m_statusTool.set(connSettings.PortName, connSettings.CanSpeed, 
                m_worker.getFwVersion(), res, maskFilter.isFilterExists(),
                connSettings.IsSilent, connSettings.IsTraceAutoSave, null);

            AppSettings.SaveConnSettings(connSettings);

            // controls
            if (res)
            {
                ucReadTrace.setCanSpeed(connSettings.CanSpeed);
                ucSendGrid.onConnect();
            }

            return res;
        }

        // stop the current communication
        private bool communicationStop()
        {
            /* stop the worker */
            m_worker.stop();
            while (m_worker.isPortOpen());
            /* stop generic */
            if (ucGenericTest != null)
                ucGenericTest.stop();
            /* stop all the scripts */
            for (int i = 0; i < ucScriptList.Count; i++)
                ucScriptList[i].Stop();
            /* update the conn status*/
            m_statusTool.setConnectionStatus(false);
            /* stop sending messages */
            ucSendGrid.onDisconnect();

            return true;
        }

        // is communication started?
        public bool isCommunication()
        {
            return m_worker.isPortOpen();
        }

        // send a CAN messge (rtr is not supported)
        public bool sendCanMessage(canMessage2 msg, int count = 1, bool rtr = false)
        {
            if (isCommunication())
            {
                for( int i = 0; i < count; i++)
                    m_worker.sendMessge(msg, rtr);
                return true;
            }
            return false;
        }

        #endregion

        // tests
        #region tests
        private int ts_test = 0;

        // test func, it wooks like emulator in case I do not have any can hacker device
        private void test ()
        {
            Random rnd = new Random();
            //if (ts > 0)
            //    return;
            const int msg_id_cnt = 50;
            const int msg_per_id_cnt = 50;

            for (int idx = 0; idx < msg_id_cnt; idx++) {
                byte[] data = { 0, 1, 2, 3, 4, 5, 6, 7 };

                if( idx != 0 && idx != 3 )
                    data[0] = (byte)rnd.Next(0, 10);

                data[1] = (byte)rnd.Next(0, 3);
                data[3] = (byte)rnd.Next(0, 0x20);
                canMessage2 m2 = new canMessage2(idx, false, data, ts_test + 400);
                m_queueMsgs.Add(m2);

                data[1] = 1;
                canMessage2 m = new canMessage2(idx, false, data, ts_test + 300);
                m_queueMsgs.Add(m);
                data[1] = 2;
                m = new canMessage2(idx, false, data, ts_test + 350);
                data[3] = (byte)rnd.Next(0, 0xFF);
                m_queueMsgs.Add(m);
                m = new canMessage2(idx, false, data, ts_test + 300);
                m_queueMsgs.Add(m);
                for (int mcnt = msg_per_id_cnt - 4; mcnt > 0; mcnt--)
                {
                    m = new canMessage2(idx, false, data, ts_test + mcnt);
                    m_queueMsgs.Add(m);
                }

                
            }
            ts_test += 500;
            //byte[] data2 = { 0, 1, 6, 3, 4, 0 };
            //canMessage2 m5 = new canMessage2(0x1fffffff, true, data2, ts );
            //m_queueMsgs.Add(m5);

            //canMessagePerioded m32 = new canMessagePerioded();
        }
        #endregion

        // optimized message handler
        #region newMessageHandler

        // thread
        private Thread m_new_can_worker = null;
        private Thread m_new_can_worker_slow = null;
        private List<canMessage2> m_new_can_rx_slow_list = new List<canMessage2>();
        private Mutex m_new_can_rx_slow_mutex = new Mutex();

        // stop flag
        private bool m_new_can_worker_stop_req = false;
        // hi performance worker mode
        private bool m_can_high_performance_mode = false;
        // previos connection state
        private bool m_new_can_rx_slow_conn_state_prev = false;

        // reset flags
        private bool m_can_reset_request = false;
        private bool m_can_reseting_slow = false;

        // pendind CAN message list
        private List<canMessage2> pending_msg_list = new List<canMessage2>();

        // clear slow control list
        private void canClearSlowControls()
        {
            // lock
            m_new_can_rx_slow_mutex.WaitOne();
            // clear
            m_new_can_rx_slow_list.Clear();
            pending_msg_list.Clear();
            // unlock
            m_new_can_rx_slow_mutex.ReleaseMutex();
        }

        // handler for the most critial controls, such as scripts
        private void canHandleFastControls(List<canMessage2> list)
        {
            bool hi_prefromance = false;

            // append to the test CA (nEmulator)
            if (ucCaractionTest != null)
            {
                ucCaractionTest.addMessageList(list);
                hi_prefromance |= ucCaractionTest.isRunning();
            }    
            // append to the generic test
            if (ucGenericTest != null)
            {
                ucGenericTest.addMessageList(list);
                hi_prefromance |= ucGenericTest.isRunning();
            } 
            // append to the scripts
            foreach (var script in ucScriptList)
            {
                script.addMessageList(list);
                hi_prefromance |= script.isRunning();
            }
            // append to the brute forcer
            if (ucBruteForceTool != null)
            {
                ucBruteForceTool.addMessage(list);
                hi_prefromance |= ucBruteForceTool.isRunning();
            }

            // append to the actication code handles
            ucCanAct.pushMessageList(list);

            if (hi_prefromance != m_can_high_performance_mode)
            {
                m_worker.highPefrormanceModeSet(hi_prefromance);
                m_can_high_performance_mode = hi_prefromance;

                Debug.WriteLine( string.Format("High performance mode {0}", 
                    hi_prefromance ? "Enabled" : "Disabled"));
            }
        }

        // handler for NON-critical controls, such as trace
        private void canHandleSlowControls(List<canMessage2> list)
        {
            if (list.Count == 0)
                return;

            // lock
            m_new_can_rx_slow_mutex.WaitOne();
            // append
            m_new_can_rx_slow_list.AddRange(list);
            // unlock
            m_new_can_rx_slow_mutex.ReleaseMutex();
        }

        // invokable update function for NON-critical controls
        private void canHandleSlowControlsDoGui(List<canMessage2> ls)
        {
            if (this.Disposing || this.IsDisposed)
                return;

            // invoke
            if (InvokeRequired)
            {
                BeginInvoke(new Action<List<canMessage2>>(canHandleSlowControlsDoGui), ls);
                return;
            }

            // get selcted items (for the tracer)
            bool[] lsSelected = new bool[ls.Count];

            bool push = true;
            bool force_push = false;

            // clean
            if (m_can_reseting_slow)
            {
                // clean the queues
                pending_msg_list.Clear();
                // clear the data list
                m_list.clear();
                // clean the UCs
                ucReadGrid.clear(); 
                ucReadTrace.clear();
                // reset the flag and finish
                m_can_reseting_slow = false;
                return;
            }            

            // handle communication state changes
            bool is_communication = isCommunication();
            if (is_communication != m_new_can_rx_slow_conn_state_prev)
            {
                // just stopped
                if (!is_communication)
                {
                    force_push = true;
                    if (ucReadTrace != null)
                        ucReadTrace.disconnected();
                }
                else
                {
                    if (ucReadTrace != null)
                        ucReadTrace.save_data_allow(connSettings.IsTraceAutoSave);
                }

                // update
                m_new_can_rx_slow_conn_state_prev = is_communication;
            }

            // update trace and grid every push_ui_each_msec ms
            if ((!resizing && push) || force_push)
            {
                // last_trace_msec = now;
                if (pending_msg_list.Count > 0)
                {
                    List<canMessage2> tmp = new List<canMessage2>();
                    tmp.AddRange(pending_msg_list);
                    tmp.AddRange(ls);
                    ls = tmp;
                    pending_msg_list.Clear();
                }

                
                UniqueDataSetCan selMessages = ucReadGrid.getSelected();

                // add to the list
                m_list.add(ls);

                // this uc is too slow
                // we can reduce time a bit if we send not one message but a list
                // grid
                if (ucReadGrid != null)
                    ucReadGrid.pushMessageList(ls);

                // trace
                if (ucReadTrace != null)
                    ucReadTrace.pushList(ls, selMessages);

                // ui counters
                m_statusTool.setReceivedMsgCount(m_worker.Received);
                m_statusTool.setMaskFilterState(maskFilter.isFilterExists());

                // errors
                var err = m_worker.errorsGet();
                m_statusTool.setErrors(err);
            }
            else
            {
                // add to the pending queue
                pending_msg_list.AddRange(ls);
            }
        }

        // worker fot NON-critical controls
        private void canHandleSlowWorker()
        {
            while (!m_new_can_worker_stop_req)
            {
                // wait
                int tmo = m_can_high_performance_mode ? 500 : 200;
                int step = 100;

                while (tmo > 0 && !m_new_can_worker_stop_req)
                {
                    Thread.Sleep(step);
                    tmo -= step;
                }

                // lock
                m_new_can_rx_slow_mutex.WaitOne();
                // get all the new messages
                List<canMessage2> ls = new List<canMessage2>(m_new_can_rx_slow_list);
                m_new_can_rx_slow_list.Clear();
                // unlock
                m_new_can_rx_slow_mutex.ReleaseMutex();

                // do
                if (!m_new_can_worker_stop_req && !this.Disposing && !this.IsDisposed)
                {
                    canHandleSlowControlsDoGui(ls);
                }
            }
        }

        // stop the worker
        private void newCanWorkerStop()
        {
            m_new_can_worker_stop_req = true;
        }

        // start the worker
        private void newCanWorkerStart()
        {
            if (newCanWorkerIsRunning())
                return;
            m_new_can_worker_stop_req = false;
            m_new_can_worker = new Thread(newCanWorkerRoutine);
            m_new_can_worker.Name = "New CAN Worker";
            m_new_can_worker.Start();
        }

        // is the worker running?
        private bool newCanWorkerIsRunning()
        {
            if (null == m_new_can_worker)
                return false;

            return m_new_can_worker.ThreadState ==
                System.Threading.ThreadState.Stopped || m_new_can_worker_stop_req ? false : true;
        }

        // this thread is getting CAN messages and then sending them to the handlers
        private void newCanWorkerRoutine()
        {
            List<canMessage2> mList = new List<canMessage2>();
            canMessage2 msg = new canMessage2();

            // start the worker
            m_new_can_worker_slow = new Thread(canHandleSlowWorker);
            m_new_can_worker_slow.Name = "Main routine: slow";
            m_new_can_worker_slow.Start();

            bool cleaning = false;
            bool was_connected = false;
            bool read_can_errors = AppSettings.LoadCanBusErrorMonitorConfig();
            long can_errors_ts = 0;

            // work until the flag is set
            while (!m_new_can_worker_stop_req && !this.Disposing)
            {
                bool is_connected = isCommunication();
                if (!was_connected && is_connected)
                    m_worker.errorsClean();
                was_connected = is_connected;

                // reset
                if (m_can_reset_request)
                {
                    m_can_reset_request = false;

                    // pause the worker
                    m_worker.set_pause(true);

                    // set-up the flags
                    cleaning = true;
                    m_can_reseting_slow = true;

                    // force clean
                    BeginInvoke(new Action(ucReadGrid.clear));
                    BeginInvoke(new Action(ucReadTrace.clear));
                }

                // clean
                if (cleaning)
                {
                    // empty the queue
                    while (true == m_queueMsgs.TryTake(out msg)) { }

                    if (!m_can_reseting_slow)
                    {
                        // clean
                        // app ver 1.1 had a possible infinitive loop here
                        // so I changed reset implementation and added the timeout
                        ulong prev_msg_cnt = m_worker.Received;
                        int wrkr_reset_tmo = 1200;

                        while (wrkr_reset_tmo > 0)
                        {
                            const int step = 20;
                            int attempt_tmo = 400;
                            bool done = false;

                            // reset
                            m_worker.Reset();

                            // wait
                            while (attempt_tmo > 0 && wrkr_reset_tmo > 0 && !done)
                            {
                                // check
                                ulong rcvd = m_worker.Received;
                                if (rcvd == 0 || rcvd < prev_msg_cnt)
                                    done = true;
                                // wait
                                if (!done)
                                {
                                    Thread.Sleep(step);
                                    attempt_tmo -= step;
                                    wrkr_reset_tmo -= step;
                                }
                            }

                            if (done)
                                break;
                        }

                        Debug.WriteLine(
                            string.Format("Reset done, ms = {0}", wrkr_reset_tmo)
                        );

                        // clear filter
                        CanFilter.clear();
                        // finished
                        cleaning = false;
                        m_worker.set_pause(false);
                        guiUpdateForce();
                    }
                }

                // get all the new messages 
                while (true == m_queueMsgs.TryTake(out msg))
                {
                    if (maskFilter.update(msg))
                        mList.Add(msg);
                }

                // push the messages into the most critical modules
                canHandleFastControls(mList);
                // into other modules
                canHandleSlowControls(mList);

                // clear the list
                mList.Clear();

                // make sure the queue is empty
                // waiting for new messages
                if (m_queueMsgs.Count == 0)
                    m_worker.waitHandle.WaitOne(500);

                // CAN errors, read no more than once every 450ms
                if (read_can_errors && can_errors_ts < DateTimeOffset.Now.ToUnixTimeMilliseconds())
                {
                    m_worker.errorsRefresh();
                    can_errors_ts = DateTimeOffset.Now.ToUnixTimeMilliseconds() + 450;
                }
                    

                //test();
            }

            // kill the thread
            while (m_new_can_worker_slow.ThreadState != System.Threading.ThreadState.Stopped)
                Thread.Sleep(100);
        }

        #endregion

        // Form events
        #region events_form

        // begin resizing
        protected override void OnResizeBegin(EventArgs e)
        {
            resizing = true;
            // trying to impove performance
            SuspendLayout();
            base.OnResizeBegin(e);
        }

        // stop resizing
        protected override void OnResizeEnd(EventArgs e)
        {
            resizing = false;
            // trying to improve performance
            ResumeLayout();
            base.OnResizeEnd(e);
        }

        // on form closing (for safety thread killing)
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // kill the GUI updater
            gui_update_stop_req = true;
            guiUpdateForce();
            // stop the message handler
            newCanWorkerStop();
            // stop the current communication
            communicationStop();

            // scripts
            foreach (var script in ucScriptList)
            {
                script.Stop();
                script.Dispose();
            }
            // CA test
            if (ucGenericTest != null)
            {
                ucGenericTest.stop();

                AppSettings.SaveGenericDataPath(ucGenericTest.dataPathGet());
                ucGenericTest.Dispose();
            }
            // CA nemulator
            if (ucCaractionTest != null)
            {
                ucCaractionTest.stop();
                ucCaractionTest.Dispose();
            }
            // grid
            if (ucReadGrid != null)
            {
                ucReadGrid.Dispose();
            }
            // trace
            if (ucReadTrace != null)
            {
                ucReadTrace.stop();
                ucReadTrace.Dispose();
            }
            // brute
            if (ucBruteForceTool != null)
            {
                ucBruteForceTool.stop();

                AppSettings.SaveBrutforcerDataPath(ucBruteForceTool.dataPathGet());
                ucBruteForceTool.Dispose();
            }    

            // wait
            while (m_new_can_worker.ThreadState == System.Threading.ThreadState.Running ||
                   guiUpdateWorker.ThreadState == System.Threading.ThreadState.Running)
            {
                Thread.Sleep(50);
            }

            // wait to make sure
            Thread.Sleep(100);

            Debug.WriteLine("App Closed");
        }

        // event: power mode change
        private void OnPowerModeChange(object s, PowerModeChangedEventArgs e)
        {
            if (e.Mode == PowerModes.Suspend)
            {
                // suspend - stop the communication
                communicationStop();
            }
        }

        #endregion

        // items events
        #region events_items

        private void menu_callback_common(string str_item)
        {
            bool force_ui_update = false;

            str_item = str_item.ToLower();

            // take a picture
            if (str_item == "screenshot")
            {
                // store
                ScreenCapture.captureAppScreen(this);
                // message
                uiMessageTool.ShowMessage(string.Format("Image stored at {0}",
                    ScreenCapture.getDateTimeBasedDirPath()));
            }

            // find a value
            if (str_item == "find a value")
            {
                FrmToolValFinder fnd = new FrmToolValFinder();
                fnd.DataList = m_list.ToList();
                fnd.Filter = CanFilter;
                fnd.ShowDialog();
            }

            // compressor
            if (str_item == "compression")
            {
                FrmCompress fc = new FrmCompress();
                fc.ShowDialog();
            }

            // show connection settings
            if (str_item == "connection settings")
            {
                // show the dialog
                frmConnect = new FrmConnect(this);
                frmConnect.setDefaultSettings(connSettings);
                if (DialogResult.OK == frmConnect.ShowDialog())
                {
                    // get port & speed
                    connSettings = frmConnect.Settings;
                    // status bar
                    m_statusTool.setPort(connSettings.PortName);
                    m_statusTool.setSpeed(connSettings.CanSpeed);
                    m_statusTool.setSilentMode(connSettings.IsSilent);
                    m_statusTool.setAutosaveTrace(connSettings.IsTraceAutoSave);
                    force_ui_update = true;
                }
            }

            // either connect or disconnect
            if (str_item == "connect" || str_item == "disconnect")
            {
                if (isCommunication())
                    communicationStop();
                else
                    communcationStart();

                force_ui_update = true;
            }

            // do reset
            if (str_item == "reset")
            {
                m_can_reset_request = true;
                resetToolStripMenuItem.Enabled = false; // beforehand
            }

            // show the about screen
            if (str_item == "about")
            {
                FrmAbout f = new FrmAbout();
                f.ShowDialog();
            }

            // update gui
            if (force_ui_update)
                guiUpdateForce();
        }

        // menu callback 1
        private void menuStrip1_ItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string str_item = e.ClickedItem.Text.ToLower();
            menu_callback_common(str_item);
        }

        // menu callback 2
        private void ToolsToolStripMenuItem_DropDownItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string str_item = e.ClickedItem.Text.ToLower();
            menu_callback_common(str_item);
        }

        private void startToolStripMenuItem_Click(object sender, EventArgs e)
        {
            maskFilter.Enabled = true;
            m_statusTool.setMaskFilterState(maskFilter.isFilterExists());
        }

        private void pauseToolStripMenuItem_Click(object sender, EventArgs e)
        {
            maskFilter.Enabled = false;
            m_statusTool.setMaskFilterState(maskFilter.isFilterExists());
        }

        private void clearToolStripMenuItem_Click(object sender, EventArgs e)
        {
            maskFilter.reset();
            m_statusTool.setMaskFilterState(maskFilter.isFilterExists());
        }

        private void saveSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            List<canMessage2> mList = m_list.ToList();
            List<CanMessageStored> storage = new List<CanMessageStored>();

            foreach (canMessage2 m in mList)
            {
                int count = ucReadGrid.getCanMessageCounter(m);
                int interval = ucReadGrid.getCanMessageInterval(m);
                bool isChecked = !CanFilter.Contains(m.Id);
                storage.Add(new CanMessageStored(m, count, interval, isChecked));
            }

            CanSessionStorageTool.Save(storage);
        }

        private void loadSessionToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // filter
            CanFilter.clear();
            // data
            m_list.clear();
            // controls
            ucReadGrid.clear();
            ucReadTrace.clear();
            m_worker.Reset();
            m_statusTool.setReceivedMsgCount(0);


            communicationStop();


            List<CanMessageStored> storage = CanSessionStorageTool.Load();

            if (storage != null)
            {
                // storage
                foreach (var m in storage)
                    m_list.add(m.getMessage());

                // grid
                foreach (var m in storage)
                {
                    int tmo = m.period;
                    int cnt = m.count;

                    List<canMessage2> ls = new List<canMessage2>();
                    
                    for (int i = 0; i < cnt; i++) {                    
                        canMessage2 item = new canMessage2(m.getMessage().Id, m.getMessage().Data, tmo * i);
                        ls.Add(item);                       
                    }
                    ucReadGrid.pushMessageList(ls);
                }
                // filter
                foreach (var m in storage)
                    if (!m.isChecked)
                        CanFilter.add(m.getMessage().Id);

                ucReadGrid.updateCheckboxesWithFilter();
            }
        }

        /*
        // take a picture
        private void screenshotToolStripMenuItem_Click(object sender, EventArgs e)
        {
            // store
            ScreenCapture.captureAppScreen(this);
            // message
            uiMessageTool.ShowMessage(string.Format("Image stored at {0}",
                ScreenCapture.getDateTimeBasedDirPath()));
        }
        */
        #endregion

    }


    // session storage
    #region sessionStorage

    // a container class for a single message
    public class CanMessageStored
    {
        public int canId = 0;
        public bool is29bit = false;
        public int dlc = 0;
        public int[] data = null;

        public int count = 0;
        public int period = 0;

        public bool isChecked = false;

        public CanMessageStored (canMessage2 msg, int msgCount, int msgPeriod, bool isMsgChecked)
        {
            canId = msg.Id.Id;
            is29bit = msg.Id.Is29bit;
            dlc = msg.Id.Dlc;

            data = new int[dlc];

            for (int i = 0; i < dlc; i++)
                data[i] = (int)msg.Data[i];

            isChecked = isMsgChecked;
            count = msgCount;
            period = msgPeriod;
        }

        public canMessage2 getMessage ()
        {
            byte[] bytebuff = new byte[dlc];
            for (int i = 0; i < dlc; i++)
                bytebuff[i] = (byte)data[i];

            canMessage2 m = new canMessage2(canId, is29bit, bytebuff);
            return m;
        }

        public CanMessageStored() {}
    }

    public static class CanSessionStorageTool
    {
        public static void Save (List<CanMessageStored> ls)
        {
            string xml = Tools.XmlSerializer<List<CanMessageStored>>.Serialize(ls);

            SaveFileDialog dlg = new SaveFileDialog();
            dlg.DefaultExt = "candata";
            dlg.AddExtension = true;
            dlg.CheckPathExists = true;
            dlg.Filter = "CAN Analyzer Data Grid|*candata";
            dlg.OverwritePrompt = true;

            if (dlg.ShowDialog() == DialogResult.OK)
                File.WriteAllText(dlg.FileName, xml);
        }

        public static List<CanMessageStored> Load ()
        {
            OpenFileDialog oDlg = new OpenFileDialog();
            oDlg.DefaultExt = "candata";
            oDlg.AddExtension = true;
            oDlg.CheckPathExists = true;
            oDlg.Filter = "CAN Analyzer Data Grid|*candata";

            if (oDlg.ShowDialog() == DialogResult.OK)
            {
                string xml = File.ReadAllText(oDlg.FileName);
                return Tools.XmlSerializer<List<CanMessageStored>>.Deserialize(xml);
            }

            return null;
        }
    }

    #endregion

    // a CAN interface level for external modules
    #region canMessageSendTool
    public class CanMessageSendTool
    {
        readonly canWorker SendWorker;

        // is Com Port open?
        public bool IsCommunication { get { return SendWorker.isPortOpen(); } }

        // is sending allowed?
        public bool IsSendingAllowed()
        {
            return SendWorker.isPortOpen() && !SendWorker.isListenOnlyModeEnabled();
        }

        // constructor
        public CanMessageSendTool(canWorker parent)
        {
            SendWorker = parent;
        }

        // send a message
        public bool SendCanMessage(canMessage2 msg, int count = 1, bool rtr = false)
        {
            if (IsCommunication)
            {
                bool res = true;
                for (int i = 0; i < count && res; i++)
                    res = SendWorker.sendMessge(msg, rtr);
                return res;
            }
            return false;
        }

        // send a message list
        public bool SendCanMessage(List<canMessage2> ls)
        {
            if (ls == null || ls.Count == 0)
                return false;

            bool res = false;
            if (IsCommunication)
            {
                res = true;
                for (int i = 0; i < ls.Count && res == true; i++)
                {
                    res = SendWorker.sendMessge(ls[i]);
                }
            }
            return res;
        }

        // is Hi-Pefromance mode enabled?
        public bool IsHighPerformanceModeEnabled()
        {
            return SendWorker.highPefrormanceModeGet();
        }
    }
    #endregion

    // status bar utils
    #region uiStatusBar
    public class uiStatus
    {
        // status strip itself
        private StatusStrip     m_control; 
        // items
        private ToolStripItem   m_port;
        private ToolStripItem   m_speed;
        private ToolStripItem   m_connStatus;
        private ToolStripItem   m_fwVersion;
        private ToolStripItem   m_msgCount;
        private ToolStripItem   m_maskFilter;
        private ToolStripItem   m_canSilentMode;
        private ToolStripItem   m_autosaveTrace;
        private ToolStripItem   m_canErrors;

        // strings (to reduce UI updates)
        private string m_str_prev_port = string.Empty;
        private string m_str_prev_speed = string.Empty;
        private string m_str_prev_connStatus = string.Empty;
        private string m_str_prev_fwVersion = string.Empty;
        private string m_str_prev_msgCount = string.Empty;
        private string m_str_prev_maskFilter = string.Empty;
        private string m_str_prev_silent = string.Empty;
        private string m_str_prev_autosaveTrace = string.Empty;
        private string m_str_prev_errors = string.Empty;

        // constructor
        public uiStatus(ref StatusStrip control)
        {
            m_control = control;

            m_port = m_control.Items["statusPort"];
            m_speed = m_control.Items["statusSpeed"];
            m_connStatus = m_control.Items["statusConnState"];
            m_fwVersion = m_control.Items["statusFwVer"];
            m_maskFilter = m_control.Items["statusMaskFilterEn"];
            m_msgCount = m_control.Items["statusMsgCnt"];
            m_canSilentMode = m_control.Items["statusSilentMode"];
            m_autosaveTrace = m_control.Items["statusAutosaveTrace"];
            m_canErrors = m_control.Items["statusErrors"];

            m_control.Height = 25;

            m_port.Width = 100;
            
            m_control.BackColor = System.Drawing.SystemColors.ActiveCaption;

            clear();

            // style
            Font f = new Font("Calibri", 9.5f, FontStyle.Bold);
            for (int i = 0; i < m_control.Items.Count; i++)
            {
                m_control.Items[i].ForeColor = Color.White;
                m_control.Items[i].Font = f;
            }

            setReceivedMsgCount(0);
        }

        // should we show bus errors?
        public void showBusErrors(bool show)
        {
            m_canErrors.Visible = show;
        }

        // set a selected COM port name
        public void setPort (string port)
        {
            string str_val = string.IsNullOrEmpty(port) ? "Port is not selected" : port;
            // check
            if (str_val != m_str_prev_port)
            {
                // update
                m_str_prev_port = str_val;
                m_port.Text = str_val;
            }
        }

        // set a selected speed value
        public void setSpeed (string speed)
        {
            string str_val = string.IsNullOrEmpty(speed) ?
                "Speed is not selected" :
                string.Format("{0}", speed);

            // check
            if (str_val != m_str_prev_speed)
            {
                m_str_prev_speed = str_val;
                m_speed.Text = str_val;

                m_speed.ForeColor = speed.ToLower() == "auto" ? Color.Yellow : Color.White;
            }
        }

        // set a CAN silent mode state
        public void setSilentMode (bool enabled)
        {
            //string str_val = enabled ?
            //     "CAN Mode: Listen only" :
            //     "CAN Mode: Normal";
            string str_val = enabled ? "Listen only" : "Normal";

            // check
            if (str_val != m_str_prev_silent)
            {
                m_str_prev_silent = str_val;
                m_canSilentMode.Text = str_val;
                m_canSilentMode.ForeColor = enabled ? Color.Yellow : Color.White;
            }
        }

        // set an autosave trace state
        public void setAutosaveTrace(bool enabled)
        {
            string str_val = enabled ?
                "Autosave: On" :
                "Autosave: Off";

            // check
            if (str_val != m_str_prev_autosaveTrace)
            {
                m_str_prev_autosaveTrace = str_val;
                m_autosaveTrace.Text = str_val;
                m_autosaveTrace.ForeColor = enabled ? Color.ForestGreen : Color.White;
            }
        }

        // set a device fw version
        public void setFwVersion (string ver)
        {
            string str_val = string.IsNullOrEmpty(ver) ?
                "FW: none" :
                string.Format("FW: {0}", ver);

            // check
            if (str_val != m_str_prev_fwVersion)
            {
                m_str_prev_fwVersion = str_val;
                m_fwVersion.Text = str_val;
            }        
        }

        // set a connection status
        public void setConnectionStatus (bool connected)
        {
            string str_val = connected ? "Connected" : "Disconnected";
            // check
            if (str_val != m_str_prev_connStatus)
            {
                m_str_prev_connStatus = str_val;
                m_connStatus.Text = str_val;
            }
        }

        // set a message counter
        public void setReceivedMsgCount (ulong cnt)
        {
            string str_val = string.Format("Received: {0}", cnt);
            // check
            if (str_val != m_str_prev_msgCount)
            {
                m_str_prev_msgCount = str_val;
                m_msgCount.Text = str_val;
            }
        }

        // set a mask filter state
        public void setMaskFilterState (bool enabled)
        {
            string str_val = enabled ?
                "Mask Filter: Enabled" :
                "Mask Filter: Disabled";
            // check
            if (str_val != m_str_prev_maskFilter)
            {
                m_str_prev_maskFilter = str_val;
                m_maskFilter.Text = str_val;
            }
        }

        public void setErrors (canParserErrors err)
        {
            if (err == null)
                return;
            bool bus_off = err.isBusOff();

            string str_val = string.Format("{0}Err: Tx {1}, Rx {2}, Reset={3}",
                bus_off ? "Bus-Off! " : "",
                err.GetErrorCounterTX(), 
                err.GetErrorCounterRX(),
                err.GetCanResetStatus() == true ? 1 : 0);

            // check
            if (str_val != m_str_prev_maskFilter)
            {
                m_str_prev_errors = str_val;
                m_canErrors.Text = str_val;
                m_canErrors.ForeColor = bus_off ? Color.IndianRed : Color.White;
            }
        }

        // set all the params
        public void set (string port, string speed, string ver, bool state,
                         bool filter, bool silent, bool autosave, canParserErrors bus_err)
        {
            setPort(port);
            setSpeed(speed);
            setFwVersion(ver);
            setConnectionStatus(state);
            setMaskFilterState(filter);
            setSilentMode(silent);
            setAutosaveTrace(autosave);
            setErrors(bus_err);
        }

        // clear all the params
        public void clear ()
        {
            set("", "", "", false, false, false, false, null);
        }
    }
    #endregion

    // settings 
    #region applicationSettins
    class AppSettings
    {
        static readonly string section = "appSettings";

        // get config wrapper
        static private Configuration getConfig()
        {
            return ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
        }

        // save a string option
        static private void save(string name, string value)
        {
            Configuration currentConfig = getConfig();

            var settings = currentConfig.AppSettings.Settings;

            // create / update
            if (settings[name] == null)
                settings.Add(name, value);
            else
                settings[name].Value = value;

            currentConfig.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(section);
        }

        // set connection settings
        static public void SaveConnSettings (ConnSettingsCan connSettings)
        {
            save("portName", connSettings.PortName);
            save("canSpeed", connSettings.CanSpeed);
            save("canListenOnly", connSettings.IsSilent.ToString());
            save("autosaveTrace", connSettings.IsTraceAutoSave.ToString());
        }

        // set device config
        /*
        static public void SaveRemotoDevice (string strDevice)
        {
            Configuration currentConfig = getConfig();

            currentConfig.AppSettings.Settings["device"].Value = strDevice;

            currentConfig.Save(ConfigurationSaveMode.Modified);
            ConfigurationManager.RefreshSection(section);
        }
        */

        // set data path
        static public void SaveGenericDataPath(string strPath)
        {
            const string name = "dataPathGeneric";
            save(name, strPath);
        }

        // set brutforcer data path
        static public void SaveBrutforcerDataPath(string strPath)
        {
            const string name = "dataPathBrute";
            save(name, strPath);
        }
        

        // get connection settings
        static public ConnSettingsCan LoadConnSettings()
        {
            string str_speed = ConfigurationManager.AppSettings.Get("canSpeed");
            string str_port = ConfigurationManager.AppSettings.Get("portName");
            string str_silent = ConfigurationManager.AppSettings.Get("canListenOnly");
            string str_autosave_trace = ConfigurationManager.AppSettings.Get("autosaveTrace");
            bool tmp_bool = false;

            ConnSettingsCan stngs = new ConnSettingsCan();
            stngs.CanSpeed = str_speed;
            stngs.PortName = str_port;
            if (!string.IsNullOrEmpty(str_silent) && bool.TryParse(str_silent, out tmp_bool))
                stngs.IsSilent = tmp_bool;
            if (!string.IsNullOrEmpty(str_autosave_trace) && bool.TryParse(str_autosave_trace, out tmp_bool))
                stngs.IsTraceAutoSave = tmp_bool;

            return stngs;
        }

        // get script number
        static public int LoadScriptNumber()
        {
            const string path = "scriptNum";
            const string default_num = "7";
            string num = ConfigurationManager.AppSettings.Get(path);
            if (num == null)
            {
                Configuration currentConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                currentConfig.AppSettings.Settings.Add(path, default_num);
                currentConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            int res = Convert.ToInt32(num);
            return res == 0 ? Convert.ToInt32(default_num) : res;
        }

        // get lines limit for scripts
        static public int LoadScriptTraceLimitLines()
        {
            const string path = "scriptTraceLinesLimit";
            const string default_num = "10000"; // 10k is a default value
            string num = ConfigurationManager.AppSettings.Get(path);
            if (num == null)
            {
                Configuration currentConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                currentConfig.AppSettings.Settings.Add(path, default_num);
                currentConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            int res = Convert.ToInt32(num);
            return res == 0 ? Convert.ToInt32(default_num) : res;
        }

        // get lines limit for scripts
        static public int LoadScriptUdsRxTimeout1()
        {
            const string path = "scriptUdsRxTimeout1";
            const string default_num = "100"; // 50ms + 50ms just in case
            string num = ConfigurationManager.AppSettings.Get(path);
            if (num == null)
            {
                Configuration currentConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                currentConfig.AppSettings.Settings.Add(path, default_num);
                currentConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            int res = Convert.ToInt32(num);
            return res == 0 ? Convert.ToInt32(default_num) : res;
        }

        // get lines limit for scripts
        static public int LoadScriptUdsRxTimeout2()
        {
            const string path = "scriptUdsRxTimeout2";
            const string default_num = "250"; // 150ms + 100ms just in case
            string num = ConfigurationManager.AppSettings.Get(path);
            if (num == null)
            {
                Configuration currentConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                currentConfig.AppSettings.Settings.Add(path, default_num);
                currentConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
            }
            int res = Convert.ToInt32(num);
            return res == 0 ? Convert.ToInt32(default_num) : res;
        }

        // Should we monitor errors on the CAN bus
        static public bool LoadCanBusErrorMonitorConfig()
        {
            const string path = "canBusErrorMonitor";
            const string default_num = "1";
            string num = ConfigurationManager.AppSettings.Get(path);
            if (num == null)
            {
                Configuration currentConfig = ConfigurationManager.OpenExeConfiguration(ConfigurationUserLevel.None);
                currentConfig.AppSettings.Settings.Add(path, default_num);
                currentConfig.Save(ConfigurationSaveMode.Modified);
                ConfigurationManager.RefreshSection("appSettings");
                num = default_num;
            }

            return Convert.ToInt32(num) == 0 ? false : true;
        }

        // remoto
        static public string LoadRemotoDevice()
        {
            return ConfigurationManager.AppSettings.Get("device");
        }

        // data path
        static public string LoadGenericDataPath()
        {
            return ConfigurationManager.AppSettings.Get("dataPathGeneric");
        }

        // data path
        static public string LoadBrutforcerDataPath()
        {
            return ConfigurationManager.AppSettings.Get("dataPathBrute");
        }
    }
    #endregion

    // message box wrapper
    #region message_box
    public class uiMessageTool
    {
        static public void ShowMessage(string text, string caption)
        {
            MessageBox.Show(text, caption);
        }

        static public void ShowMessage(string text)
        {
            MessageBox.Show(text);
        }
    }
    #endregion
}



