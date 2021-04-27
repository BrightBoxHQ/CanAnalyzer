using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace canAnalyzer
{
    public class canMessageList
    {
        List<canMessage> m_list;

        public canMessageList()
        {
            m_list = new List<canMessage>();
        }

        public void clear()
        {
            m_list.Clear();
        }

        public canMessage get(canMessage msg)
        {
            canMessage res = find(msg);
            return res;
        }

        public void add(List<canMessage> ls)
        {
            foreach (canMessage m in ls)
                add(m);
        }

        public void add(canMessage msg)
        {
            int idx = findIndex(msg);

            if (idx >= 0)
            {
                canMessage prev = m_list[idx];
                int per = canMessage.getPeriod(msg.getTimestamp(), prev.getTimestamp());
                msg.period = per;
                m_list[idx] = msg;
            }
            else
                m_list.Add(msg);

            return;
        }

        public void remove(canMessage msg)
        {
            canMessage prev = find(msg);
            if (!prev.isEmpty())
                m_list.Remove(prev);
        }

        private int findIndex (canMessage msg)
        {
            return m_list.FindIndex(x => x.Id == msg.Id && x.getDlc() == msg.getDlc());
        }

        private canMessage find(canMessage msg)
        {
            int idx = findIndex(msg);

            if (idx >= 0)
                return m_list[idx];
            return null;
        }

        public bool isEmpty()
        {
            return Count() == 0;
        }

        public int Count()
        {
            return m_list.Count;
        }

        public List<canMessage> getList()
        {
            return m_list;
        }
    }
    // end of the class
    //-----------------------------------------------------------------------

    public class canMessageId
    {
        // id value
        // 0 - 0x7FF (11 bit)
        // 0 - 0x‭1FFFFFFF‬ (29bit)
        public int Id { get; } 

        // dlc value
        // 0 - 8
        public int Dlc { get; }

        // is 29 bit id
        // true - 29 bit
        // false - 11 bit
        public bool Is29bit { get; }

        // internal hash value
        private readonly long unique_hash;

        // constructor
        public canMessageId ()
        {
            Is29bit = false;
            Dlc = 0;
            Id = 0;
            unique_hash = calculateHash();
        }

        // constructor
        public canMessageId (int id, int dlc, bool is29b = false)
        {
            // 29bit flag
            Is29bit = is29b;        
            
            // dlc
            if( dlc >= 0 && dlc <= MaxDlc )
                Dlc = dlc;
            else
            {
                string msg = string.Format(
                    "DLC value cannot be {0} cuz max possible value is {1} and min possible value is {2}",
                    dlc, MaxDlc, 0);
                throw new System.ArgumentException(msg, "message dlc");
            }

            // id
            if ( id >= 0 && id <= GetMaxId() )
            {
                Id = id;
            }
            else
            {
                string msg = string.Format(
                    "Id value cannot be {0} cuz max possible value is {1} and min possible value is {2}",
                    id, GetMaxId(), 0);
                throw new System.ArgumentException(msg, "message id");

            }

            // hash
            unique_hash = calculateHash();
        }

        // constructor
        public canMessageId(string sId, int dlc)
        {
            // dlc
            if (dlc >= 0 && dlc <= MaxDlc)
                Dlc = dlc;
            else
            {
                string msg = string.Format(
                    "DLC value cannot be {0} cuz max possible value is {1} and min possible value is {2}",
                    dlc, MaxDlc, 0);
                throw new System.ArgumentException(msg, "message dlc");
            }

            // id
            if( sId.Length != 3 && sId.Length != 8 )
            {
                string msg = string.Format(
                    "Id value ( {0} ) format mismatch",
                    sId);
                throw new System.ArgumentException(msg, "message id");
            }

            Is29bit = sId.Length > 3;

            int id = Convert.ToInt32(sId, 16);
            if (id >= 0 && id <= GetMaxId())
                Id = id;
            else
            {
                string msg = string.Format(
                    "Id value cannot be {0} cuz max possible value is {1} and min possible value is {2}",
                    id, GetMaxId(), 0);
                throw new System.ArgumentException(msg, "message id");
            }

            // hash
            unique_hash = calculateHash();

        }

        // a very simple alghoritm to get an unique fields-based 'hash' value
        private long calculateHash()
        {
            // CAN ID - either 11 or 29 bit
            ulong hash = (ulong)Id;

            // is 29 bit ID?
            if (Is29bit)
                hash |= (ulong)1 << 29;

            // dlc - 4 bits
            hash |= (ulong)(Dlc & 0x0F) << 30;

            return (long)hash;
        }


        // max dlc value
        static public readonly int MaxDlc = 8;
        // max id value
        static public readonly int MaxId11bit = 0x7FF;
        static public readonly int MaxId29bit = 0x1FFFFFFF;

        // returns a max possible id value
        static public int GetMaxId (bool is29bit)
        {
            return is29bit ? MaxId29bit : MaxId11bit;
        }
        // returns a max possible id value
        public int GetMaxId()
        {
            return GetMaxId(Is29bit);
        }

        // id to string
        static public string GetIdAsString(int id, bool is29bit = false)
        {
            return id.ToString(is29bit ? "X8" : "X3");
        }
        // id to string
        public string GetIdAsString()
        {
            return GetIdAsString(Id, Is29bit);
        }

        // dlc to string
        static public string GetDlcAsString(int dlc)
        {    
            return dlc.ToString();
        }

        public string GetDlcAsString()
        {
            return GetDlcAsString(Dlc);
        }

        // equals
        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            canMessageId p = (canMessageId)obj;
            return p.GetHashCodeUnique() == GetHashCodeUnique();
        }

        public override int GetHashCode()
        {
            return (int)GetHashCodeUnique();
        }

        // get hash code
        public long GetHashCodeUnique()
        {
            return unique_hash;
        }

        // returns an empty message
        static public canMessageId Empty { get { return new canMessageId(0, 0, false); } }

        // returns true if Id is null or empty
        static bool IsNullOrEmpry(canMessageId id)
        {
            return null == id || id.Equals(Empty);
        }
    }

    public class canTimeStamp
    {
        public int TimeStamp { get; }

        public static readonly int MaxTimeStamp = 59999;

        public canTimeStamp(int ts = 0)
        {
            TimeStamp = ts;
        }
    }


    public class canPeriod
    {
        public int MilliSec
        {
            get
            {
                return (count >= 2) ? 
                    (int)Math.Round( ((double)summ) / ((double)(count-1)) ) : 0;
            }
        }

        // counter and summ
        private long summ;
        private int count;

        private canTimeStamp LastTimeStamp;

        // constructor
        public canPeriod()
        {
            Clear();
        }

        // contructor
        public canPeriod(canTimeStamp ts)
        {
            Clear();
            Update(ts);
        }

        // update
        public void Update(canTimeStamp ts)
        {
            if (count != 0)
            {
                int tmp = Calculate(ts, LastTimeStamp);//, ts);

                // check
                if( count > 2)
                {
                    int cur = MilliSec;
                    if (cur > 20 || tmp > 20)
                    {
                        int diff = Math.Abs(tmp - cur);
                        double diffPrcnt = (double)diff / (double)cur;
                        double maxDiff = 0.2;
                        if (diffPrcnt > maxDiff) // 20%
                        {
                            count = 0;
                            summ =  0;
                            return;
                        }
                    }
                }

                summ += tmp;
            }

            LastTimeStamp = ts;
            count++;
        }

        // calculate
        public static int Calculate(canTimeStamp ts1, canTimeStamp ts2)
        {
            int ts = ts1.TimeStamp - ts2.TimeStamp;
            if (ts < 0)
                ts += canTimeStamp.MaxTimeStamp;
            return ts;
        }

        // clear
        public void Clear()
        {
            count = 0;
            summ = 0;
            LastTimeStamp = null;
        }
    }

    public class canMessage2
    {
        // id class
        public canMessageId Id { get; }
        // data buffer
        public byte[] Data { get; }
        // timestamp value
        public canTimeStamp TimeStamp { get; }

        public int timestamp_absolute = 0;

       // public long unix_ts = 0;

        // constructor
        public canMessage2()
        {
            Id = canMessageId.Empty;
            Data = null;
            TimeStamp = new canTimeStamp(0);
        }
        // constructor
        public canMessage2(int id = 0, bool is29bit = false, byte[] data = null, int ts = 0)
        {
            // data
            if (data != null && data.Length > 0)
            {
                Data = new byte[data.Length];
                data.CopyTo(Data, 0);
            } else
            {
                Data = null;
            }
            // id
            Id = new canMessageId(id, Data == null ? 0 : data.Length, is29bit);
            // timestamp
            TimeStamp = new canTimeStamp(ts);
        }
        // constructor
        public canMessage2(canMessageId id, byte[] data = null, int ts = 0)
        {
            Id = id;
            // data
            if (data != null && data.Length > 0)
            {
                Data = new byte[data.Length];
                data.CopyTo(Data, 0);
            }
            else
            {
                Data = null;
            }
            // todo: compare data len and DLC

            // ts
            TimeStamp = new canTimeStamp(ts);
        }
        // constructor
        public canMessage2(int id, bool is_29bit, byte b0, byte b1, byte b2, byte b3, byte b4, byte b5, byte b6, byte b7)
        {
            Data = new byte[8];
            Data[0] = b0;
            Data[1] = b1;
            Data[2] = b2;
            Data[3] = b3;
            Data[4] = b4;
            Data[5] = b5;
            Data[6] = b6;
            Data[7] = b7;
            Id = new canMessageId(id, Data.Length, is_29bit);
            TimeStamp = new canTimeStamp(0);
        }

        // data string
        public List<string> GetDataStringList()
        {
            List<string> res = new List<string>();
            for (int i = 0; i < Id.Dlc; i++)
                res.Add(Data[i].ToString("X2"));
            return res;
        }

        public string GetDataString(string dataSeparator = null, string dataPrefix = null)
        {
            StringBuilder sb = new StringBuilder();

            if (Data == null || Data.Length == 0)
                return string.Empty;

            int last_separator_idx = Data.Length - 1;

            for (int i = 0; i < Data.Length; i++)
            {
                // prefix
                if (!string.IsNullOrEmpty(dataPrefix))
                    sb.Append(dataPrefix);
                // data
                sb.Append(Data[i].ToString("X2"));
                // separator
                if (!string.IsNullOrEmpty(dataSeparator) && i != last_separator_idx)
                    sb.Append(dataSeparator);
            }

            return sb.ToString();
        }

        /* // obsolete
        // data string
        public string GetDataString(string dataSeparator = null, string dataPrefix = null)
        {
            List<string> ls = GetDataStringList();
            string res = String.Empty;

            for (int i = 0; i < ls.Count; i++)
            {
                // prefix
                if( !string.IsNullOrEmpty(dataPrefix) )
                    res += dataPrefix;
                // data
                res += ls[i];
                // separator
                if ( !string.IsNullOrEmpty(dataSeparator) && i != ls.Count - 1 )
                    res += dataSeparator;
            }

            return res;
        }
        */

        public byte GetDatByteFromString(string sByte)
        {
            return Convert.ToByte(sByte, 16);
        }

        public int GetIdFromString(string sId)
        {
            return Convert.ToInt32(sId, 16);
        }

        public long GetHashCodeUnique()
        {
            return Id.GetHashCodeUnique();
        }

        public override bool Equals(object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            canMessage2 p = (canMessage2)obj;
            return p.Id.Equals(Id);
        }

        public override int GetHashCode()
        {
            return (int)(GetHashCodeUnique());
        }


        // returns true if Id is null or empty
        static public bool IsNullOrEmpry(canMessage2 msg)
        {
            return null == msg || msg.Id == canMessageId.Empty || msg.Data == null;
        }
    }
    

    public class canMessageList2
    {
        private Dictionary<long, canMessage2> list;

        public canMessageList2()
        {
            list = new Dictionary<long, canMessage2>();
        }

        public List<canMessage2> ToList()
        {
            return list.Values.ToList();
        }

        public void add (canMessage2 msg)
        {
            long key = msg.GetHashCodeUnique();
            
            if ( list.ContainsKey(key) )
                list[key] = msg;        // update
            else
                list.Add(key, msg);     // add
        }

        public void add (List<canMessage2> ls)
        {
            foreach (var msg in ls)
                add(msg);
        }

        public bool isEmpty()
        {
            return list.Count == 0;
        }

        public void clear()
        {
            list.Clear();
        }
    }


    // message
    public class canMessage
    {

        // constructor
        public canMessage(int id = 0, byte[] data = null, int ts = 0, bool is29bit = false)
        {
            Id = id;
            m_data = null;
            m_dlc = 0;
            period = 0;
            mask = new byte[8] { 0, 0, 0, 0, 0, 0, 0, 0 };
            bytesChanged = new bool[8];

            if (null != data && data.Length >= 1 && data.Length <= 8)
            {
                m_data = data;
                m_dlc = data.Length;
                
                for (int i = 0; i < data.Length; i++)
                {
                    mask[i] |= data[i];     // mask
                }         
            }

            m_29bit = is29bit;
            m_ts = ts;  
        }

        // check is empty
        public bool isEmpty()
        {
            return 0 == Id || 0 == m_dlc;
        }

        /// access
        /// 

        // id
        public int getCanId()
        {
            return Id;
        }

        // id 
        public string getCanIdString()
        {
            return getCanIdString(getCanId(), m_29bit);
        }

        static public string getCanIdString(int id, bool is29bit)
        {
            return id.ToString(is29bit ? "X8" : "X3");
        }

        // data
        public List<string> getDataStringList()
        {
            List<string> res = new List<string>();
            
            if (getDlc() > 0)
                for (int i = 0; i < getDlc(); i++) 
                    res.Add(getData()[i].ToString("X2"));

            return res;
        }

        // data
        public byte[] getData()
        {
            return m_data;
        }

        public string getDataString(bool separate = false, string separator = " ")
        {
            List<string> ls = getDataStringList();

            string res = String.Empty;

            for (int i = 0; i < ls.Count; i++)
            {
                res += ls[i];
                if (separate && i != ls.Count - 1)
                    res += separator;
            }

            return res;
        }

        public int getTimestamp()
        {
            return m_ts;
        }


        // DLC
        //
        public int getDlc()
        {
            return m_dlc;
        }

        public string getDlcAsString()
        {
            return getDlc().ToString();
        }

        // utils
        // todo: remove
        public static int getPeriod(canMessage m1, canMessage m2)
        {
            int ts1 = m1.getTimestamp();
            int ts2 = m2.getTimestamp();

            return getPeriod(ts1, ts2);
        }

        public static int getPeriod(int ts1, int ts2)
        {
            int ts = ts1 - ts2;
            if (ts < 0)
                ts += 59999;
            return ts;
        }

        public bool is29bitId ()
        {
            return m_29bit;
        }

        static public int idMax (bool is29bit)
        {
            return is29bit ? 0x1FFFFFFF : 0x7FF;
        }

        public int idMax()
        {
            return idMax(is29bitId());
        }

        static public int maxDataBytesNum ()
        {
            return 8;   // 8 bytes
        }

        private bool m_29bit;               // 29 bit id
        public int Id { get; set; }         // message id
        private int m_dlc;                  // data len
        private byte[] m_data;              // data
        private int m_ts;                   // timestamp (ms)
        public int period { get; set; }
        public byte[] mask { get; set; }    // mask of all the prev bytes
        public bool[] bytesChanged { get; set; }


        public override bool Equals(Object obj)
        {
            // Check for null values and compare run-time types.
            if (obj == null || GetType() != obj.GetType())
                return false;

            canMessage p = (canMessage)obj;
            return getCanId() == p.getCanId() &&    // id
                is29bitId() == p.is29bitId() &&     // id len
                getDlc() == p.getDlc() &&           // dlc
                getData() == p.getData();           // data
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
    // end of the class
    //-----------------------------------------------------------------------

}
