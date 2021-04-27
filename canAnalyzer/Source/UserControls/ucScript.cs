using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;
using System.Threading;
using ScintillaNET;
using NCalc;    // I left it for trace only cuz it supports the ternary operator
using System.Text.RegularExpressions;


namespace canAnalyzer
{
    public partial class ucScript : UserControl
    {
        private CanMessageSendTool CanTool;
        private Thread worker;
        private nsScriptParser.scriptParserCmd[] cmdList;
        private bool workerStopRequest;
        private bool isTraceEnabled;

        private advansedTextEdior txtEditor;
        private List<canMessage2> readList;
        private ContextMenuStrip menuEditor;

        private bool m_is_restart_on = false;
    
        private bool breakpointIsSet = false;

        private int m_trace_lines_limit = 10000;
        private int m_uds_timeout1 = 0; // 0 - use default values
        private int m_uds_timeout2 = 0;

        private Mutex m_rx_lock = new Mutex();
        private int m_read_waiting_for_can_id = 0;
        private EventWaitHandle m_worker_wait_handle_stop =
            new EventWaitHandle(false, EventResetMode.AutoReset);
        private EventWaitHandle m_worker_read_expected_can_id =
            new EventWaitHandle(false, EventResetMode.AutoReset);
        private EventWaitHandle m_worker_breakpoint =
            new EventWaitHandle(false, EventResetMode.AutoReset);

        public ucScript(CanMessageSendTool canSendTool)
        {
            InitializeComponent();

            CanTool = canSendTool;
            this.Dock = DockStyle.Fill;

            readList = new List<canMessage2>();

            // gui
            guiConfig();
            // menu
            menuConfig();

            buttonContinueEnable(false);
        }

        public void setTraceLineLimit(int limit)
        {
            if (limit > 0)
                m_trace_lines_limit = limit;
        }

        public void setUdsTimeouts(int rx_tmo1, int rx_tmo2)
        {
            if (rx_tmo1 > 0)
                m_uds_timeout1 = rx_tmo1;
            if (rx_tmo2 > 0)
                m_uds_timeout2 = rx_tmo2;
        }

        private void menuConfig()
        {
            // script
            menuEditor = new ContextMenuStrip();
            menuEditor.Items.Add("Save Script");
            menuEditor.Items.Add("Load Script");
            menuEditor.Items.Add("Clear");
            menuEditor.ItemClicked += onContextMenuEditorItemClicked;
            txtEditor.contextMenu = menuEditor;
        }

        // trace context menu (doesnt work)
        private void onContextMenuTraceItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string selected = e.ClickedItem.Text.ToLower();

            if (selected == "clear")
                tbScript.Clear();
        }

        // script context menu
        private void onContextMenuEditorItemClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string selected = e.ClickedItem.Text.ToLower();

            if (selected == "clear")
            {
                txtEditor.Text = string.Empty;
            }

            if( selected == "save script")
            {
                menuEditor.Hide();

                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = "txt";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.Filter = "CAN Analyzer script|*txt";
                dlg.OverwritePrompt = true;
                //dlg.FileName = "123-456";
                //dlg.InitialDirectory =
                ///    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                //    "\\" + Application.ProductName;
                if (dlg.ShowDialog() == DialogResult.OK)
                    System.IO.File.WriteAllText(dlg.FileName, txtEditor.Text);
            }

            if( selected == "load script")
            {
                menuEditor.Hide();

                OpenFileDialog oDlg = new OpenFileDialog();
                oDlg.DefaultExt = "txt";
                oDlg.AddExtension = true;
                oDlg.CheckPathExists = true;
                oDlg.Filter = "CAN Analyzer script|*txt";

                //oDlg.InitialDirectory =
                //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
               //     "\\" + Application.ProductName;
                if (oDlg.ShowDialog() == DialogResult.OK)
                    txtEditor.Text = System.IO.File.ReadAllText(oDlg.FileName);
            }

            /*
            if (selected == "copy all" || selected == "copy selected" || selected == "save all" || selected == "save selected")
            {
                menu.Hide();

                string txt = getRowsAsAstring(selected.Contains("selected"));
                // copy
                if (selected.Contains("copy"))
                    Clipboard.SetText(txt);
                else if (selected.Contains("save"))
                {
                    SaveFileDialog dlg = new SaveFileDialog();
                    dlg.DefaultExt = "txt";
                    dlg.AddExtension = true;
                    dlg.CheckPathExists = true;
                    dlg.Filter = "CAN Analyzer trace list|*txt";
                    dlg.OverwritePrompt = true;
                    //dlg.FileName = "123-456";
                    dlg.InitialDirectory =
                        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                        "\\" + Application.ProductName;
                    if (dlg.ShowDialog() == DialogResult.OK)
                        System.IO.File.WriteAllText(dlg.FileName, txt);
                }
            }
            */

        }


        private void guiConfig()
        {
            tbScript.Multiline = true;
            tbScript.Dock = DockStyle.Fill;
            tbScript.Font = new Font("Consolas", 8.3f, FontStyle.Italic);
            tbScript.TextAlign = HorizontalAlignment.Left;
            tbScript.ScrollBars = ScrollBars.Vertical;

            tbScript.Visible = false;

            tbScript.Text = scriptTxtManual.getHelpString();

            cbEnableTrace.Text = "Enable Trace";
            cbEnableTrace.Checked = true;
            isTraceEnabled = true;

            // config trace window
            tbTrace.Multiline = true;
            tbTrace.Dock = DockStyle.Fill;
            tbTrace.ScrollBars = RichTextBoxScrollBars.Vertical;
            Font f = new Font("Consolas", 8.5f, FontStyle.Italic);
            tbTrace.Font = f;

            // config text window
            txtEditor = new advansedTextEdior(tableLayoutPanel1);
            txtEditor.Text = tbScript.Text;

            // buttons
            btnStart.Click += btnStart_Click;
            btnContinue.Click += btnContinue_Click;

            cbRestartScript.CheckedChanged += evtRestartCheckChanged;
        }

        // script auto-restart
        public bool isAutoRestartEnabled()
        {
            //if (InvokeRequired)
            // {
            //     bool myBool = false;
            //     this.Invoke(new MethodInvoker(() => myBool = isAutoRestartEnabled()));
            //     return myBool;
            // }
            return m_is_restart_on;
            //return cbRestartScript.Checked;
        }

        // is script worker running
        private bool isWorkerRunning()
        {
            return worker != null && worker.ThreadState != System.Threading.ThreadState.Stopped;
        }

        // add new messages
        public void addMessageList (List<canMessage2> ls)
        {
            if (!isWorkerRunning() || ls.Count == 0)
                return;

            m_rx_lock.WaitOne();
            // add
            readList.AddRange(ls.ToArray());
            m_rx_lock.ReleaseMutex();

            // check and indicate to force reading sleeping
            if (m_read_waiting_for_can_id > 0)
            {
                foreach (var m in ls)
                {
                    if (m.Id.Id == m_read_waiting_for_can_id)
                    {
                        m_worker_read_expected_can_id.Set();
                        break;
                    }
                }
            }
        }

        public bool isRunning()
        {
            return isWorkerRunning();
        }

        class traceItem
        {
            public readonly string Text;
            public readonly Color TextColor;

            public traceItem (string txt, Color cla)
            {
                Text = txt;
                TextColor = cla;
            }
        }

        private List<traceItem> traceStorage = new List<traceItem>();
        private bool is_trace_overflow = false;

        // add a message to the queue
        private void trace (string str, Color color)
        {
            if (!isTraceEnabled || is_trace_overflow)
                return;

            // 50 unhandled messages is too much, looks like a loop
            if (traceStorage.Count < 50)
                traceStorage.Add(new traceItem(str, color));
        }

        // flush
        private void traceFlush()
        {
            if (traceStorage.Count == 0)
                return;

            // enabled? allowed?
            if (isTraceEnabled == false || is_trace_overflow)
                return;

            if (InvokeRequired)
            {
                this.BeginInvoke(new Action(traceFlush));
                return;
            }

            // check
            int line_num = tbTrace.Lines.Length + traceStorage.Count;
            if (line_num > m_trace_lines_limit && !is_trace_overflow)
            {
                is_trace_overflow = true;
                traceStorage.Clear();
                traceStorage.Add(new traceItem("Trace overflow. Stop tracing", Color.Red));
            }

            const string emptyLine = "\"\"";

            int cnt = traceStorage.Count;

            for (int i = 0; i < cnt; i++)
            {
                string str = traceStorage[i].Text;
                Color color = traceStorage[i].TextColor;

                if (str != emptyLine)
                {
                    tbTrace.SelectionStart = tbTrace.TextLength;
                    tbTrace.SelectionLength = 0;

                    tbTrace.SelectionColor = color;
                    tbTrace.AppendText(str);
                    tbTrace.SelectionColor = tbTrace.ForeColor;
                }
                tbTrace.AppendText(Environment.NewLine);
            }

            tbTrace.ScrollToCaret();

            traceStorage.RemoveRange(0, cnt);
        }

        private void buttonContinueEnable (bool en)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(buttonContinueEnable), new object[] { en });
                return;
            }

            btnContinue.Enabled = en;
        }

        private void evtRestartCheckChanged(object sender, EventArgs e)
        {
            CheckBox cb = (CheckBox)sender;
            if (cb == cbRestartScript)
                m_is_restart_on = cbRestartScript.Checked;
        }

        private void btnContinue_Click(object sender, EventArgs e)
        {
            breakpointIsSet = false;
            m_worker_breakpoint.Set();
        }

        private void SetStartStopButtonMode (bool en)
        {
            if (InvokeRequired)
            {
                this.BeginInvoke(new Action<bool>(SetStartStopButtonMode), new object[] { en });
                return;
            }

            btnStart.Text = en ? "Execute" : "Stop";
        }

        private void btnStart_Click(object sender, EventArgs e)
        {
            // is running?
            if( isWorkerRunning() )
            {
                // stop
                stopWorker();
                SetStartStopButtonMode(true);
                return;
            }

            string s = txtEditor.getText();

            nsScriptParser.scriptParserCmd[] cmd = nsScriptParser.scriptParser.parseText(s);

            bool isNotEmpty = false;
            foreach(var c in cmd)
            {
                if (c.cmd != nsScriptParser.cmdType.comment)
                    isNotEmpty = true;
            }

            if ( !isNotEmpty )
            {
                trace("Error. Script is empty", Color.Black);
                traceFlush();
                return;
            }

            string errorList = string.Empty;

            foreach (var v in cmd)
                if (v.error)
                    errorList += v.strCmd;// + Environment.NewLine;

            if( !string.IsNullOrEmpty(errorList) )
            {
                // trace
                trace("Error. Text cannot be parsed", Color.Red);
                trace("Please check the lines:" + Environment.NewLine + errorList, Color.Black);
                traceFlush();
                return;
            }

            // start the thread
            cmdList = cmd;

            worker = new Thread(onWorker);
            worker.Name = "scriptWrkr";
            worker.Start();

            SetStartStopButtonMode(false);
        }

        private bool checkDataMask(canMessage2 msg, string[] mask)
        {
            int okCnt = 0;

            // check all the received data bytes
            for (int byte_idx = 0; byte_idx < msg.Id.Dlc; byte_idx++)
            {
                bool ok = false;
                string s = mask[byte_idx];

                // mask byte, like **, ***, 0x*0, 0x0*
                if (s.Contains('*'))
                {
                    if (s.Contains("***"))
                    {
                        // all the next byte are allowed
                        return true;
                    }
                    if (s.Contains("**"))
                    {
                        // we can use any byte
                        ok = true;
                    }
                    else
                    {
                        /*
                        // mask pattern
                        string str_rx_byte = msg.Data[i].ToString("X2");
                        if (s.IndexOf('*') == s.Length - 1)
                            s = s.Replace('*', str_rx_byte[1]);
                        else
                            s = s.Replace('*', str_rx_byte[0]);

                        int b;
                        if (Tools.tryParseInt(s, out b))
                        {
                            ok = (msg.Data[i] & b) == b;
                        }
                        */

                        // compare them as strings
                        // patt 0x*7, rx 0x07;  ok = '7' == '7'
                        string s_rx = "0x" + msg.Data[byte_idx].ToString("X2");
                        if (s_rx.Length == s.Length)
                        {
                            bool match = true;
                            for (int pos = 0; pos < s_rx.Length; pos++)
                            {
                                if (s_rx[pos] != s[pos] && s[pos] != '*')
                                {
                                    match = false;
                                    break;
                                }
                            }

                            ok = match;
                        }
                    }
                }
                else
                {
                    // not a mask, just do compare
                    int b = 0;
                    if (Tools.tryParseInt(mask[byte_idx], out b))
                    {
                        ok = msg.Data[byte_idx] == b;
                    }                      
                }

                // the present data byte check failed, return false
                if (!ok)
                    return false;
                okCnt++;
            }

            return okCnt == msg.Id.Dlc;
        }

        // parse a byte string, for example, for 'send'
        private byte[] scriptParseByteString(string[] pData, canMessage2[] varsCan, List<byte>[] varsBuff)
        {
            byte[] data = new byte[pData.Length];

            // fill
            for (int i = 0; i < data.Length; i++)
            {
                string p = pData[i];

                // is int?
                int val = -1;
                bool isInt = Tools.tryParseInt(p, out val);
                if (isInt && val >= 0 && val <= 0xFF)
                {
                    data[i] = (byte)val;
                }
                else
                {
                    // replace can variables
                    //while (p.Contains("var"))
                    {
                        const string pattern = @"var(\d)\[\s*(\d)\s*\]";
                        var varBuff = Regex.Matches(p, pattern);
                        foreach (Match s in varBuff)
                        {
                            string sVar = s.Groups[0].ToString();
                            int varIdx = Convert.ToInt32(s.Groups[1].ToString());
                            int valIdx = Convert.ToInt32(s.Groups[2].ToString());

                            if (varIdx < 0 || varIdx >= varsCan.Count())
                                throw new System.ArgumentException("Invalid parameter value", "varIdx");

                            canMessage2 carVar_ = varsCan[varIdx];
                            int value = 0;
                            if (null != carVar_ && carVar_.Id.Dlc >= valIdx)
                                value = carVar_.Data[valIdx];
                            p = p.Replace(sVar, value.ToString());
                        }
                    }
                    // replace data buffers
                    {
                        const string pattern = @"arr(\d)\[\s*(\d)\s*\]";
                        var varBuff = Regex.Matches(p, pattern);
                        foreach (Match s in varBuff)
                        {
                            string sVar = s.Groups[0].ToString();
                            int varIdx = Convert.ToInt32(s.Groups[1].ToString());
                            int valIdx = Convert.ToInt32(s.Groups[2].ToString());

                            if (varIdx < 0 || varIdx >= varsBuff.Count())
                                throw new System.ArgumentException("Invalid parameter value", "varIdx");

                            var arrData = varsBuff[varIdx];
                            int value = 0;
                            if (null != arrData && arrData.Count() > valIdx)
                                value = arrData[valIdx];
                            p = p.Replace(sVar, value.ToString());
                        }
                    }

                    object eval = MyCalc.evaluate(p);
                    data[i] = (eval != null) ? (MyCalc.objToByte(eval)) : (byte)0;
                }
            }

            return data;
        }

        // script worker
        private void onWorker()
        {
            workerStopRequest = false;
            int cmdIdx = 0;

            m_rx_lock.WaitOne();
            readList.Clear();
            m_rx_lock.ReleaseMutex();

            // wait for hi-perf mode
            if (!workerStopRequest)
            {
                var tmo_until = DateTimeOffset.Now.ToUnixTimeMilliseconds() + 3000;
                while (DateTimeOffset.Now.ToUnixTimeMilliseconds() < tmo_until &&
                       !CanTool.IsHighPerformanceModeEnabled())
                {
                    Thread.Sleep(100);
                }

                // sleep (hi-perf mode workaround)
                //Thread.Sleep(500);
            }

            trace(string.Format(
                "Start {0}",
                DateTime.Now.ToString("dd-MM-yyyy HH:mm:ss")), Color.Black);

            buttonContinueEnable(false);

            // a simple workaround to avoid loops
            int delay_msec_total = 0;
            const int delay_msec_total_min = 5;
            bool has_any_read_command = false;

            foreach (var cmd in cmdList)
            {
                if (cmd.cmd == nsScriptParser.cmdType.sleep)
                    delay_msec_total += cmd.sleepTmo;
                else if (cmd.cmd == nsScriptParser.cmdType.read)
                    has_any_read_command = true;
                else if (cmd.cmd == nsScriptParser.cmdType.breakpoint)
                    has_any_read_command = true;
            }

start_goto_pos:

            // clear the list
            m_rx_lock.WaitOne();
            readList.Clear();
            m_rx_lock.ReleaseMutex();
 
            // variables
            canMessage2[] canVarList = new canMessage2 [10];
            List<byte>[] canBuffList = new List<byte>[10];

            while (!workerStopRequest && cmdIdx < cmdList.Length)
            {
                // get a cmd
                nsScriptParser.scriptParserCmd cmd = cmdList[cmdIdx++];

                // execute
// empty
                if( cmd.cmd == nsScriptParser.cmdType.comment )
                {
                    // do nothing
                    continue;
                }
// sleep
                if (cmd.cmd == nsScriptParser.cmdType.sleep)
                {
                    int tmo = cmd.sleepTmo;
                    
                    if (tmo > 0)
                    {
                        trace(string.Format("Sleep for {0} msec", tmo), Color.Black);
                        traceFlush();

                        // wait (timeout or stop request)
                        if (!workerStopRequest)
                            m_worker_wait_handle_stop.WaitOne(tmo);

                        if (workerStopRequest)
                        {
                            trace("Aborted by user", Color.Red);
                            break;
                        }
                    }
                }
// send
                else if (cmd.cmd == nsScriptParser.cmdType.send)
                {
                    byte[] data = scriptParseByteString(cmd.paramsSend.sData, canVarList, canBuffList);

                    canMessage2 msg = new canMessage2(
                        cmd.paramsSend.Id, 
                        cmd.paramsSend.Is29BitId,
                        data, 
                        0);

                    // send the message
                    bool sent = CanTool.SendCanMessage(msg);

                    // trace
                    trace(string.Format("Send: Id=0x{0}, Dlc={1}: {2}",
                        msg.Id.GetIdAsString(), 
                        msg.Id.GetDlcAsString(),
                        msg.GetDataString(",", "0x")), Color.Black);

                    if (!sent)
                    {
                        trace("Error. The message cannot be sent. Aborted", Color.Red);
                        break;
                    }
                }
// read
                else if (cmd.cmd == nsScriptParser.cmdType.read)
                {
                    bool got = false;
                    int tmo = cmd.paramsRead.Timeout;
                    int sameIdMsgCount = 0;

                    int last_checked_idx = 0;

                    // start reading
                    m_read_waiting_for_can_id = cmd.paramsRead.Id.Id;

                    while (!got && !workerStopRequest && tmo > 0)
                    {
                        int gotMessageIdx = -1;

                        // for all the receiver messages
                        for (int i = last_checked_idx; i < readList.Count && !got; i++)
                        {
                            var msg = readList[i];
                            // 1. check the ID
                            if (cmd.paramsRead.Check(msg.Id))
                            {
                                // 2. check the data mask
                                if (checkDataMask(msg, cmd.paramsRead.DataMask))
                                {
                                    // found the required message
                                    trace(string.Format("Got: Id=0x{0}, Dlc={1}, {2}",
                                        msg.Id.GetIdAsString(), 
                                        msg.Id.GetDlcAsString(), 
                                        msg.GetDataString(",", "0x")), Color.Black);
                                    
                                    if (cmd.paramsRead.UseAsVar && cmd.paramsRead.VarIdx >= 0)
                                        canVarList[cmd.paramsRead.VarIdx] = msg;

                                    got = true;
                                    gotMessageIdx = i;
                                }

                                sameIdMsgCount++;
                            }

                            last_checked_idx++;
                        }

                        if (got)
                        {
                            //readList.Clear();
                            m_rx_lock.WaitOne();
                            readList.RemoveRange(0, gotMessageIdx + 1);
                            m_rx_lock.ReleaseMutex();    
                        }

                        traceFlush();

                        // really need to sleep?
                        if (!got && !workerStopRequest)
                        {
                            if (m_read_waiting_for_can_id > 0)
                            {
                                long sleep_start_ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                m_worker_read_expected_can_id.WaitOne(tmo);
                                long sleep_took_ms = DateTimeOffset.Now.ToUnixTimeMilliseconds() -
                                    sleep_start_ms;

                                tmo -= (int)sleep_took_ms;
                                if (tmo < 0)
                                    tmo = 0;
                            }
                        }
                    }

                    // stop reading
                    m_read_waiting_for_can_id = -1;

                    if (workerStopRequest) {
                        trace("Aborted by user", Color.Red);
                        break;
                    }

                    if( !got )
                    {
                        string str = string.Format("Not Received: Id=0x{0}, Mask={1}",
                            cmd.paramsRead.Id.GetIdAsString(), cmd.paramsRead.DataMaskString);
                        trace(str, Color.Red);
                        string str2 = string.Format("Received messages with Id=0x{0}: {1}",
                            cmd.paramsRead.Id.GetIdAsString(),
                            sameIdMsgCount);
                        trace(str2, Color.Black);

                        if (cmd.paramsRead.UseAsVar)
                            canVarList[cmd.paramsRead.VarIdx] = null;
                    }

                }

// UDS request
                else if (cmd.cmd == nsScriptParser.cmdType.udsReqSend)
                {
                    canUdsParser uds = new canUdsParser();
                    var curCmd = cmd.paramsUdsReq;
                    bool sent = false;
                    bool got = false;

                    // prepare the request
                    byte[] data = scriptParseByteString(curCmd.sData, canVarList, canBuffList);

                    // set timeouts
                    uds.SetRxTimeouts(m_uds_timeout1, m_uds_timeout2);

                    if (!curCmd.IsBmw)
                    {
                        canMessage2 msg_req = new canMessage2(
                            curCmd.IdReq,
                            curCmd.Is29BitId,
                            data);

                        canMessageId id_resp = new canMessageId(curCmd.IdResp, 1, curCmd.Is29BitId);

                        // send the request
                        sent = uds.sendRequest(CanTool, msg_req, id_resp) == canUdsParser.ErrorCode.Ok;

                        // trace
                        trace(string.Format("Send UDS: Req={0},{1}; Rsp={2}",
                            "0x" + msg_req.Id.GetIdAsString(),
                            msg_req.GetDataString(",", "0x"),
                            "0x" + id_resp.GetIdAsString()), Color.Black);
                    }
                    else {
                        canMessage2 msg_req = new canMessage2(
                            curCmd.BmwEcuId,
                            curCmd.Is29BitId,
                            data);

                        sent = uds.sendRequestBMW(CanTool, (byte)(msg_req.Id.Id), msg_req.Data.ToList()) == canUdsParser.ErrorCode.Ok;
                        
                        // trace
                        trace(string.Format("Send BMW: Req={0} {1}",
                            "0x" + msg_req.Id.Id.ToString("X2"),
                            msg_req.GetDataString(",", "0x")), Color.Black);
                    }

                    // something went wrong, stop
                    if (!sent)
                    {
                        trace("Error. The message cannot be sent. Aborted", Color.Red);
                        break;
                    }

                    // start reading, add the filter
                    m_read_waiting_for_can_id = uds.getExpectedEcuId();

                    // do
                    while (sent && !uds.IsSuccesfullyFinished() && !uds.IsTimeoutExpired() && !workerStopRequest)
                    {
                        // for all the receiver messages
                        m_rx_lock.WaitOne();
                        List<canMessage2> rxed = new List<canMessage2>();
                        rxed.AddRange(readList);
                        readList.RemoveRange(0, rxed.Count);
                        m_rx_lock.ReleaseMutex();

                        // handle
                        uds.handleMessages(rxed);
                        // done?
                        got = uds.IsSuccesfullyFinished();

                        traceFlush();

                        // sleep
                        if (!got && !workerStopRequest)
                        {
                            if (m_read_waiting_for_can_id > 0)
                            {
                                long tmo_ms = uds.GetTimeoutUnix() -
                                    DateTimeOffset.Now.ToUnixTimeMilliseconds();
                                if (tmo_ms > 0)
                                    m_worker_read_expected_can_id.WaitOne((int)tmo_ms);
                            }
                        }
                    }
                    
                    // stopped
                    if (workerStopRequest)
                    {
                        trace("Aborted by user", Color.Red);
                        break;
                    }

                    // append the received data to the buffer (or clean if failed)
                    if (curCmd.UseAsVar && curCmd.VarIdx >= 0)
                        canBuffList[curCmd.VarIdx] = got ? uds.getResponse() : null;

                    // finish
                    if (got)
                    {
                        // the full response has been received
                        trace(
                            string.Format("Got 0x{0}, rx={1}: {2}",
                                uds.getExpectedEcuId().ToString("X3"),
                                uds.getResponse().Count,
                                canAnalyzer.Tools.convertByteListToString(uds.getResponse(), "0x", ",")
                            ),
                            Color.Black);
                    }
                    else
                    {
                        // we've got some data, but not all of them
                        var resp = uds.getResponse();
                        if (resp != null && resp.Count > 0)
                        {
                            trace(
                                string.Format("Partially: 0x{0}, rx={1}(exp={2}): {3}",
                                uds.getExpectedEcuId().ToString("X3"),
                                resp.Count,
                                uds.GetNumOfExpectedBytes(),
                                canAnalyzer.Tools.convertByteListToString(resp, "0x", ",")
                            ),
                            Color.Orange);
                        }
                        else
                        {
                            trace("Req failed", Color.Red);
                        }
                    }
                }
// trace
                else if ( cmd.cmd == nsScriptParser.cmdType.trace )
                {
                    string message = prepareMessage4Trace(cmd, canVarList, canBuffList, 10);
                    trace(message, Color.Blue);
                }
// breakpoint
                else if( cmd.cmd == nsScriptParser.cmdType.breakpoint)
                {
                    breakpointIsSet = true;
                    trace("Breakpoint", Color.DarkGreen);

                    buttonContinueEnable(true);

                    traceFlush();

                    // wait
                    if (!workerStopRequest)
                        m_worker_breakpoint.WaitOne();

                    //while (!workerStopRequest && breakpointIsSet)
                   //     Thread.Sleep(50);

                    buttonContinueEnable(false);
                    if (!breakpointIsSet )
                        trace("Continue", Color.Black);
                }
            }

           
            traceFlush();

            // restart
            if (!workerStopRequest && isAutoRestartEnabled())
            {
                if (!has_any_read_command && delay_msec_total < delay_msec_total_min)
                {
                    // avoid pointless loops, finish the task
                    trace(string.Format("Restarting this script may crash the application. " +
                        "Min delay = {0}. Aborted", delay_msec_total_min), Color.Red);
                } 
                else
                {
                    // restart
                    cmdIdx = 0;
                    goto start_goto_pos;
                }
            }

            // finish
            SetStartStopButtonMode(true);
            trace("Stop" + Environment.NewLine, Color.Black);
            readList.Clear();
            traceFlush();
        }

        private void stopWorker()
        {
            // stop the task
            workerStopRequest = true;
            // stop sleeping
            m_worker_wait_handle_stop.Set();
            m_worker_read_expected_can_id.Set();
            m_worker_breakpoint.Set();
        }


        public void Stop()
        {
            // stop the task
            stopWorker();
        }

        private string prepareMessage4Trace(nsScriptParser.scriptParserCmd cmd, 
            canMessage2[] varList, List<byte>[] buffList, int maxItems)
        {
            // copy input arguments (cmd is a ref value, we should not overwrite it)
            string[] p_ArgList = new string[cmd.paramsTrace.ArgList.Length];
            for (int i = 0; i < p_ArgList.Length; i++)
                p_ArgList[i] = cmd.paramsTrace.ArgList[i];

            string res = cmd.paramsTrace.Format;
            // var p = cmd.paramsTrace;
            //string res = p.Format;
            const string str_null = "Null";

            // prepare arguments
            for (int i = 0; i < p_ArgList.Length; i++) {
                string sval = p_ArgList[i];

                // replace vars
                for (int k = 0; k < maxItems; k++)
                {
                    canMessage2 var = varList[k];         
                    for (int j = 0; j < 8; j++)
                    {
                        string s2replace = "var" + k.ToString() + "[" + j.ToString() + "]";
                        if (sval.Contains(s2replace))
                            sval = var != null ?
                                sval.Replace(s2replace, var.Data[j].ToString()) :
                                sval.Replace(s2replace, "Null");
                    }
                }
                // replace buffers
                for (int k = 0; k < maxItems; k++)
                {
                    List<byte> var = buffList[k];
                    var reg = Regex.Matches(sval, @"arr" + k.ToString() + @"\[(\d+)\]");

                    foreach (Match v in reg)
                    {
                        var sBuff = v.Groups[0].ToString();
                        var buffIdx = Convert.ToInt32(v.Groups[1].ToString());

                        sval = var != null && buffIdx >= 0 && var.Count() > buffIdx ?
                            sval.Replace(sBuff, var[buffIdx].ToString()) :
                            sval.Replace(sBuff, "Null");
                    }
                }
                // hex 2 dec
                while (sval.Contains("0x"))
                {
                    var reg = Regex.Match(sval, "0x[0-9A-Fa-f]*");
                    if (reg.Success)
                    {
                        int val = 0;
                        Tools.tryParseInt(reg.Value, out val);
                        sval = sval.Replace(reg.Value, val.ToString());
                    }
                }

                // update
                p_ArgList[i] = sval;
            }

            // resplace 'res' with the params
            for (int i = 0; i < p_ArgList.Length; i++)
            {
                string sval = p_ArgList[i];
                int val = 0;
                bool parsed = Tools.tryParseInt(sval, out val);
                string pattern_hex = "{x" + i.ToString() + "}";
                string pattern_int = "{" + i.ToString() + "}";
                string pattern_string_main = "{s" + i.ToString() + "}";

                Expression e = new Expression(sval);
                object eval = null;
                try
                {
                    eval = e.Evaluate();
                }
                catch
                {
                }

                // print as hex
                if (res.Contains(pattern_hex))
                {
                    string tmp = str_null;
                    if (eval != null)
                    {
                        tmp = eval.ToString();
                        tmp = Convert.ToInt32(tmp).ToString("X2");
                    }
                    res = res.Replace(pattern_hex, tmp);
                }
                // print as int
                if (res.Contains(pattern_int))
                {
                    string tmp = eval == null ? str_null : eval.ToString();
                    res = res.Replace(pattern_int, tmp);
                }
                // print as ASCII char
                if (res.Contains(pattern_string_main))
                {
                    string tmp = str_null;
                    if (eval != null)
                    {
                        tmp = eval.ToString();
                        char ch = Convert.ToChar(Convert.ToInt32(tmp));
                        if (ch == 0)
                            ch = ' ';
                        tmp = Char.ToString(ch);
                    }
                    res = res.Replace(pattern_string_main, tmp);
                }
            }

            return res;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnCleanTrace_Click(object sender, EventArgs e)
        {
            traceStorage.Clear();
            tbTrace.Clear();
            tbTrace.ScrollToCaret();
            is_trace_overflow = false;
        }

        private void cbEnableTrace_CheckedChanged(object sender, EventArgs e)
        {
            /* update */
            isTraceEnabled = cbEnableTrace.Checked;
            /* clean the storage */
            traceStorage.Clear();
        }
    }




    class advansedTextEdior
    {
        private ScintillaNET.Scintilla TextArea;

        public string Text { get { return TextArea.Text; } set { TextArea.Text = value; } }

        public ContextMenuStrip contextMenu { get { return TextArea.ContextMenuStrip; } set { TextArea.ContextMenuStrip = value; } }

        public string getText ()
        {
            return TextArea.Text;           
        }

        public advansedTextEdior(TableLayoutPanel pnl)
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
            TextArea.SetSelectionBackColor(true, IntToColor(0x114D9C));
            TextArea.CaretForeColor = Color.LightGray;
        }

        /// <summary>
        /// the background color of the text area
        /// </summary>
        private const int BACK_COLOR = 0x2A211C;

        /// <summary>
        /// default text color of the text area
        /// </summary>
        private const int FORE_COLOR = 0xB7B7B7;

        /// <summary>
        /// change this to whatever margin you want the line numbers to show in
        /// </summary>
        private const int NUMBER_MARGIN = 1;

        private void InitNumberMargin()
        {

            TextArea.Styles[Style.LineNumber].BackColor = IntToColor(BACK_COLOR);
            TextArea.Styles[Style.LineNumber].ForeColor = IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].ForeColor = IntToColor(FORE_COLOR);
            TextArea.Styles[Style.IndentGuide].BackColor = IntToColor(BACK_COLOR);

            var nums = TextArea.Margins[NUMBER_MARGIN];
            nums.Width = 30;
            nums.Type = MarginType.Number;
            nums.Sensitive = true;
            nums.Mask = 0;
        }

        private void InitSyntaxColoring()
        {

            // Configure the default style
            TextArea.StyleResetDefault();
            TextArea.Styles[Style.Default].Font = "Consolas";
            TextArea.Styles[Style.Default].Size = 9;
            TextArea.Styles[Style.Default].BackColor = IntToColor( 0x212121);
            TextArea.Styles[Style.Default].ForeColor = IntToColor(0xFFFFFF);
          
            TextArea.StyleClearAll();

            // Configure the CPP (C#) lexer styles
            TextArea.Styles[Style.Cpp.Identifier].ForeColor = IntToColor(0xD0DAE2);
            TextArea.Styles[Style.Cpp.Comment].ForeColor = IntToColor(0xBD758B);
            TextArea.Styles[Style.Cpp.CommentLine].ForeColor = IntToColor(0x40BF57);
            TextArea.Styles[Style.Cpp.CommentDoc].ForeColor = IntToColor(0x2FAE35);
            TextArea.Styles[Style.Cpp.Number].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.String].ForeColor = IntToColor(0xFFFF00);
            TextArea.Styles[Style.Cpp.Character].ForeColor = IntToColor(0xE95454);
            TextArea.Styles[Style.Cpp.Preprocessor].ForeColor = IntToColor(0x8AAFEE);
            TextArea.Styles[Style.Cpp.Operator].ForeColor = IntToColor(0xE0E0E0);
            TextArea.Styles[Style.Cpp.Regex].ForeColor = IntToColor(0xff00ff);
            TextArea.Styles[Style.Cpp.CommentLineDoc].ForeColor = IntToColor(0x77A7DB);
            TextArea.Styles[Style.Cpp.Word].ForeColor = IntToColor(0x48A8EE);
            TextArea.Styles[Style.Cpp.Word2].ForeColor = IntToColor(0xF98906);
            TextArea.Styles[Style.Cpp.CommentDocKeyword].ForeColor = IntToColor(0xB3D991);
            TextArea.Styles[Style.Cpp.CommentDocKeywordError].ForeColor = IntToColor(0xFF0000);
            TextArea.Styles[Style.Cpp.GlobalClass].ForeColor = IntToColor(0x48A8EE);

            TextArea.Lexer = Lexer.Cpp;

            
            TextArea.SetKeywords(0, "send send11 send29 sleep read read11 read29 " +
                "printf breakpoint uds_req uds_req11 uds_req29 bmw_req");
            TextArea.SetKeywords(1, "int uint8_t U8_t " +
                "var0 var1 var2 var3 var4 var5 var6 var7 var8 var9 " + 
                "arr0 arr1 arr2 arr3 arr4 arr5 arr6 arr7 arr8 arr9 " +
                "inf");
            //TextArea.SetKeywords(0, "class extends implements import interface new case do while else if for in switch throw get set function var try catch finally while with default break continue delete return each const namespace package include use is as instanceof typeof author copy default deprecated eventType example exampleText exception haxe inheritDoc internal link mtasc mxmlc param private return see serial serialData serialField since throws usage version langversion playerversion productversion dynamic private public partial static intrinsic internal native override protected AS3 final super this arguments null Infinity NaN undefined true false abstract as base bool break by byte case catch char checked class const continue decimal default delegate do double descending explicit event extern else enum false finally fixed float for foreach from goto group if implicit in int interface internal into is lock long new null namespace object operator out override orderby params private protected public readonly ref return switch struct sbyte sealed short sizeof stackalloc static string select this throw true try typeof uint ulong unchecked unsafe ushort using var virtual volatile void while where yield");
            //TextArea.SetKeywords(1, "void Null ArgumentError arguments Array Boolean Class Date DefinitionError Error EvalError Function int Math Namespace Number Object RangeError ReferenceError RegExp SecurityError String SyntaxError TypeError uint XML XMLList Boolean Byte Char DateTime Decimal Double Int16 Int32 Int64 IntPtr SByte Single UInt16 UInt32 UInt64 UIntPtr Void Path File System Windows Forms ScintillaNET");

        }

        public static Color IntToColor(int rgb)
        {
            return Color.FromArgb(255, (byte)(rgb >> 16), (byte)(rgb >> 8), (byte)rgb);
        }
    }

    static class scriptTxtManual
    {
        public static string getHelpString ()
        {
            return @"
/*  
    The number of script windows can be changed
      using 'scriptNum' value in 'canAnalyzer.exe.Config' file.

    List of commands:
    1. uds_req/uds_req11/uds_req29 - send the request
    2. bmw_req                     - send the BMW request
    3. read/read11/read29          - read the message
    4. send/send11/send29          - send the message
    5. sleep                       - sleep
    6. printf                      - print the string
    7. breakpoint                  - breakpoint
*/

// 0 - disabled, enabled otherwise.
#if 0


// all the declared variables work as defines
int obd_read_tmo_ms = 500;
int ecm_req_id      = 0x7E0;
int ecm_resp_id     = 0x7E8;
int bmw_ecu_cluster = 0x60;

// We have 2 sets of commands: requests and raw data sending/reading
//  1. using uds_req/uds_req11/uds_req29 or bmw_req.
//       Received data can be stored into 'arr'
//  2. using send/send11/send29 and read/read11/read29 commands.
//       Received data can be stored into 'var'

// there are 10 buffers for requested data, arr0 ... arr9. The lenght of an array is not limited.
// access example: arr0[0] (array 0, byte 0), arr1[3] (array 1, byte 3), etc.

// there are 10 buffers for received raw CAN data, var0 ... var9
// access example: var0[0] (buffer 0, byte 0), var1[3] (buffer 1, byte 3), etc.


// cmd:      UDS request (both 11-bit and 29-bit)
// function: uds_req or uds_req11 (11-bit), uds_req29 (29-bit)
// param1:   request id
// param2:   response id
// param3..: data
// example: uds_req(0x7E0, 0x7E8, 0x22, 0xF1, 0x90);

// You do not need to set timeouts here, they can be changed in canAnalyzer.exe.Config
// scriptUdsRxTimeout1 - the 1st frame
// scriptUdsRxTimeout2 - next frames

printf(""Request OBD (11-bit)"");
// OBD RPM (ECM)
arr0 = uds_req(ecm_req_id, ecm_resp_id, 0x01, 0x0C);
printf(""req OBD RPM = {0}"", ((arr0[2]<<8) + arr0[3]) / 4);
// OBD VIN (ECM)
arr0 = uds_req(ecm_req_id, ecm_resp_id, 0x09, 0x02);
printf(""req OBD VIN = {s0-s16}"",                            \
    arr0[4],arr0[5],arr0[6],arr0[7],arr0[8],arr0[9],arr0[10], \
    arr0[11],arr0[12],arr0[13],arr0[14],arr0[15],arr0[16],    \
    arr0[17],arr0[18],arr0[19],arr0[20]);


// cmd:      BMW requests (11-bit only)
// function: bmw_req
// param1:   ecu id (byte)
// param2..: data
// example: bmw_req(0x60, 0x22, 0xD1, 0x06);

// You do not need to set timeouts here, they can be changed in canAnalyzer.exe.Config
// scriptUdsRxTimeout1 - the 1st frame
// scriptUdsRxTimeout2 - next frames

printf(""Request BMW (11-bit)"");

// RPM 
arr0 = bmw_req(bmw_ecu_cluster, 0x22, 0xD1, 0x06);
printf(""BMW RPM = {0}"", arr0[3]*256 + arr0[4]);
// VIN
arr1 = bmw_req(0x10, 0x22, 0xF1, 0x90);

printf(""Read OBD sensors raw (11-bit)"");

// RPM
send(ecm_req_id, 0x02,0x01,0x0C,0,0,0,0,0);
var0 = read(ecm_resp_id, obd_read_tmo_ms, 0x04, 0x41, 0x0C, ***);
printf(""OBD RPM = {0} (raw = 0x{x1}{x2})"", \
    var0[1] != 0x41 ? -1 : ((var0[3] << 8) | var0[4]) / 4, var0[3], var0[4] );
// Speed
send(ecm_req_id, 0x02,0x01, 0x0D, 0,0,0,0,0);
var1 = read(ecm_resp_id, obd_read_tmo_ms, **,**, 0x0D, ***);
printf(""OBD Speed = {0} km/h (raw = 0x{x1})"", \
    var1[1] != 0x41 ? -1 : var1[3], var1[3]);
// Fuel
send(ecm_req_id, 0x02,0x01, 0x2F, 0,0,0,0,0);
var2 = read(ecm_resp_id, obd_read_tmo_ms, ***);
printf(""OBD Fuel = {0} % (raw = 0x{x1})"", \
    var2[1] != 0x41 ? -1 : var2[3] * 100 / 255, var2[3]);

// VIN
send(ecm_req_id, 0x02, 0x09, 0x02, 0x00, 0x00, 0x00, 0x00, 0x00);
var3 = read(ecm_resp_id, 150, 0x10, **, 0x49, 0x02, ***);
send(ecm_req_id, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
var4 = read(ecm_resp_id, 100, 0x21, ***);
var5 = read(ecm_resp_id, 100, 0x22, ***);
// print as ASCII string
printf(""OBD VIN = {s0-s16}"",                                 \
    var3[5],var3[6],var3[7],                                 \
    var4[1],var4[2],var4[3],var4[4],var4[5],var4[6],var4[7], \
    var5[1],var5[2],var5[3],var5[4],var5[5],var5[6],var5[7]);

// ECU Name
send(ecm_req_id, 0x02, 0x09, 0x0A, 0x00, 0x00, 0x00, 0x00, 0x00);
var3 = read(ecm_resp_id, 150, 0x10, **, 0x49, 0x0A, ***);
send(ecm_req_id, 0x30, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00);
var4 = read(ecm_resp_id, 100, 0x21, ***);
var5 = read(ecm_resp_id, 100, 0x22, ***);
var6 = read(ecm_resp_id, 100, 0x23, ***);
// print as ASCII string
printf(""OBD ECU Name = {s0-s19}"",                            \
    var3[5],var3[6],var3[7],                                 \
    var4[1],var4[2],var4[3],var4[4],var4[5],var4[6],var4[7], \
    var5[1],var5[2],var5[3],var5[4],var5[5],var5[6],var5[7], \
    var6[1],var6[2],var6[3]);
printf("""");

// cmd:      print
// function: printf
// param1:   format string
// param2..: params
// format: {0} - int, {x0} - hex, {s0} - ascii, {s0-s3} - ascii range
int test_val = 0x77;
printf("""");            // print an empty string
printf(""Hello World""); // print the 'Hello world' message
printf(""Int = {0}, Hex = 0x{x0}, ASCII = '{s0}', str = {s1}..{s1-s2}..{s1-s3}..{s1-s4}"", \
    0x43, 0x54, 0x65, 0x24, 0x74);
printf(""var0[0]={0}, var9[0]={1}, test_value={x2}, test_value2={x3}, 3={4}"", \
    var0[0], var9[0], test_val, test_val & 0xF0 + 1, 10*3 - 27);


printf("""");
printf(""Sleep function example: wait for 123 ms"");

// cmd:      sleep
// function: sleep
// param:    timeout, msec
sleep(123);

// cmd:      breakpoint
// function: breakpoint.
// waiting for the 'continue' button to press.
printf("""");
printf(""Breakpoint function example"");
printf(""Click the 'CONTINUE' button"");
breakpoint();

printf("""");
printf(""Send function example"");

// cmd:       send the CAN message
// function:  send (11 bit) / send11 (11 bit) / send29 (29 bit)
// param1:    can id  (123 - dec, 0x123 - hex)
// param2..9: data bytes (12 - dec, 0x12 - hex)
send( 0x100, 1, 2, 3, 4, 0x05, 0x6, var0[0] );
send11( 0x7FF, 0);
send29( 0x17FFFF, 0, 1, 2 );

printf("""");
printf(""Read function example"");

// cmd:        read the CAN message
// function:   read (11 bit) / read11 (11 bit) / read29 (29 bit)
// param1:     can id  (123 - dec, 0x123 - hex)
// param2:     timeout (msec) or 'inf' to wait with no timeout
// param3..10: data bytes: 0x10, 0x*0, 0x1*, **, ***

// the 1st byte should be 0x10, the 2nd one shold be 0xF0..0xFF
// other bytes will be ignored. DLC >= 2
int read_tmo = 200;
var7 = read(0x123, read_tmo, 0x10, 0xF*, ***);
// almost the same, but DLC = 5
var7 = read(0x123, 250, 0x10, 0xF*, **, **, 0x0C);
// wait with no timeout (timeout = inf)
printf("""");
printf(""Start endless reading, timeout = inf"");
printf(""Click the 'STOP' button"");
read(0x123, inf, ***);


printf(""Finish"");

#else

int sleep_ms = 1000;
printf(""Else section. Just wait for 1 sec"");
sleep(sleep_ms);

#endif
";
        }
    }
}



