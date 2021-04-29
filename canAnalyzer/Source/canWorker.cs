using System;
using System.Collections.Generic;
using System.Threading;
using canSerialPort;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace canAnalyzer
{
    public class canWorker
    {
        // thread
        private Thread      m_worker;                       // communication thread
        private CancellationTokenSource rxTokenSource = new CancellationTokenSource();
        private CancellationToken rxToken;

        // data
        private ConcurrentBag<canMessage2> m_queueMsgs;     // message queue
        private List<byte>  m_dataToParse;                  // data queue
        private canParser   m_parser;                       // parser
        public canFilter    CanFilter { set; get; }         // filter

        // internal field list (communication)
        private comPort         m_port;                     // serial port
        readonly private int    m_comBaud = 921600;         // baud
        private string          m_comName = String.Empty;   // port name
        private string          m_canSpeed = String.Empty;  // speed
        private string          m_fwVer = String.Empty;     // firmware version
        private bool            m_autoSpeed = false;        // use autospeed
        private bool            m_silentMode = false;       // silent mode

        // transmition
        private BlockingCollection<string> txQueue = new BlockingCollection<string>();
        private Thread  txWorker;
        private CancellationTokenSource txTokenSource = new CancellationTokenSource();
        private CancellationToken txToken;

        private ulong MessageCounter = 0;
        public ulong Received { get { return MessageCounter; } }

        //public bool HighPerformanceMode { get; }
        public EventWaitHandle waitHandle = 
            new EventWaitHandle(false, EventResetMode.AutoReset);

        private bool reset_requested = false;
        private bool paused = false;

        private EventWaitHandle m_sleep_handle =
            new EventWaitHandle(false, EventResetMode.AutoReset);
        private bool m_high_performance_enabled = false;

        public void highPefrormanceModeSet(bool set)
        {
            // do no sleep if we're goint to set it
            if (set && !m_high_performance_enabled)
                m_sleep_handle.Set();
            m_high_performance_enabled = set;
        }

        public bool highPefrormanceModeGet()
        {
            return m_high_performance_enabled;
        }
        // constructor
        public canWorker(ref ConcurrentBag<canMessage2> queue)
        {
            m_parser = new canParser(CanFilter);
            m_dataToParse = new List<byte>();
            m_queueMsgs = queue;
            m_port = new comPort();

            m_high_performance_enabled = false;// HighPerformanceMode = false;
            paused = false;
            reset_requested = false;
        }


        #region methods_access

        private int req_err_cnt_step = 0;
        readonly private int req_err_cnt_step_max = 2; // rx, tx, reset

        public void errorsRefresh()
        {
            if (m_port.isOpen())
            {
                // make sure we handled the previous request
                if (m_parser.RawRegesterReq == canParser.CanRegisterReq.none)
                {
                    if (req_err_cnt_step == 0)
                    {
                        // request the tx error counter
                        m_parser.RawRegesterReq = canParser.CanRegisterReq.tx_error_counter;
                        txQueue.Add("G0F");     // reg addr = 15
                    }
                    else if (req_err_cnt_step == 1) {
                        // request the rx error counter
                        m_parser.RawRegesterReq = canParser.CanRegisterReq.rx_error_counter;
                        txQueue.Add("G0E");     // reg addr = 14
                    }
                    else if (req_err_cnt_step == 2)
                    {
                        // request the CAN reset status
                        m_parser.RawRegesterReq = canParser.CanRegisterReq.can_reset_flag;
                        txQueue.Add("G00");     // reg addr = 0
                    }

                    req_err_cnt_step += 1;
                    if (req_err_cnt_step > req_err_cnt_step_max)
                        req_err_cnt_step = 0;
                }

                // txQueue.Add("F");       // erorrs
            } else
            {
                m_parser.RawRegesterReq = canParser.CanRegisterReq.none;
                req_err_cnt_step = 0;
            }
        }

        public canParserErrors errorsGet()
        {
            return m_parser.ErrorCounterGet();
        }

        public void errorsClean()
        {
            m_parser.ErrorCounterClean();
        }

        // get selected port name
        public string getPortName()
        {
            return m_comName;
        }

        // get selected speed
        public string getCanSpeed()
        {
            return m_canSpeed;
        }

        // get firmware version
        public string getFwVersion()
        {
            return m_fwVer;
        }
        #endregion

        // send a message
        public bool sendMessge(canMessage2 msg, bool rtr = false)
        {
            // it is not allowed to send messages in the listen only mode
            if (m_silentMode)
                return false;

            // convert message to raw data string
            string s = canParser.parseCanMessageForSend(msg, rtr);
            if (txQueue.Count > 100)
                return false;

            txQueue.Add(s);
            return true;
        }


        public void Reset()
        {
            if (isPortOpen())
            {
                // the port is open, set the flag
                reset_requested = true;
            } else
            {
                clearRxer();
                reset_requested = false;
            }
        }


        public bool setComPort(string name)
        {
            if (string.IsNullOrEmpty(name))
                return false;

            m_comName = name;
            return true;
        }

        public bool setCanSpeed(string speed)
        {
            if (string.IsNullOrEmpty(speed))
                return false;

            m_canSpeed = speed;
            m_autoSpeed = canSpeedList.isAutoSpeed(speed);

            return true;
        }

        public bool setSilentMode (bool enable)
        {
            m_silentMode = enable;
            return true;
        }

        public bool isListenOnlyModeEnabled()
        {
            return m_silentMode;
        }

        public bool isPortOpen()
        {
            return m_port.isOpen();
        }

        private void writeCmd(char cmd)
        {
            m_port.write((byte)cmd);
            m_port.write(0x0D);
        }

        private void writeCmd(string cmd)
        {
            m_port.write(cmd);
            m_port.write((byte)'\r');
        }

        private bool writeCmdGetResponse(string cmd)
        {
            byte[] b = new byte[256];
            return writeCmdGetResponse(cmd, ref b);
        }

        private bool writeCmdGetResponse(string cmd, ref byte[] resp)
        {
            List<byte> res = new List<byte>();
            bool success = false;

            for (int tries = 0; tries < 3 && !success; tries++)
            {
                bool done = false;
                writeCmd(cmd);
                const int tmoStep = 50;
                for (int i = 1000; i > 0 && !done; i -= tmoStep)
                {
                    Thread.Sleep(tmoStep);
                    byte[] rcv = m_port.readAll();
                    foreach (byte b in rcv)
                    {
                        if (b == '\r')
                            success = true;
                        done |= b == '\r' || b == '\a';

                        res.Add(b);
                    }
                }

                if (!done)
                    res.Clear();
            }

            resp = res.ToArray();

            return success;
        }


        private void checkFixTimestamp()
        {
            if (m_parser.TimestampError)
            {
                bool res = writeCmdGetResponse("Z");    // toggle
                Thread.Sleep(500);                      // wait for stability
                m_port.clearReceiver();                 // extract data from the port
                m_parser.TimestampError = false;
            }
        }

        private string canSpeedDetect()
        {
            string res = String.Empty;
            canMessage2 msg;

            // get all the speeds
            List<string> speedList = canSpeedList.getSpeedListSorted();

            // stop (just in case)
            writeCmdGetResponse("C");

            foreach (string speed in speedList)
            {
                // set the speed
                bool speedIsSet = false;

                for (int i = 0; i < 3; i++)
                {
                    speedIsSet = writeCmdGetResponse(canSpeedList.getCmd(speed));
                    if (speedIsSet)
                        break;
                    // repeat
                    Thread.Sleep(100);
                }

                // clear buffers              
                m_dataToParse.Clear();

                while (m_queueMsgs.TryTake(out msg)) ;
                m_port.clearReceiver();

                // start (listen only)
                bool started = writeCmdGetResponse("L");
                //bool ans = writeCmdGetResponse("F");
                //writeCmdGetResponse("F");

                int tmo = 1000; //

                const int tmoStep = 100;

                while (tmo > 0 && 0 == m_queueMsgs.Count)
                {
                    Thread.Sleep(tmoStep);              // wait

                    // try to get some data 
                    byte[] rcv = m_port.readAll();
                    if (rcv.Length > 0)
                        for (int i = 0; i < rcv.Length; i++)
                            m_dataToParse.Add(rcv[i]);

                    // try to parse them
                    if (m_dataToParse.Count > 0)
                        m_parser.parseBufferFast(m_dataToParse, m_queueMsgs, 5);

                    // check for timestamp error
                    checkFixTimestamp();

                    // tmo
                    tmo -= tmoStep;
                }

                // stop
                bool stopped = writeCmdGetResponse("C");

                //Thread.Sleep(1000);

                // are there any messages?
                if (m_queueMsgs.Count > 0)
                {
                    res = speed;
                    break;
                }


            }

            return res;
        }


        #region ftdi_driver_config

        readonly int m_expected_latency = 1;
        private bool is_latency_ok = false;
        private int ftdi_latency = 0;

        public bool isLatencyCorrect()
        {
            if (comPortEnumerator.getFTDIComPortLatency(m_comName, ref ftdi_latency))
                return ftdi_latency <= m_expected_latency;
            return false;
        }

        public bool ftdiLatencyUpdate()
        {
            return comPortEnumerator.updateFTDIComPortLatency(m_comName, m_expected_latency);
        }

        #endregion

        public bool start()
        {
            is_latency_ok = false;
            m_port.close();
            m_port.open(m_comName, m_comBaud);

            m_parser.CanFilter = CanFilter;

            m_fwVer = string.Empty;

            if (m_port.isOpen())
            {
                // reset the buffs (todo: create a new class later)
                canMessage2 msg;
                while (m_queueMsgs.TryTake(out msg)) ;

                bool error = false;

                // stop (do not check the res)
                writeCmdGetResponse("C");

                byte[] buff = new byte[128];
                
                // get version
                if (!error)
                    error |= !writeCmdGetResponse("v", ref buff);
                m_fwVer = m_parser.parseVersion(ref buff);

                // sn
                if (!error)
                    error |= !writeCmdGetResponse("N", ref buff);
                string sSn = m_parser.parseSerial(ref buff);


                // detect speed
                if (m_autoSpeed && !error)
                {
                    m_canSpeed = String.Empty;          // reset
                    string speed = canSpeedDetect();    // try to find it
                    Thread.Sleep(1000);
                    if (!string.IsNullOrEmpty(speed))
                    {       // success?              
                        m_canSpeed = speed;

                    }
                }

                // set speed
                string speedCmd = canSpeedList.getCmd(m_canSpeed);
                error |= string.IsNullOrEmpty(speedCmd);
                if (!error)
                    error |= !writeCmdGetResponse(speedCmd);

                // start
                if (!error)
                {
                    if (m_silentMode)
                        error |= !writeCmdGetResponse("L");
                    else
                        error |= !writeCmdGetResponse("O");
                }     

                if (error)
                {
                    // close the port and return
                    writeCmdGetResponse("C");
                    m_port.close();
                    return false;
                }

                // get latency config
                is_latency_ok = isLatencyCorrect();

                // reset the tx token
                if (txTokenSource.Token.IsCancellationRequested)
                {
                    txTokenSource.Dispose();
                    txTokenSource = new CancellationTokenSource();
                }

                // rx
                m_worker = new Thread(worker);
                m_worker.Name = "canWorker RX";
                m_worker.Start();

                // tx
                txWorker = new Thread(onTxWorker);
                txWorker.Name = "canWorker TX";
                txWorker.Start();

                return true;
            }

            return false;
        }

        public void stop()
        {
            // stop the thread
            rxTokenSource.Cancel();
            rxStopFlag = true;
            txTokenSource.Cancel();
            txStopFlag = true;

            int timeout = 3000;

            // wait
            while (m_worker != null && m_worker.IsAlive && txWorker != null && txWorker.IsAlive)
            {
                Thread.Sleep(100);
                timeout -= 100;
                if (timeout <= 0)
                    break;
            }

            if (m_port.isOpen())
            {
                writeCmdGetResponse("C");
                m_port.clearReceiver();
            }
            // close
            m_port.close();
        }


        private void onTxWorker()
        {
            txToken = txTokenSource.Token;
            txStopFlag = false;

            // waiting for data
            while (m_port.isOpen() == true && !txStopFlag)
            {
                string s = null;

                // get
                try
                {
                    s = txQueue.Take(txToken);
                }
                catch { }

                if (txToken.IsCancellationRequested)
                    txStopFlag = true;

                // send
                if (s != null && !txStopFlag && m_port.isOpen())
                {
                    m_parser.sent = false;
                    // send
                    this.writeCmd(s);
                    // wait
                    Thread.Sleep(1);
                }
            }

            // reset the token
            txTokenSource.Dispose();
            txTokenSource = new CancellationTokenSource();
        }

        private bool rxStopFlag = false;
        private bool txStopFlag = false;

        private void clearRxer()
        {
            // clean the queue, the message counter and the parser
            canMessage2 msg = new canMessage2();
            while (true == m_queueMsgs.TryTake(out msg)) { }
            MessageCounter = 0;
            m_parser.reset();
        }

        private void worker()
        {
            // clear the message queue
            canMessage2 msg = new canMessage2();
            while (m_queueMsgs.TryTake(out msg)) ;

            // clear the receiver
            m_port.clearReceiver();

            rxStopFlag = false;
            rxToken = rxTokenSource.Token;

            // rx loop
            while (m_port.isOpen() == true && !rxStopFlag)
            {
                // read
                byte[] ls = m_port.readAll();

                // handle
                if (ls.Length > 0)
                {
                    // copy
                    m_dataToParse.AddRange(ls);
                    // parse
                    m_parser.parseBufferFast(m_dataToParse, m_queueMsgs);
                    // timestamp
                    checkFixTimestamp();
                    // counter
                    MessageCounter += (ulong)m_queueMsgs.Count;
                }
                
                // reset
                if (reset_requested)
                {
                    clearRxer();
                    reset_requested = false;
                    // wait (do we really need to wait here?
                    m_sleep_handle.WaitOne(get_interval_msec_default());
                    continue;
                }

                // report
                if (m_queueMsgs.Count > 0)
                    waitHandle.Set();

                // adaptive sleep
                int sleepMs = get_interval_msec_default();
                if (m_high_performance_enabled)
                {
                    if (ls.Length <= 20)
                        sleepMs = 3;
                    else if (ls.Length <= 50)
                        sleepMs = 15;
                    else
                        sleepMs = 20;

                    //Debug.WriteLine("sleep {0}", sleepMs);
                }

                //long sleep_start_ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();


                m_sleep_handle.WaitOne(sleepMs);

                /*
                long sleep_stop_ms = DateTimeOffset.Now.ToUnixTimeMilliseconds();

                long diff = sleep_stop_ms - sleep_start_ms;
                if (diff < sleepMs)
                    Debug.WriteLine("sleep {0}", diff);
                */
            }

            // reset the token
            rxTokenSource.Dispose();
            rxTokenSource = new CancellationTokenSource();
        }

        public int get_interval_msec_default()
        {
            return 250;
        }

        public void set_pause(bool enable)
        {
            paused = enable;
        }

        /*
        public void testPush(byte[] buff)
        {
            foreach (byte item in buff)
            {
                m_dataToParse.Add(item);
            }
        }
        */
    }


}
