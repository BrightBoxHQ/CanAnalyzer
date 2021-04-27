using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;

namespace canAnalyzer
{
    public partial class ucGenericTest : UserControl
    {
        /* private stuff */
        private CanMessageSendTool m_can_tool;
        private Thread m_worker;
        private bool m_stop_worker_req;
        private List<canMessage2> m_can_list_rx;
        private List<canMessage2> m_can_list_rx_trace;

        private Mutex m_rx_lock = new Mutex();
        private string dataPath = string.Empty;
        private StringBuilder m_sbTrace = new StringBuilder();

        private int m_scan_flow_sec = 0;
        private bool m_is_sending_requests = false;

        /* internal default strings */
        private readonly string m_str_default_vehicle = "Your vehicle name";
        private readonly string m_str_default_comment = "Custom text";
        private readonly string[] m_str_pwr_states = {
            "Off", "ACC", "Ignition", "Running" };
        private readonly string m_str_default_pwr_state = "None";

        /* constructor */
        public ucGenericTest(CanMessageSendTool canSendTool)
        {
            InitializeComponent();
            /* create */
            m_can_tool = canSendTool;
            m_stop_worker_req = true;
            m_can_list_rx = new List<canMessage2>();
            m_can_list_rx_trace = new List<canMessage2>();
            /* ui */
            uiInit();
            uiUpdate();
        }

        /* force stop */
        public void stop()
        {
            workerStop();
        }
       
        // returns data path
        public string dataPathGet()
        {
            return dataPath;
        }

        // update data path
        public void dataPathSet(string path)
        {
            dataPath = string.IsNullOrEmpty(path) ? string.Empty : path;
            tbSavePath.Text = dataPath;
        }

        // add new messages
        public void addMessageList(List<canMessage2> ls)
        {
            if (!workerIsRunning())
                return;
            // lock
            m_rx_lock.WaitOne();
            // 1. add to the reponse queue
            if (m_is_sending_requests)
                m_can_list_rx.AddRange(ls);
            // 2. add to the common trace queue
            m_can_list_rx_trace.AddRange(ls);
            m_rx_lock.ReleaseMutex();
        }

        public bool isRunning()
        {
            return workerIsRunning();
        }
   
        /* is the worker running? */
        private bool workerIsRunning()
        {
            if (null == m_worker)
                return false;

            return m_worker.ThreadState == ThreadState.Stopped || m_stop_worker_req ? 
                false : true;
        }

        // start the worker
        private void workerStart()
        {
            if (workerIsRunning())
                return;

            dt_start = DateTime.Now;

            // create the file name
            str_fname = string.Empty;
            if (tbVehicleName.Text != m_str_default_vehicle)
            {
                str_fname += tbVehicleName.Text;
            }
            if (tbComment.Text != m_str_default_comment)
            {
                if (str_fname != string.Empty)
                    str_fname += "__";
                str_fname += tbComment.Text;
            }
            if (cbVehiclePwrState.SelectedItem.ToString() != m_str_default_pwr_state)
            {
                if (str_fname != string.Empty)
                    str_fname += "__";
                str_fname += "pwr" + cbVehiclePwrState.SelectedItem.ToString();
            }
            // date time
            if (str_fname != string.Empty)
                str_fname += "__";
            str_fname += string.Format(dt_start.ToString("yyyy_MM_dd_HH_mm_ss"));

            // check the path, run the dialog
            if (!Directory.Exists(dataPath))
            {
                FolderBrowserDialog dlg = new FolderBrowserDialog();
                if (DialogResult.OK == dlg.ShowDialog())
                {
                    dataPath = dlg.SelectedPath + "\\";
                    tbSavePath.Text = dataPath;
                }
            }

            // is the path exist?
            if (!Directory.Exists(dataPath))
            {
                trace("Cannot be started, the directory is not found", Color.Red);
                return;
            }

            // append the '\' char to the path
            if (dataPath.LastIndexOf('\\') != (dataPath.Length - 1))
            {
                dataPath += "\\";
                tbSavePath.Text = dataPath;
            }
            // 
            str_fname = dataPath + str_fname;

            m_stop_worker_req = false;
            m_worker = new Thread(workerRoutine);
            m_worker.Name = "generic test worker";
            m_worker.Start();
        }

        /* stop the worker */
        private void workerStop()
        {
            m_stop_worker_req = true;
        }

        /* returns null if a error is detected */
        private List<can_response> sendRequesGetResponses(int ecu, byte[] cmd, bool is_obd, bool is29bit)
        {
            const int sleep_step = 5;
            const int sleep_increase = 50;
            int sleep_dftl = 150;  /* 50 from the proto  + 100 just in case */
            int sleep_ext  = 500;  /* 150 from the proto + 350 just in case */

            bool has_pid = cmd.Length > 1;
            bool sent = false;
            List<can_response> resp_list = new List<can_response>();

            /* check the cmd */
            if (cmd.Length == 0)
                return null;
            if (is_obd && cmd.Length > 2)
                return null;

            /* broadcast */
            if (ecu == 0)
                ecu = is29bit ? 0x18DB33F1 : 0x7DF;

            for (int attempt = 0; attempt < 2 && !m_stop_worker_req; attempt++)
            {
                // clean the rx list
                m_rx_lock.WaitOne();
                m_can_list_rx.Clear();
                m_rx_lock.ReleaseMutex();
                // clean the response list 
                resp_list.Clear();

                // send the request
                if (is_obd)
                {
                    if (!has_pid) // send sid only
                        sent = sendObdRequest(ecu, cmd[0], is29bit);
                    else // send sid + pid
                        sent = sendObdRequest(ecu, cmd[0], cmd[1], is29bit);
                }
                else
                {
                    sent = sendRequest(ecu, cmd, is29bit);
                }
                // stop if the request cannot be sent
                if (!sent)
                {
                    return null;
                }

                int sleep_tmo = sleep_dftl;

                // getting the messages
                while (sleep_tmo >= 0 && !m_stop_worker_req)
                {
                    List<canMessage2> msg_list = new List<canMessage2>();
                    m_rx_lock.WaitOne();
                    foreach (var msg in m_can_list_rx)
                        msg_list.Add(msg);
                    m_can_list_rx.Clear();
                    m_rx_lock.ReleaseMutex();

                    /* parse them */
                    if (is_obd)
                    {
                        if (has_pid)
                            can_parser.parse_obd(msg_list, resp_list, cmd[0], cmd[1], is29bit);
                        else
                            can_parser.parse_obd(msg_list, resp_list, cmd[0], is29bit);
                    }
                    else
                    {
                        if (has_pid)
                            can_parser.parse_uds(msg_list, resp_list, cmd[0], cmd[1], is29bit);
                        else
                            can_parser.parse_uds(msg_list, resp_list, cmd[0], is29bit);
                    }

                    /* flow ctrl */
                    for (int idx = 0; idx < resp_list.Count; idx++)
                    {
                        if (resp_list[idx].needToSendFlow() && !resp_list[idx].IsFlowCtrlMsgSent)
                        {
                            resp_list[idx].IsFlowCtrlMsgSent = sendFlowControl(resp_list[idx].getId(), is29bit);
                            sleep_tmo = sleep_ext;
                        }
                    }

                    sleep_tmo -= sleep_step;
                    Thread.Sleep(sleep_step);
                }

                // stop
                if (resp_list.Count > 0)
                {
                    bool got_all_msg = true;
                    foreach (var rsp in resp_list)
                    {
                        if (!rsp.isFinished())
                            got_all_msg = false;
                    }

                    if (got_all_msg)
                        break;

                    sleep_dftl += sleep_increase;
                    sleep_ext += sleep_increase;
                }
            }

            /* append */
            foreach(var rsp in resp_list)
            {
                ecuList_append(rsp.getId());
            }

            return resp_list;
        }

        /* returns null if a error is detected */
        private List<can_response> sendObdRequesGetResponses(int ecu_id, byte service, byte pid, bool is29bit)
        {
            byte[] cmd = { service, pid };
            return sendRequesGetResponses(ecu_id, cmd, true, is29bit);
        }

        /* returns null if a error is detected */
        private List<can_response> sendObdRequesGetResponses(int ecu_id, byte service, bool is29bit)
        {
            byte[] cmd = { service };
            return sendRequesGetResponses(ecu_id, cmd, true, is29bit);
        }

        private void trace_responses(List<can_response> resp_list, string str_title)
        {
            if (resp_list.Count() == 0)
            {
                trace(" No response: " + str_title, Color.DarkOrange);
            }
            else
            {
                for (int i = 0; i < resp_list.Count; i++)
                {
                    var item = resp_list[i];
                    if (item.isFinished())
                    {
                        trace(string.Format(" {0}: {1}",
                            item.getIdAsString(), item.getDataAsString()));
                    }
                    else
                    {
                        trace(string.Format(" {0} (Not Finished): {1}",
                            item.getIdAsString(), item.getDataAsString()), Color.DarkOrange);
                    }
                }
            }
        }

        private List<int> can_resp_ecu_list = new List<int>();

        private void ecuList_append(int ecu_id)
        {
            bool found = false;
            for (int i = 0; i < can_resp_ecu_list.Count && !found; i++)
                found = can_resp_ecu_list[i] == ecu_id;
        
            if (!found)
                can_resp_ecu_list.Add(ecu_id);
        }

        private List<can_response> sendObdRequestGetResponsesParseThem(int ecu_id, bool is29bit, byte service, byte pid)
        {
            string str_req_info = ecu_id == 0 ?
                string.Format("Sid 0x{0}, Pid 0x{1}", service.ToString("X2"), pid.ToString("X2")) :
                string.Format("Ecu 0x{0}, Sid 0x{1}, Pid 0x{2}", ecu_id.ToString("X3"), service.ToString("X2"), pid.ToString("X2"));

            /* send the request, wait for responses */
            List<can_response> resp_list = sendObdRequesGetResponses(ecu_id, service, pid, is29bit);

            /* failed to send? */
            if (null == resp_list)
            {
                trace("Failed to send the request: " + str_req_info, Color.Red);
                /* force stop */
                m_stop_worker_req = true;
            }

            /* should we stop? */
            if (m_stop_worker_req)
                return null;

            /* trace */
            trace_responses(resp_list, str_req_info);

            return resp_list;
        }

        private List<can_response> sendObdRequestGetResponsesParseThem(int ecu_id, bool is29bit, byte service)
        {
            string str_req_info = ecu_id == 0 ?
                string.Format("Sid 0x{0}", service.ToString("X2")) :
                string.Format("Ecu 0x{0}, Sid 0x{1}", ecu_id.ToString("X3"), service.ToString("X2"));

            /* send the request, wait for responses */
            List<can_response> resp_list = sendObdRequesGetResponses(ecu_id, service, is29bit);

            /* failed to send? */
            if (null == resp_list)
            {
                trace("Failed to send the request: " + str_req_info, Color.Red);
                /* force stop */
                m_stop_worker_req = true;
            }

            /* should we stop? */
            if (m_stop_worker_req)
                return null;

            /* trace */
            trace_responses(resp_list, str_req_info);

            return resp_list;
        }

        private List<can_response> sendRawRequestGetResponsesParseThem(int ecu_id, bool is29bit, byte[] cmd)
        {
            string str_req_info = string.Empty;
            if (ecu_id != 0)
                str_req_info += "Ecu " + ecu_id.ToString("X3") + ", ";
            foreach (byte b in cmd)
                str_req_info += b.ToString("X2") + " ";

            /* send the request, wait for responses */
            List<can_response> resp_list = sendRequesGetResponses(ecu_id, cmd, false, is29bit);

            /* failed to send? */
            if (null == resp_list)
            {
                trace("Failed to send the request: " + str_req_info, Color.Red);
                /* force stop */
                m_stop_worker_req = true;
            }

            /* should we stop? */
            if (m_stop_worker_req)
                return null;

            /* trace */
            trace_responses(resp_list, str_req_info);

            return resp_list;
        }

        private string conv_payload_to_str(List<byte> ls, int offset)
        {
            StringBuilder sb = new StringBuilder();
            foreach (var b in ls)
            {
                if (offset > 0)
                {
                    offset--;
                    continue;
                }

                char ch = b == 0 ? ' ' : (char)b;
                sb.Append(ch);
            }
            return sb.ToString();
        }

        private void handle_result(List<can_response> new_resp_list, string str_req_info, List<can_response> total_resp_ls)
        {
            // failed to send?
            if (null == new_resp_list)
            {
                if (string.IsNullOrEmpty(str_req_info))
                    trace("Failed to send the request", Color.Red);
                else
                    trace("Failed to send the request: " + str_req_info, Color.Red);
                // force stop
                m_stop_worker_req = true;
            }
            // append
            if (new_resp_list != null)
                total_resp_ls.AddRange(new_resp_list);
        }


        private string can_list_to_string (List<canMessage2> ls)
        {
            // convert to string array
            //canTraceUtils.timestamp ts = new canTraceUtils.timestamp();
            canTraceUtils.timestamp_offset ts = new canTraceUtils.timestamp_offset();
            canTraceUtils.mConverter conv = new canTraceUtils.mConverter();
            conv.TS = ts;
            List<string> s_list = new List<string>();
            for (int i = 0; i < ls.Count; i++)
            {
                object[] obj = conv.msg2row(ls[i], i);
                s_list.Add(conv.obj2str(obj, true));
            }
            // to string
            StringBuilder sb = new StringBuilder();
            sb.Append(conv.getHeaderString());
            sb.Append(Environment.NewLine);
            foreach (string str in s_list)
            {
                sb.Append(str);
                sb.Append(Environment.NewLine);
            }

            return sb.ToString();
        }

        private bool m_use_29bit = false;
        private bool m_use_direct_ecu = false;
        private string str_fname = string.Empty;
        private DateTime dt_start;

        /* worker routine */
        private void workerRoutine()
        {
            byte service = 1;
            bool stop_obd = false;
            const int obd_data_offset = 2; // sid + pid
            Color cla_evt = Color.ForestGreen;
            Color cla_title = Color.DarkBlue;

            bool use_29bit_req = m_use_29bit;

            List<can_response> obd_resp_ls = new List<can_response>(); // responses
            List<canMessage2> flow_trace = new List<canMessage2>();    // 


            // VIN
            List<byte[]> cmd_vin_common = new List<byte[]>();
            cmd_vin_common.Add(new byte[] { 0x22, 0xF1, 0x90 });
            cmd_vin_common.Add(new byte[] { 0x22, 0xF1, 0xA0 });
            cmd_vin_common.Add(new byte[] { 0x22, 0xF1, 0xB0 });
            cmd_vin_common.Add(new byte[] { 0x1A, 0x90 });

            // wait for 500 msec to make sure the hi-prio mode started
            Thread.Sleep(500);

// 1. prepare 

            // clean trace
            m_sbTrace.Clear();
            // clean ecu list
            can_resp_ecu_list.Clear();
            // start message
            trace(string.Format("Start {0}", dt_start.ToString("dd-MM-yyyy HH:mm:ss")),
                cla_title);
            
// 2. flow
            if (m_scan_flow_sec > 0)
            {
                // clean rx queue
                m_rx_lock.WaitOne();
                m_can_list_rx_trace.Clear();
                m_is_sending_requests = false;
                m_rx_lock.ReleaseMutex();

                // wait for m_scan_flow_sec seconds
                trace(string.Format("Getting trace for {0} sec", m_scan_flow_sec), cla_title);
                for (int wait_sec = 0; wait_sec < m_scan_flow_sec && !m_stop_worker_req; wait_sec++)
                {
                    Thread.Sleep(1000);
                }
                // copy
                m_rx_lock.WaitOne();
                flow_trace.AddRange(m_can_list_rx_trace);
                m_can_list_rx_trace.Clear();
                m_rx_lock.ReleaseMutex();

                // handle
                if (!m_stop_worker_req)
                {
                    if (flow_trace.Count > 0)
                    {
                        trace(string.Format("Got {0} Flow messages", flow_trace.Count));
                        // convert to string array
                        string s_flow_trace = can_list_to_string(flow_trace);
                        // save
                        System.IO.File.WriteAllText(str_fname + "_flow_trace.txt", s_flow_trace);
                    }
                    else
                    {
                        trace("No Flow messages");
                    }
                }
            }

// 3. requests
            // clean rx queue
            m_rx_lock.WaitOne();
            m_can_list_rx_trace.Clear();
            m_is_sending_requests = true;
            m_rx_lock.ReleaseMutex();

            // 3.1 request PIDs, Service 0x01
            service = 1;
            trace(string.Format("Read OBD PIDs (SID = {0})", service.ToString("X2")), cla_title);
            for (byte pid = 0; pid < 0xFF && !m_stop_worker_req && !stop_obd; pid++)
            {
                string str_req_info = string.Format("Sid 0x{0}, Pid 0x{1}", 
                    service.ToString("X2"), pid.ToString("X2"));

                // send the request, wait for responses
                List<can_response> resp_list = sendObdRequestGetResponsesParseThem(0, use_29bit_req, service, pid);
                // handle
                handle_result(resp_list, str_req_info, obd_resp_ls);
 
                if (pid == 0x1F && resp_list != null)
                {
                    trace("Wait for 2 sec and repeat 1F request");
                    Thread.Sleep(2000);
                    sendObdRequestGetResponsesParseThem(0, use_29bit_req, service, pid);
                }

                // do we really need to continue? does the car support the next pid pool?
                if ((pid % 32) == 0 && resp_list != null)
                {  
                    stop_obd = true;
                    for (int i = 0; i < resp_list.Count; i++)
                    {
                        var pl = resp_list[i].getData();
                        for (int pl_pos = obd_data_offset; pl_pos < pl.Count; pl_pos++)
                            if (pl[pl_pos] != 0)
                                stop_obd = false;
                    }

                    if (stop_obd)
                    {
                        trace("Skip other PIDs");
                    }
                }
            }

// 3.2 request PIDs, Service 0x09
            if (!m_stop_worker_req)
            {
                service = 9;
                stop_obd = false;
                trace(string.Format("Read OBD PIDs (SID = {0})", service.ToString("X2")), cla_title);
            }
            for (byte pid = 0; pid < 32 && !m_stop_worker_req && !stop_obd; pid++)
            {
                string str_req_info = string.Format("Sid 0x{0}, Pid 0x{1}",
                    service.ToString("X2"), pid.ToString("X2"));

                // send the request, wait for responses
                List<can_response> resp_list = sendObdRequestGetResponsesParseThem(0, use_29bit_req, service, pid);
                // handle
                handle_result(resp_list, str_req_info, obd_resp_ls);

                // do we really need to continue? does the car support the next pid pool?
                if ((pid % 32) == 0 && resp_list != null)
                {
                    stop_obd = true;
                    for (int i = 0; i < resp_list.Count; i++)
                    {
                        var pl = resp_list[i].getData();
                        for (int pl_pos = obd_data_offset; pl_pos < pl.Count; pl_pos++)
                            if (pl[pl_pos] != 0)
                                stop_obd = false;
                    }

                    if (stop_obd)
                    {
                        trace("Skip other PIDs");
                    }
                }
            }

// 3.3 request OBD DTCs
            if (!m_stop_worker_req)
                trace("Read OBD DTCs", cla_title);
            byte[] dtc_svcs = {0x03, 0x07, 0x0A};
            foreach(var s in dtc_svcs)
            {
                if (m_stop_worker_req)
                    break;
                // send the request, wait for responses
                List<can_response> resp_list = sendObdRequestGetResponsesParseThem(0, use_29bit_req, s);
                // handle
                handle_result(resp_list, null, obd_resp_ls);
            }

// 3.4 UDS VIN
            if (!m_stop_worker_req)
            {
                trace("Read UDS VIN", cla_title);
                foreach (var cmd in cmd_vin_common)
                {
                    if (m_stop_worker_req)
                        break;
                    List<can_response> resp_list =
                        sendRawRequestGetResponsesParseThem(0, use_29bit_req, cmd);
                    // handle
                    handle_result(resp_list, null, obd_resp_ls);
                }
            }

// 3.5 UDS Tester Present
            if (!m_stop_worker_req)
            {
                trace("Send Tester Present", cla_title);
                List<byte[]> cmd_tester = new List<byte[]>();
                cmd_tester.Add(new byte[] { 0x3E });
                cmd_tester.Add(new byte[] { 0x3E, 0x00 });
                cmd_tester.Add(new byte[] { 0x3E, 0x01 });

                foreach (var req in cmd_tester)
                {
                    if (m_stop_worker_req)
                        break;
                    List<can_response> resp_list =
                        sendRawRequestGetResponsesParseThem(0, use_29bit_req, req);
                    // handle
                    handle_result(resp_list, null, obd_resp_ls);
                }   
            }
// 3.6 UDS Test
            if (!m_stop_worker_req)
            {
                trace("UDS test", cla_title);
                List<byte[]> cmd_test = new List<byte[]>();
                cmd_test.Add(new byte[] { 0x22, 0xF1, 0x91 });
                cmd_test.Add(new byte[] { 0x22, 0xF1, 0x97 });
                //cmd_test.Add(new byte[] { 0x22, 0xF1, 0xA0 });

                foreach (var req in cmd_test)
                {
                    if (m_stop_worker_req)
                        break;
                    List<can_response> resp_list =
                        sendRawRequestGetResponsesParseThem(0, use_29bit_req, req);
                    // handle
                    handle_result(resp_list, null, obd_resp_ls);
                }
            }

// 3.7 direct OBD/UDS requests
            if (!m_stop_worker_req)
            {
                if (m_use_direct_ecu)
                {
                    if (can_resp_ecu_list.Count == 0)
                    {
                        trace("There is no answered ECUs to check", Color.Red);
                    }
                    else
                    {
                        trace(string.Format("Direct requests to the answered ECUs, ECU cnt={0}",
                            can_resp_ecu_list.Count),
                            cla_title);
                    }
                }
                else
                    trace("Skip direct requests to the answered ECUs", cla_title);
            }
                
            foreach (var resp_id in can_resp_ecu_list)
            {
                if (!m_stop_worker_req && resp_id != 0 && m_use_direct_ecu)
                {
                    int req_id = response_id_to_request_id(resp_id, use_29bit_req);
                    byte [] services = {0x01, 0x01, 0x01, 0x01, 0x01, 0x09, 0x09};
                    byte [] pids =     {0x00, 0x01, 0x0C, 0x0D, 0x2F, 0x02, 0x0A};
                    List<can_response> resp_list = null;
                    for (int req_idx = 0; req_idx < pids.Length; req_idx++)
                    {
                        if (m_stop_worker_req)
                            break;
                        resp_list = sendObdRequestGetResponsesParseThem(req_id, use_29bit_req,
                            services[req_idx], pids[req_idx]);
                        // handle
                        handle_result(resp_list, null, obd_resp_ls);
                    }
                    // VIN
                    foreach (var cmd in cmd_vin_common)
                    {
                        if (m_stop_worker_req)
                            break;
                        resp_list = sendRawRequestGetResponsesParseThem(req_id, use_29bit_req, cmd);
                        // handle
                        handle_result(resp_list, null, obd_resp_ls);
                    }
                }   
            }

// 4. extract and trace useful data
            if (!m_stop_worker_req)
            {
                trace("Data", cla_title);
                trace("Number of responded ECU: " + can_resp_ecu_list.Count.ToString(), cla_evt);
            }

            List<string> list_report_trace = new List<string>();
            foreach (var rsp in obd_resp_ls)
            {
                List<byte> resp_buff = rsp.getData();

                if (resp_buff.Count < 2)
                    continue;
                if (m_stop_worker_req)
                    break;

                // data
                byte item_sid = rsp.getData()[0];
                byte item_pid = rsp.getData()[1];
                string title = string.Empty;
                string data = string.Empty;
                
                // OBD, service 0x09
                if (item_sid == 0x49)
                {
                    if (item_pid == 0x02)
                    {
                        title = "OBD VIN";
                        data = conv_payload_to_str(resp_buff, 3);
                    }  
                    if (item_pid == 0x0A)
                    {
                        title = "OBD Name";
                        data = conv_payload_to_str(resp_buff, 3);
                    }
                }
                // obd service 0x01
                if (item_sid == 0x41)
                {
                    const int start_pos = 2;
                    // rpm
                    if (item_pid == 0x0C)
                    {
                        title = "OBD RPM";
                        if (resp_buff.Count >= 4)
                        {
                            int tmp = (resp_buff[start_pos] << 8) + resp_buff[start_pos+1];
                            tmp = tmp / 4;
                            data = tmp.ToString();
                        }
                    }
                    // speed
                    if (item_pid == 0x0D)
                    {
                        title = "OBD Speed";
                        if (resp_buff.Count >= 3)
                        {
                            int tmp = resp_buff[start_pos];
                            data = tmp.ToString() + " km/h";
                        }
                    }
                    // fuel
                    if (item_pid == 0x2F)
                    {  
                        title = "OBD Fuel";
                        if (resp_buff.Count >= 3)
                        {
                            float tmp = resp_buff[start_pos];
                            tmp = tmp * 100 / 255;
                            data = tmp.ToString("F1") + " %";
                        }
                    }
                    // enginge running sec
                    if (item_pid == 0x1F)
                    {
                        title = "OBD Engine Run time";
                        if (resp_buff.Count >= 4)
                        {
                            int tmp = (resp_buff[start_pos] << 8) + resp_buff[start_pos + 1];
                            data = tmp.ToString() + " sec";
                        }
                    }
                    // Fuel type
                    if (item_pid == 0x51)
                    {
                        title = "OBD Fuel type";
                        if (resp_buff.Count >= 3)
                        {
                            int tmp = resp_buff[start_pos];
                            data = can_parser.get_obd_fuel_type(tmp);
                        }
                    }
                }
                // UDS
                if (item_sid == 0x62)
                {
                    if (resp_buff.Count >= 3 && item_pid == 0xF1 && resp_buff[2] == 0x90)
                    {
                        title = "UDS VIN 0x22 0xF1 0x90";
                        data = conv_payload_to_str(rsp.getData(), 3);
                    }
                    if (resp_buff.Count >= 3 && item_pid == 0xF1 && resp_buff[2] == 0xA0)
                    {
                        title = "UDS VIN 0x22 0xF1 0xA0";
                        data = conv_payload_to_str(rsp.getData(), 3);
                    }
                }
                // UDS-like VIN (0x1A)
                if (item_sid == 0x5A)
                {
                    if (item_pid == 0x90)
                    {
                        title = "UDS VIN 0x1A 0x90";
                        data = conv_payload_to_str(rsp.getData(), 2);
                    }
                }

                // trace
                if (!string.IsNullOrEmpty(data) && !string.IsNullOrEmpty(title))
                {
                    string str_info = string.Format("{0}, {1}: {2}",
                        rsp.getIdAsString(),
                        title, data);
                    // already traced?
                    if (!list_report_trace.Contains(str_info))
                    {
                        trace(str_info, cla_evt);
                        list_report_trace.Add(str_info);
                    }
                }
            }
      
            // one more trace
            if (!m_stop_worker_req)
            {
                List<canMessage2> req_trace = new List<canMessage2>();
                m_rx_lock.WaitOne();
                req_trace.AddRange(m_can_list_rx_trace);
                m_can_list_rx_trace.Clear();
                m_rx_lock.ReleaseMutex();
                // convert to string array
                string s_flow_trace = can_list_to_string(req_trace);
                // save
                System.IO.File.WriteAllText(str_fname + "_req_trace.txt", s_flow_trace);
            }

// 5. finish
            trace("Stop", cla_title);
            trace("");
            // save the report
            if (!m_stop_worker_req)
            {
                System.IO.File.WriteAllText(str_fname + "_report.txt", m_sbTrace.ToString());
            }
            m_sbTrace.Clear();
            // stop
            m_stop_worker_req = true;
            m_is_sending_requests = false;
            // update UI
            Invoke(new Action(uiUpdate));
        }


        private int response_id_to_request_id(int rsp_can_id, bool is29bit)
        {
            int req_id = rsp_can_id;
            if (is29bit)
            {
                req_id = 0x18DA00F1 | ((rsp_can_id & 0xFF) << 8);
            }
            else
            {
                req_id = rsp_can_id - 8;
            }

            return req_id;
        }

        private bool sendFlowControl(int rsp_can_id, bool is29bit)
        {
            byte[] data = {0x30, 0, 0, 0, 0, 0, 0, 0};
            int req_id = response_id_to_request_id(rsp_can_id, is29bit);
 
            canMessage2 msg = new canMessage2(req_id, is29bit, data, 0);
            return m_can_tool.SendCanMessage(msg);
        }


        private bool sendRequest(int can_id, byte[] cmd, bool is29bit)
        {
            const byte dummy = 0xFF;
            if (cmd.Length == 0 || cmd.Length > 7 || cmd[0] == 0)
                return false;

            byte[] data_req = new byte[8];
            // len 
            data_req[0] = (byte)cmd.Length;
            // data
            for (int i = 1; i < data_req.Length; i++)
                data_req[i] = dummy;
            for (int i = 0; i < cmd.Length; i++)
                data_req[i + 1] = cmd[i];
            // send
            canMessage2 msg = new canMessage2(can_id, is29bit, data_req, 0);
            return m_can_tool.SendCanMessage(msg);
        }

        private bool sendObdRequest(int can_id, byte service, byte pid, bool is29bit)
        {
            if (service == 0)
                return false;
            byte[] cmd2 = { service, pid };
    
            return sendRequest(can_id, cmd2, is29bit);
        }

        private bool sendObdRequest(int can_id, byte service, bool is29bit)
        {
            if (service == 0)
                return false;
            byte[] cmd1 = { service };

            return sendRequest(can_id, cmd1, is29bit);
        }

        /* ui initialization, call it within the constructor */
        private void uiInit()
        {
            /* resize */
            this.Dock = DockStyle.Fill;
            /* trace */
            tbTrace.Multiline = true;
            tbTrace.Dock = DockStyle.Fill;
            tbTrace.ScrollBars = RichTextBoxScrollBars.Vertical;
            Font f = new Font("Consolas", 8.5f, FontStyle.Italic);
            tbTrace.Font = f;
            /* name and comment */
            tbVehicleName.Text = m_str_default_vehicle;
            tbComment.Text = m_str_default_comment;
            /* power state combo box */
            cbVehiclePwrState.Items.Add(m_str_default_pwr_state);
            foreach (string s_item in m_str_pwr_states)
                cbVehiclePwrState.Items.Add(s_item);
            cbVehiclePwrState.SelectedIndex = 0;
            /* progress bar */
            //pbProgress.Minimum = 0;
            //pbProgress.Maximum = 100;
            //pbProgress.Value = 0;
            /* config */
            numCfgTrace.Minimum = 0;    // 0 means disabled
            numCfgTrace.Maximum = 60;   // up to 1 minute
            numCfgTrace.Value = 15;     // 15 seconds
            m_scan_flow_sec = (int)numCfgTrace.Value;
            cbObd.Checked = true;
            cbObd29bit.Checked = false;
            cbDirectEcuReq.Checked = true;

            m_use_direct_ecu = cbDirectEcuReq.Checked;

            cbObd.CheckedChanged += onConfigChanged;
            cbObd29bit.CheckedChanged += onConfigChanged;
            cbDirectEcuReq.CheckedChanged += onConfigChanged;
            numCfgTrace.ValueChanged += onConfigChanged;
            tbSavePath.TextChanged += onConfigChanged;
            /* button */
            btnStart.Click += onBtnClicked;
        }

        /* ui update */
        private void uiUpdate()
        {
            bool isInProcess = workerIsRunning();
            btnStart.Enabled = numCfgTrace.Value != 0 || cbObd.Checked;
            btnStart.Text = isInProcess ? "Stop" : "Start";

            pnlInfo.Enabled = !isInProcess;
            gbConfig.Enabled = !isInProcess;
            tbSavePath.Enabled = !isInProcess;
        }

        /* on button clicked */
        private void onBtnClicked(object sender, EventArgs e)
        {
            if (workerIsRunning())
            {
                workerStop();
            } else
            {
                if (m_can_tool.IsSendingAllowed())
                    workerStart();
                else
                    trace("Failed to start. Sending messages is prohibited.\n" +
                          "The CAN Analyzer is not connected, or Listen Only mode is switched on.\n", 
                          Color.Red);
            }

            /* update UI */
            Invoke(new Action(uiUpdate));
        }

        
        /* the config has been just changed */
        private void onConfigChanged(object sender, EventArgs e)
        {
            m_use_29bit = cbObd29bit.Checked;
            m_use_direct_ecu = cbDirectEcuReq.Checked;

            m_scan_flow_sec = (int)numCfgTrace.Value;
            dataPath = tbSavePath.Text;
            /* update UI */
            if (InvokeRequired)
                Invoke(new Action(uiUpdate));
            else
                uiUpdate();
        }

        private void trace(string str)
        {
            trace(str, Color.Black);
        }

        private void trace(string str, Color color)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string, Color>(trace), new object[] { str, color });
                return;
            }

            const string emptyLine = "\"\"";

            if (str != emptyLine)
            {
                tbTrace.SelectionStart = tbTrace.TextLength;
                tbTrace.SelectionLength = 0;

                tbTrace.SelectionColor = color;
                tbTrace.AppendText(str);
                tbTrace.SelectionColor = tbTrace.ForeColor;

                // add to sb
                m_sbTrace.Append(str);
                m_sbTrace.Append(Environment.NewLine);
            }

            tbTrace.AppendText(Environment.NewLine);
            tbTrace.ScrollToCaret();
        }

        private static class can_parser
        {

            static public string get_obd_fuel_type(int fuel_type)
            {
                string res = string.Empty;

                switch (fuel_type)
                {
                    case 0: res = "Not available"; break;
                    case 1: res = "Gasoline"; break;
                    case 2: res = "Methanol"; break;
                    case 3: res = "Ethanol"; break;
                    case 4: res = "Diesel"; break;
                    case 5: res = "LPG"; break;
                    case 6: res = "CNG"; break;
                    case 7: res = "Propane"; break;
                    case 8: res = "Electric"; break;
                    case 9: res = "Bifuel running Gasoline"; break;
                    case 10: res = "Bifuel running Methanol"; break;
                    case 11: res = "Bifuel running Ethanol"; break;
                    case 12: res = "Bifuel running LPG"; break;
                    case 13: res = "Bifuel running CNG"; break;
                    case 14: res = "Bifuel running Propane"; break;
                    case 15: res = "Bifuel running Electricity"; break;
                    case 16: res = "Bifuel running electric and combustion engine"; break;
                    case 17: res = "Hybrid gasoline"; break;
                    case 18: res = "Hybrid Ethanol"; break;
                    case 19: res = "Hybrid Diesel"; break;
                    case 20: res = "Hybrid Electric"; break;
                    case 21: res = "Hybrid running electric and combustion engine"; break;
                    case 22: res = "Hybrid Regenerative"; break;
                    case 23: res = "Bifuel running diesel "; break;
                    // 
                    default: res = "Unknown"; break;
                }

                return res;
            }

            /*
            static public string get_fuel_type(can_response rsp)
            {
                string res = string.Empty;
                List<byte> buff = rsp.getData();
                if (rsp.isFinished() && buff.Count >= 3 && buff[0] == 0x41 && buff[1] == 0x51)
                {
                    byte data = buff[2];
                    switch (data)
                    {
                        case 0: res = "NA"; break;
                        case 1: res = "Gasoline"; break;
                        case 2: res = "Methanol"; break;
                        case 3: res = "Ethanol"; break;
                        case 4: res = "Diesel"; break;
                        case 5: res = "LPG"; break;

                        default: res = "Unknown"; break;
                    }
                }

                return res;
            }
            */

            static public int get_rpm(can_response rsp)
            {
                int rpm = 0;

                return rpm;
            }

            static public void parse_obd(List<canMessage2> rx, List<can_response> resp, byte service, bool is29bit)
            {
                parse_obd(rx, resp, service, 0xFF, is29bit);
            }

            static public void parse_obd(List<canMessage2> rx, List<can_response> resp, byte service, byte pid, bool is29bit)
            {
                if (rx.Count == 0)
                    return;

                foreach(var msg in rx)
                {
                    /* is this message can be used? */
                    if (!can_response.isMessageCorrect(msg, is29bit))
                        continue;

                    int idx = -1;
                    /* is the message already exists in the response list? */
                    for (int i = 0; i < resp.Count && idx < 0; i++)
                    {
                        if (msg.Id.Id == resp[i].getId())
                        {
                            idx = i;
                        }
                    }
                    /* prepare to add */
                    if (idx < 0)
                    {
                        /* check the service */
                        if (can_response.isFirstFrameMessage(msg))
                        {
                            byte msg_svc = can_response.getRequestedService(msg);
                            if (msg_svc == service)
                            {
                                bool is_pid_ok = false;
                                if (pid == 0xFF)
                                    is_pid_ok = true;
                                else
                                    is_pid_ok = pid == msg.Data[can_response.getFirstPayloadPos(msg) + 1];
                                /* allocate */
                                resp.Add(new can_response(is29bit));
                                idx = resp.Count() - 1;
                            }
                        }
                    }

                    if (idx >= 0)
                        resp[idx].add(msg);          
                }
            }

            static public void parse_uds(List<canMessage2> rx, List<can_response> resp, byte service, byte cmd1, bool is29bit = false)
            {
                if (rx.Count == 0)
                    return;

                foreach (var msg in rx)
                {
                    /* is this message can be used? */
                    if (!can_response.isMessageCorrect(msg, is29bit))
                        continue;

                    int idx = -1;
                    /* is the message already exists in the response list? */
                    for (int i = 0; i < resp.Count && idx < 0; i++)
                    {
                        if (msg.Id.Id == resp[i].getId())
                        {
                            idx = i;
                        }
                    }
                    /* prepare to add */
                    if (idx < 0)
                    {
                        /* check the service */
                        if (can_response.isFirstFrameMessage(msg))
                        {
                            byte msg_svc = can_response.getRequestedService(msg);
                            if (msg_svc == service)
                            {
                                byte cmd_rcvd = msg.Data[can_response.getFirstPayloadPos(msg) + 1];
                                if (cmd1 == cmd_rcvd)
                                {
                                    /* allocate */
                                    resp.Add(new can_response(is29bit));
                                    idx = resp.Count() - 1;
                                }
                            }
                        }
                    }

                    if (idx >= 0)
                        resp[idx].add(msg);
                }
            }

            static public void parse_uds(List<canMessage2> rx, List<can_response> resp, byte service, bool is29bit = false)
            {
                if (rx.Count == 0)
                    return;

                foreach (var msg in rx)
                {
                    /* is this message can be used? */
                    if (!can_response.isMessageCorrect(msg, is29bit))
                        continue;

                    int idx = -1;
                    /* is the message already exists in the response list? */
                    for (int i = 0; i < resp.Count && idx < 0; i++)
                    {
                        if (msg.Id.Id == resp[i].getId())
                        {
                            idx = i;
                        }
                    }
                    /* prepare to add */
                    if (idx < 0)
                    {
                        /* check the service */
                        if (can_response.isFirstFrameMessage(msg))
                        {
                            byte msg_svc = can_response.getRequestedService(msg);
                            if (msg_svc == service)
                            {
                                byte cmd_rcvd = msg.Data[can_response.getFirstPayloadPos(msg) + 1];
                                if (/*cmd1 == cmd_rcvd*/ true)
                                {
                                    /* allocate */
                                    resp.Add(new can_response(is29bit));
                                    idx = resp.Count() - 1;
                                }
                            }
                        }
                    }

                    if (idx >= 0)
                        resp[idx].add(msg);
                }
            }

        }

        private class can_response
        {
            private List<byte> m_payload;
            private int m_exp_len;
            private bool m_is_29_bit;
            private int m_can_id;
            private bool m_is_finished;
            private bool m_req_to_send_flow;

            public bool IsFlowCtrlMsgSent { get; set; }

            /* access */
            public int getId() {return m_can_id;}
            public List<byte> getData() { return m_payload; }
            public bool isFinished() { return m_is_finished; }
            public bool needToSendFlow() {return m_req_to_send_flow; }

            public string getDataAsString()
            {
                string res = string.Empty;
                for (int i = 0; i < m_payload.Count; i++)
                {
                    res += "0x" + m_payload[i].ToString("X2");
                    if (i != m_payload.Count - 1)
                        res += ", ";
                }
                return res;
            }

            public string getIdAsString()
            {
                return "0x" + canMessageId.GetIdAsString(m_can_id, m_is_29_bit);
            }

            /* constructor */
            public can_response(bool is_29_bit)
            {
                m_payload = new List<byte>();
                m_exp_len = 0;
                m_can_id = 0;
                m_is_29_bit = is_29_bit;
                m_is_finished = false;
                m_req_to_send_flow = false;
                IsFlowCtrlMsgSent = false;
            }

            /* is this a multiframe message? */
            static public bool isMultiframeMessage(canMessage2 msg)
            {
                return msg.Id.Dlc == 8 && msg.Data[0] >= 0x10;
            }

            /* is this the 1st frame message */
            static public bool isFirstFrameMessage(canMessage2 msg)
            {
                return msg.Id.Dlc == 8 && msg.Data[0] <= 0x10;
            }

            static public int getFirstPayloadPos(canMessage2 msg)
            {
                if (msg.Id.Dlc != 8)
                    return -1;
                if (msg.Data[0] < 0x10)
                    return 1;
                if (msg.Data[0] == 0x10)
                    return 2;
                return -1;
            }

            /* try to extract the service, returns 0 if cannot do it */
            static public byte getRequestedService(canMessage2 msg)
            {
                if (msg.Id.Dlc != 8)
                    return 0;

                int pos = getFirstPayloadPos(msg);
                if (pos >= 0 && msg.Data[pos] >= 0x40)
                    return (byte)((int)msg.Data[pos] - 0x40);

                return 0;
            }

            /* check the message */
            static public bool isMessageCorrect(canMessage2 msg, bool is29bitMode)
            {
                /* are the dlc and mode correct? */
                if (msg.Id.Dlc != 8 || msg.Id.Is29bit != is29bitMode)
                    return false;
                /* is the message id range correct? */
                if (!is29bitMode && (msg.Id.Id < 0x700 || msg.Id.Id >= 0x7FF))
                    return false;
                /* is this response? */
                if (!is29bitMode && (msg.Id.Id % 16) < 8)
                    return false;
                
                return true;
            }

            /* try to add a new message */
            public bool add(canMessage2 msg)
            {
                /* check message id and dlc */
                if (!isMessageCorrect(msg, m_is_29_bit))
                    return false;
                /* is the same id? */
                if (m_can_id != 0 && m_can_id != msg.Id.Id)
                    return false;
                /* is finished? */
                if (m_is_finished)
                    return false;

                /* reset */
                m_req_to_send_flow = false;

                bool res = false;
                bool is_multiframe = isMultiframeMessage(msg);
                bool is_first_frame_msg = false;
                /* create */
                if (m_can_id == 0)
                {
                    int offset = is_multiframe ? 2 : 1;

                    is_first_frame_msg = isFirstFrameMessage(msg);
                    /* the message should be the 1st message of the frame (or a single one) */
                    if (is_multiframe && !is_first_frame_msg)
                        return false;
                    /* get exp len */
                    int exp_len = is_multiframe ? msg.Data[1] : msg.Data[0];
                    if ((is_multiframe && exp_len <= 8) || (!is_multiframe && exp_len > 7))
                            return false;
                    /* extract */
                    m_exp_len = exp_len;
                    for (int i = offset; i < (m_exp_len + offset) && i < msg.Id.Dlc; i++)
                        m_payload.Add(msg.Data[i]);

                    m_can_id = msg.Id.Id;
                    res = true;
                }
                else /* append */
                {
                    const int offset = 1;
             
                    if (!is_multiframe)
                        return false;
                    if (msg.Data[0] < 0x21)
                        return false;

                    /* 6 bytes from the 0x10 msg, 7 bytes from the next ones */
                    int exp_start_pos = 6 + (msg.Data[0] - 0x21) * 7;

                    if (exp_start_pos != m_payload.Count)
                        return false;

                    for (int i = offset; m_payload.Count < m_exp_len && i < msg.Id.Dlc; i++)
                        m_payload.Add(msg.Data[i]);

                    res = true;
                }

                /* finished? */
                if (m_payload.Count > 0 && m_payload.Count == m_exp_len && res)
                    m_is_finished = true;

                /* indicate we should send the flow control message */
                m_req_to_send_flow = res && !m_is_finished && 
                    is_multiframe && is_first_frame_msg;
                return res;
            }

          
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            FolderBrowserDialog dlg = new FolderBrowserDialog();
            dlg.SelectedPath = tbSavePath.Text;

            if (DialogResult.OK == dlg.ShowDialog())
            {
                    dataPath = dlg.SelectedPath + "\\";
                    tbSavePath.Text = dataPath;
            }
        }
    }
}
