using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Text.RegularExpressions;
using Microsoft.Win32;
using System.Threading;
using System.Text;
using System.Security.Principal;


// receiced data buff -> parse

namespace canSerialPort
{
    // utils 1: manufacturer data container
    public class serialPortManufacturer
    {
        public serialPortManufacturer(string vid, string pid)
        {
            VID = vid;
            PID = pid;
        }

        public string VID { get; set; }
        public string PID { get; set; }
    }

    // serial port infor container
    public class serialPortInfo
    {
        public string portName { set; get; }
    }

    // enumerator class
    public class comPortEnumerator
    {
        // is the application running in administrator mode?
        private static bool IsAdministrator()
        {
            var identity = WindowsIdentity.GetCurrent();
            var principal = new WindowsPrincipal(identity);
            return principal.IsInRole(WindowsBuiltInRole.Administrator);
        }

        // get a current FTDI driver latency value
        public static bool getFTDIComPortLatency(string portName, ref int latency)
        {
            const string sFtdiVid = "0403"; // ftdi vid
            const string sFtdiPid = "6001"; // ftdi pid

            String pattern = String.Format("^VID_{0}.PID_{1}", sFtdiVid, sFtdiPid);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
            foreach (String s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");

                            string cur_portName = (string)rk6.GetValue("PortName");
                            if (cur_portName == portName)
                            {
                                latency = (int)rk6.GetValue("LatencyTimer");
                                return true;
                            }
                        }
                    }
                }
            }

            latency = 0;
            return false;
        }

        // update the FTDI driver latency
        public static bool updateFTDIComPortLatency(string portName, int latency)
        {
            const string sFtdiVid = "0403"; // ftdi vid
            const string sFtdiPid = "6001"; // ftdi pid
            bool res = false;

            if (!IsAdministrator())
                return false;

            String pattern = String.Format("^VID_{0}.PID_{1}", sFtdiVid, sFtdiPid);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
            foreach (String s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters", true);

                            string cur_portName = (string)rk6.GetValue("PortName");
                            if (cur_portName == portName)
                            {
                                int cur_latency = (int)rk6.GetValue("LatencyTimer");
                                // update
                                if (cur_latency > latency)
                                {
                                    rk6.SetValue("LatencyTimer", latency);
                                    res = true;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            return res;
        }

        // get list of existing ports with the vid and pid values we need
        private static List<string> getSystemComPortNames(String VID, String PID)
        {
            String pattern = String.Format("^VID_{0}.PID_{1}", VID, PID);
            Regex _rx = new Regex(pattern, RegexOptions.IgnoreCase);
            List<string> comports = new List<string>();
            RegistryKey rk1 = Registry.LocalMachine;
            RegistryKey rk2 = rk1.OpenSubKey("SYSTEM\\CurrentControlSet\\Enum");
            foreach (String s3 in rk2.GetSubKeyNames())
            {
                RegistryKey rk3 = rk2.OpenSubKey(s3);
                foreach (String s in rk3.GetSubKeyNames())
                {
                    if (_rx.Match(s).Success)
                    {
                        RegistryKey rk4 = rk3.OpenSubKey(s);
                        foreach (String s2 in rk4.GetSubKeyNames())
                        {
                            RegistryKey rk5 = rk4.OpenSubKey(s2);
                            RegistryKey rk6 = rk5.OpenSubKey("Device Parameters");
                            comports.Add((string)rk6.GetValue("PortName"));

                            //int tmr = (int)rk6.GetValue("LatencyTimer");
                        }
                    }
                }
            }

            return comports;
        }

        // get list of the available ports
        public static List<string> getAvailablePortNames()
        {
            List<string> comports = new List<string>();

            //show list of valid com ports
            foreach (string s in SerialPort.GetPortNames())
                comports.Add(s);
            
            return comports;
        }

        // check is the port available
        public static bool isPortAvailable (string port)
        {
            List<string> avail = getAvailablePortNames();
            return avail.Contains(port);
        }

        // get avail port names
        public static List<string> getPortNames(serialPortManufacturer info)
        {
            // avail ports
            List<string> available = getAvailablePortNames();
            // system ports with the appropriate vid and pid values
            List<string> system = getSystemComPortNames(info.VID, info.PID);

            // compare 2 lists
            List<string> comports = new List<string>();
            foreach (string port in system)
                if (available.Contains(port) && !comports.Contains(port) )
                    comports.Add(port);

            return comports;
        }
    }

    // com port class
    public class comPort
    {
        private SerialPort m_port;                  // serial port
        private Mutex m_mutex;                      // mutex
        // constructor
        public comPort()
        {
            m_mutex = new Mutex();                  // create the mutex
        }

        // open the port
        public bool open(string name, int baudrate)
        {
            m_mutex.WaitOne();      // waint until the mutex is released

            if (isOpen())
                m_port.Close();

            // check port name
            if (comPortEnumerator.isPortAvailable(name)) {
                // create
                m_port = new SerialPort(name, baudrate, Parity.None, 8, StopBits.One);
                m_port.ReadBufferSize = 500000; // 4096 is default
                m_port.NewLine = "\r";

                // open
                try
                {                   
                    m_port.Open();
                }
                catch { }
            }

            m_mutex.ReleaseMutex();
            return isOpen();
        }

        // events take too much CPU resourses
        /*
        public void startEvent()
        {
           // m_port.ReceivedBytesThreshold = 1000;
           // m_port.DataReceived += new SerialDataReceivedEventHandler(EvtOnDataReceived);
        }

        public void stopEvent()
        {
            //m_port.DataReceived -= new SerialDataReceivedEventHandler(EvtOnDataReceived);
        }
        */
        /*
        private void EvtOnDataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            try
            {
                //while (m_port.IsOpen && m_port.BytesToRead > 0)
                //    queue.Add((byte)m_port.ReadByte());
                if( m_port.IsOpen )
                {
                    byte[] b = new byte[m_port.BytesToRead];
                    m_port.Read(b, 0, b.Length);
                    foreach (byte bb in b)
                        queue.Add(bb);

                    //queue.CompleteAdding();
                   
                }
            }
            catch { }
        }
        */

        // close the port
        public bool close()
        {
            // exists?
            if (null == m_port )
                return true;
            if (!m_port.IsOpen )
                return true;

            // close
            m_mutex.WaitOne();
            m_port.Close();
            m_mutex.ReleaseMutex();

            return !isOpen();
        }

        private bool prevOpenState = false;
        // is open
        public bool isOpen()
        {
            bool res = m_port != null && m_port.IsOpen;
            if (res != prevOpenState && res == false)
                close();
            prevOpenState = res;
            return res;
        }

        // write string
        public void write(string str)
        {
            byte[] buff = Encoding.ASCII.GetBytes(str);
            this.write(buff);
        }
        // write byte
        public void write(byte b)
        {
            byte[] buff = new byte[]{b};
            this.write(buff);
        }
        // write buff
        public void write(byte[] buff)
        {
            m_mutex.WaitOne();
            if (isOpen())
                m_port.Write(buff, 0, buff.Length);
            m_mutex.ReleaseMutex();
        }

        public byte[] readLine(out bool timeout)
        {
            //m_mutex.WaitOne();

            byte[] rcv = new byte[0];

            timeout = false;
            string s = string.Empty;
            bool gotOne = false;

            m_port.DtrEnable = true;
          
            // read all the bytes
            while ( isOpen() && !timeout )
            {
                try
                {
                    s += m_port.ReadLine() + "\r";
                    
                }
                catch (TimeoutException) {
                    timeout = true;
                }

                if( !gotOne )
                {
                    m_port.ReadTimeout = 10;
                    gotOne = true;
                }
                
            }

            m_port.ReadTimeout = -1;
            //m_mutex.ReleaseMutex();
            rcv = Encoding.ASCII.GetBytes(s);

            return rcv;
        }

        // read
        public byte[] readAll()
        {
            m_mutex.WaitOne();
       
            byte[] rcv = new byte[0];

            // read all the bytes
            if (isOpen())
            {
                try
                {
                    string s = m_port.ReadExisting();
                    rcv = Encoding.ASCII.GetBytes(s);
                } catch(UnauthorizedAccessException)
                {
                    close();
                }
            }
            
            m_mutex.ReleaseMutex();
                        
            return rcv;
        }

        // clear
        public void clearReceiver ()
        {
            m_mutex.WaitOne();
            m_port.ReadExisting();
            m_mutex.ReleaseMutex();
        }
    }
}





/* This AX-Fast Serial Library
   Developer: Ahmed Mubarak - RoofMan
 
   This Library Provide The Fastest & Efficient Serial Communication
   Over The Standard C# Serial Component
*/
 
