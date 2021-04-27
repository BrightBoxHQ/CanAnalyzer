using System;
using System.Collections.Generic;
using System.Windows.Forms;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Xml.Serialization;
using System.IO;
using System.Text.RegularExpressions;
using NCalc;
using System.Data;
using System.Text;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO.Compression;
using System.Security.Cryptography;

namespace appStyle
{
    static class font
    {
        public static System.Drawing.Font headerFontBack()
        {
            return new System.Drawing.Font("Calibri", 9.0f, System.Drawing.FontStyle.Bold);
        }

        public static System.Drawing.Color headerColor()
        {
            return System.Drawing.Color.White;
        }
    }
}


namespace Remoto
{
    class deviceTemplate
    {
        public readonly string Name;
        public readonly string TemplateSend;
        public readonly string TemplateDelay;

        public deviceTemplate(string name, string templateSend, string templateDelay)
        {
            Name = name;
            TemplateSend = templateSend;
            TemplateDelay = templateDelay;
        }

        static public deviceTemplate Empty { get { return new deviceTemplate(string.Empty, string.Empty, string.Empty); } }

        static public bool IsEmpty (deviceTemplate item)
        {
            return string.IsNullOrEmpty(item.Name) || string.IsNullOrEmpty(item.TemplateSend);
        }

        public bool IsEmpty()
        {
            return IsEmpty(this);
        }
    }

    class deviceTemplateList
    {
        public List<deviceTemplate> list;

        public deviceTemplateList ()
        {
            list = new List<deviceTemplate>();
        }

        public void Add (deviceTemplate item)
        {
            list.Add(item);
        }

        public bool Contains (string device)
        {
            foreach (var i in list)
                if (i.Name == device)
                    return true;
            return false;
        }

        public List<string> GetNames()
        {
            List<string> ls = new List<string>();

            foreach (var item in list)
                ls.Add(item.Name);

            return ls;
        }

        public void Clear ()
        {
            list.Clear();
        }

        public deviceTemplate Get (string device)
        {
            foreach (var item in list)
                if (item.Name == device)
                    return item;

            return deviceTemplate.Empty;
        }

    }

    class xlsDevices
    {
        static public deviceTemplateList getDeviceList()
        {
            deviceTemplateList devices = new deviceTemplateList();
            /*
            XmlDocument xDoc = new XmlDocument();
            xDoc.Load("devices.xml");

            XmlElement xRoot = xDoc.DocumentElement;

            foreach (XmlNode xnode in xRoot)
            {
                string name = string.Empty;
                string templateSend = string.Empty;
                string templateDelay = string.Empty;

                if (xnode.Attributes.Count > 0)
                {
                    XmlNode attr = xnode.Attributes.GetNamedItem("name");
                    if (null == attr)
                        break;
                    name = attr.Value;
                }
                foreach (XmlNode childnode in xnode.ChildNodes)
                {
                    if (childnode.Name == "send")
                        templateSend = childnode.InnerText;
                    if (childnode.Name == "delay")
                        templateDelay = childnode.InnerText;
                }

                // add
                if( !string.IsNullOrEmpty(name) && !string.IsNullOrEmpty(templateSend) )
                {
                    deviceTemplate item = new deviceTemplate(name, templateSend, templateDelay);
                    devices.Add(item);
                }
            }
            */
            return devices;
        }
    }
}




namespace canAnalyzer
{

    #region speed 
    public class canSpeed
     {
        canSpeed(string str, string cmd)
        {
            asString = str;
            asCmd = cmd;
        }

        public string asString { get; set; }
        public string asCmd { get; set;  }
     }
    #endregion

    #region speed list
    public static class canSpeedList
    {
        public static List<string> getSpeedList ()
        {
            return m_speed;
        }

        public static List<string> getSpeedListSorted ()
        {
            return m_speedPriority;
        }

        public static string getCmd (string speed)
        {
            string res = string.Empty;           
            if (m_speed.Contains(speed))
                res = m_cmd[m_speed.IndexOf(speed)];
            return res;
        }

        public static bool isAutoSpeed (string speed)
        {
            return !m_speed.Contains(speed);
        }

        private static readonly List<string> m_speed = new List<string> {
            "5 Kb/s", "10 Kb/s", "20 Kb/s", "50 Kb/s", "100 Kb/s", "125 Kb/s", "250 Kb/s", "500 Kb/s", "800 Kb/s", "1000 Kb/s", "8.333 Kb/s", "33.333 Kb/s", "47.619 Kb/s", "95.238 Kb/s"
        };

        private static readonly List<string> m_cmd = new List<string> {
            "Sd", "S0", "S1", "S2", "S3", "S4", "S5", "S6", "S7", "S8", "Sa", "Sc", "Sb", "S9"
        };

        private static readonly List<string> m_speedPriority = new List<string> {
            "500 Kb/s", "250 Kb/s", "125 Kb/s", "100 Kb/s", "1000 Kb/s", "800 Kb/s", "50 Kb/s", "20 Kb/s", "10 Kb/s", "5 Kb/s", "8.333 Kb/s", "33.333 Kb/s", "47.619 Kb/s", "95.238 Kb/s"
        };
    }
    #endregion

    #region compression
    static public class Compression
    {


        private static byte[] u32_to_bytes(UInt32 val)
        {
            byte[] res = new byte[4];

            res[0] = (byte)((val >> 24) & 0xFF);
            res[1] = (byte)((val >> 16) & 0xFF);
            res[2] = (byte)((val >> 8) & 0xFF);
            res[3] = (byte)((val) & 0xFF);

            return res;
        }


        public static canMessage2 buff_to_can_message(byte[] buff, ref int start_pos)
        {
            // looking for a header
            canMessage2 msg = null;

            // to short
            if (start_pos + 2 >= buff.Length)
                return null;

            // get header
            UInt16 hdr = 0;
            hdr += (UInt16)buff[start_pos + 1];
            hdr += (UInt16)((buff[start_pos]) << 8);

            // check it 
            if ((hdr & 0xF000) != 0x1000)
                return null;

            // restore frame len
            int len = 0;
            len += (hdr & 0x0100) > 0 ? 16 : 0;
            len += (hdr >> 4) & 0x0F;

            // incorrect len. min = 6 (2 hdr + 2 ts + 2 id)
            if (len < 6 || len > 18)
                return null;

            // too short
            if (start_pos + len > buff.Length)
                return null;

            byte[] test_buff = new byte[len];
            for (int i = 0; i < len; i++)
                test_buff[i] = buff[start_pos + i];

            UInt32 ts = 0;
            UInt32 id = 0;
            bool is29bit = false;
            int id_len_bytes = 0;
            int ts_len_bytes = 0;
            int dlc = 0;

            is29bit = (hdr & 0x0200) > 0;
            ts_len_bytes = (hdr & 0x0800) > 0 ? 4 : 2;
            id_len_bytes = (hdr & 0x0400) > 0 ? 4 : 2;
            dlc = hdr & 0x0F;

            // check msg dlc
            if (dlc > 8)
                return null;

            int pos = start_pos + 2;

            // restore timestamp
            if (ts_len_bytes == 4)
            {
                ts += (UInt32)(buff[pos++] << 24);
                ts += (UInt32)(buff[pos++] << 16);
            }
            ts += (UInt32)(buff[pos++] << 8);
            ts += (UInt32)(buff[pos++]);

            // restore msg id
            if (id_len_bytes == 4)
            {
                id += (UInt32)(buff[pos++] << 24);
                id += (UInt32)(buff[pos++] << 16);
            }
            id += (UInt32)(buff[pos++] << 8);
            id += (UInt32)(buff[pos++]);

            // finish
            byte[] data = new byte[dlc];
            for (int i = 0; i < dlc; i++)
                data[i] = buff[pos++];
            msg = new canMessage2((int)id, is29bit, data, 0);
            msg.timestamp_absolute = (int)ts;

            start_pos = pos;
            return msg;
        }

        // most popular - dlc=8, id=11bit = type 3
        // hdr 0003_TIII IIII_IIII ts 2-4 data (12-14b) 

/*
        private static byte[] can_compress_3(canMessage2 msg)
        {
            if (msg.Id.Is29bit || msg.Id.Dlc != 8)
                return null;


            UInt32 ts = (UInt32)msg.timestamp_absolute;
            UInt32 id = (UInt32)msg.Id.Id;

            // timestamp
            byte[] ts_buff = u32_to_bytes(ts);
            // message id
            byte[] id_buff = u32_to_bytes(id);

            byte hdr = 0x30;
            bool short_timestamp = false;// ts <= 0xFFFF;
            if (!short_timestamp)
                hdr += 0x08;

            hdr += (byte)((id >> 8) & 0x0F);
            byte id_byte = (byte)id;

            int frame_len = 10 + (short_timestamp ? 2 : 4);

            byte[] res = new byte[frame_len];
            int pos = 0;
            // header
            res[pos++] = hdr;
            // id_byte
            res[pos++] = id_byte;
            // timestamp
            if (!short_timestamp)
            {
                for (int i = 0; i < 4; i++)
                    res[pos++] = ts_buff[i];
            }
            else
            {
                for (int i = 2; i < 4; i++)
                    res[pos++] = ts_buff[i];
            }

            // data
            for (int i = 0; i < msg.Id.Dlc; i++)
                res[pos++] = msg.Data[i];

            return res;
        }

        private static byte[] can_compress_2(canMessage2 msg)
        {
            // type 2 - for 11 bit messages

            // u16 - id + dlc => 0DDD_DIII_IIII_IIII
            // u8 * 8 - data
            // u16/32 - ts 
            // 01TL_LLLL
            // header (1) + ts 2/4 + id_dlc (2) + data * 8
            // len min = 13, len max = 15
            // 0002_0TLL

            if (msg.Id.Is29bit)
                return null;

            byte hdr = 0;
            UInt32 ts = (UInt32)msg.timestamp_absolute;
            UInt32 id = (UInt32)(msg.Id.Dlc << 5) + (UInt32)msg.Id.Id;

            bool short_timestamp = ts <= 0xFFFF;
            bool short_id = true;

            // timestamp
            byte[] ts_buff = u32_to_bytes(ts);
            // message id
            byte[] id_buff = u32_to_bytes(id);

            int frame_len = 1  + msg.Id.Dlc ;
            // id
            frame_len += short_id ? 2 : 4;
            // timestamp
            frame_len += short_timestamp ? 2 : 4;


            // header
            hdr = 0x10;
            if (short_timestamp)
                hdr += 0x04;
            switch (frame_len)
            {
                case 13:
                    hdr += 0x00;
                    break;
                case 14:
                    hdr += 0x01;
                    break;
                case 15:
                    hdr += 0x02;
                    break;
                default:
                    return null;
            }

            byte[] res = new byte[frame_len];
            int pos = 0;
            // header
            res[pos++] = hdr;
            // timestamp
            if (!short_timestamp)
            {
                for (int i = 0; i < 4; i++)
                    res[pos++] = ts_buff[i];
            }
            else
            {
                for (int i = 2; i < 4; i++)
                    res[pos++] = ts_buff[i];
            }

            // id
            for (int i = 2; i < 4; i++)
                res[pos++] = id_buff[i];

            // data
            for (int i = 0; i < msg.Id.Dlc; i++)
                res[pos++] = msg.Data[i];

            return res;

        }
        */

        public static byte[] can_message_to_buff(canMessage2 msg)
        {
            // type 1
            // header u16 + ts u16/u32 + id u16/u32 + data u8 * (up to 8)
            // min buff len: 2 + 2 + 2 + 8 = 14
            // max buff len: 2 + 4 + 4 + 8 = 18
            // 'T' - timestamp len, 2 bytes => '0', 4 bytes => '1'
            // 'I' - id len, 2 bytes => '0', 4 bytes => '1'
            // 'E' - 29bit flag - 1bit 3
            // 'L' - len - 5 bits
            // 'D' - dlc - 4 bits
            // header 0001_TIEL_LLLL_DDDD

            byte hdr_hi = 0, hdr_lo = 0;
            UInt32 ts = (UInt32)msg.timestamp_absolute;
            UInt32 id = (UInt32)msg.Id.Id;

            bool short_timestamp = ts <= 0xFFFF;
            bool short_id = id <= 0xFFFF;

            // timestamp
            byte[] ts_buff = u32_to_bytes(ts);
            // message id
            byte[] id_buff = u32_to_bytes(id);

            int frame_len = 2 /* header */ + msg.Id.Dlc /* dlc */;
            // id
            frame_len += short_id ? 2 : 4;
            // timestamp
            frame_len += short_timestamp ? 2 : 4;

            // header
            hdr_lo = 0;
            hdr_hi = 0x10;
            if (!short_timestamp)
                hdr_hi += 0x08;
            if (!short_id)
                hdr_hi += 0x04;
            if (msg.Id.Is29bit == true)
                hdr_hi += 0x02;
            if ((frame_len & 0x10) != 0)
                hdr_hi += 0x01;
            hdr_lo += (byte)((frame_len & 0x0F) << 4);
            hdr_lo += (byte)(msg.Id.Dlc & 0x0F);


            byte[] res = new byte[frame_len];
            int pos = 0;
            // header
            res[pos++] = hdr_hi;
            res[pos++] = hdr_lo;
            // timestamp
            if (!short_timestamp)
            {
                for (int i = 0; i < 4; i++)
                    res[pos++] = ts_buff[i];
            }
            else
            {
                for (int i = 2; i < 4; i++)
                    res[pos++] = ts_buff[i];
            }

            // id
            if (!short_id)
            {
                for (int i = 0; i < 4; i++)
                    res[pos++] = id_buff[i];
            }
            else
            {
                for (int i = 2; i < 4; i++)
                    res[pos++] = id_buff[i];
            }

            // data
            for (int i = 0; i < msg.Id.Dlc; i++)
                res[pos++] = msg.Data[i];

            return res;
        }

        private static byte[] calc_hash(byte[] buff, int offset = 0, int len = -1)
        {
            if (len < 0)
                len = buff.Length;

            return ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(buff, offset, len);
        }

        private static bool check_hash(byte[] buff_hashed, int offset = 0, int len = -1)
        {
            // check
            if (buff_hashed == null || buff_hashed.Length < 16)
                return false;

            if (len < 0)
                len = buff_hashed.Length - offset;

            if (offset < 0 || len < 16)
                return false;
            if (len + offset + 16 > buff_hashed.Length)
                return false;

            // calc
            byte[] hash = ((HashAlgorithm)CryptoConfig.CreateFromName("MD5")).ComputeHash(
                buff_hashed, offset, len);
            // check
            if (hash == null || hash.Length != 16)
                return false;
            // compare
            for (int i = 0; i < hash.Length; i++)
                if (hash[i] != buff_hashed[len + offset + i])
                    return false;

            return true;
        }

        // compress a CAN message list
        public static byte[] CanMessagesCompress(List<canMessage2> ls)
        {
            if (ls == null || ls.Count == 0)
                return null;

            //Stopwatch sw = Stopwatch.StartNew();
            // create a raw buffer
            List<byte> raw = new List<byte>();

            //byte[] raw = new byte[ls.Count * 20];
            int raw_len = 0;
            foreach(var m in ls)
            {
                byte[] tmp = can_message_to_buff(m);
                if (tmp != null)
                {
                    raw.AddRange(tmp);
                    //tmp.CopyTo(raw, raw_len);
                   // raw_len += tmp.Length;
                }
            }

            raw_len = raw.Count;

            //long ts = sw.ElapsedMilliseconds;

            // compress
            if (raw_len == 0)
                return null;

            // do compress
            byte[] compressed = Compress(raw.ToArray(), raw_len);


            // prepare the header string
            bool use_compressed = compressed.Length < raw_len;

            // use_compressed = false;

            int data_len = use_compressed ? compressed.Length : raw_len;

            string sheader = string.Format("<type=1,compress={0},dt={1:yyyy/MM/dd H:mm:ss zzz},cnt={2},len={3}>",
                use_compressed ? 1 : 0, DateTimeOffset.Now, ls.Count, data_len) + Environment.NewLine;

            List<byte> res = new List<byte>();
            // add the header
            res.AddRange(Encoding.ASCII.GetBytes(sheader));
            // add the data
            if (use_compressed)
                res.AddRange(compressed);           
            else
                res.AddRange(raw);

            Debug.WriteLine(string.Format("C: Comp = {0}, data_len = {1}, msg_cnt = {2}", 
                use_compressed, data_len, ls.Count));

            // add the hash
            byte[] md5 = calc_hash(res.ToArray());
            res.AddRange(md5);
            // new line
            res.AddRange(Encoding.ASCII.GetBytes(Environment.NewLine));

            // done
            return res.ToArray();
        }

        private static string extract_header(byte[] buff, int start_pos, out int out_hdr_start_pos, out int out_data_start_pos)
        {
            string header = string.Empty;
            int tmp_hdr_start_pos = -1;

            for (int i = start_pos; i < buff.Length; i++)
            {
                char ch = (char)buff[i];
                if (ch == '<')
                {
                    tmp_hdr_start_pos = i;
                }
                if (ch == '>' && tmp_hdr_start_pos >= 0)
                {
                    int end_pos = i;
                    if (end_pos > tmp_hdr_start_pos && (end_pos - tmp_hdr_start_pos) < 128)
                    {
                        int len = end_pos - tmp_hdr_start_pos + 1 + Environment.NewLine.Length;
                        if (len > 0)
                        {
                            header = Encoding.Default.GetString(buff, tmp_hdr_start_pos, len);
                            // check
                            if (header.IndexOf("<type=") == 0)
                                break;
                        }
                    }
                    // reset
                    tmp_hdr_start_pos = -1;
                }
            }

            out_hdr_start_pos = tmp_hdr_start_pos;
            out_data_start_pos = header.Length > 0 ?
                tmp_hdr_start_pos + header.Length : -1;
            return header;
        }

        public static long CanMessageCompressedMessageCountGet(byte[] compressed)
        {
            long res = 0;

            // check in data
            if (compressed == null || compressed.Length == 0)
                return 0;

            int buff_cur_pos = 0;

            while (buff_cur_pos >= 0 && buff_cur_pos <= compressed.Length)
            {
                // header params
                int header_arg_data_len = -1;
                long header_arg_msg_cnt = -1;

                int start_pos_data = -1;
                int start_pos_hdr = -1;

                // try to get the next header
                string header = extract_header(compressed, buff_cur_pos, out start_pos_hdr, out start_pos_data);

                // finish
                if (header.Length == 0)
                    return res;

                // parse the header
                Match m = Regex.Match(header, "cnt=(\\d+)");
                if (m != null && m.Success)
                    header_arg_msg_cnt = Convert.ToInt32(m.Groups[1].ToString());
                m = Regex.Match(header, "len=(\\d+)");
                if (m != null && m.Success)
                    header_arg_data_len = Convert.ToInt32(m.Groups[1].ToString());

                // update the buff pos
                buff_cur_pos = start_pos_data + header_arg_data_len + 16;

                res += header_arg_msg_cnt;
            }

            return res;
        }

        // decompress a CAN message list
        public static List<canMessage2> CanMessagesDecompress(byte[] compressed)
        {
            List<canMessage2> ls = new List<canMessage2>();

            // check in data
            if (compressed == null || compressed.Length == 0)
                return ls;

            int buff_cur_pos = 0;

            while (buff_cur_pos >= 0 && buff_cur_pos <= compressed.Length)
            {
                // header params
                int header_arg_is_compressed = -1;
                int header_arg_type = -1;
                int header_arg_data_len = -1;
                int header_arg_msg_cnt = -1;

                int start_pos_data = -1;
                int start_pos_hdr = -1;

                // try to get the next header
                string header = extract_header(compressed, buff_cur_pos, out start_pos_hdr, out start_pos_data);

                // check
                if (header.Length == 0)
                    return ls;

                // parse the header
                Match m = Regex.Match(header, "compress=(\\d+)");
                if (m != null && m.Success)
                    header_arg_is_compressed = Convert.ToInt32(m.Groups[1].ToString());
                m = Regex.Match(header, "type=(\\d+)");
                if (m != null && m.Success)
                    header_arg_type = Convert.ToInt32(m.Groups[1].ToString());
                m = Regex.Match(header, "len=(\\d+)");
                if (m != null && m.Success)
                    header_arg_data_len = Convert.ToInt32(m.Groups[1].ToString());
                m = Regex.Match(header, "cnt=(\\d+)");
                if (m != null && m.Success)
                    header_arg_msg_cnt = Convert.ToInt32(m.Groups[1].ToString());

                // check
                if (header_arg_type != 1 || header_arg_is_compressed < 0 || header_arg_data_len < 0)
                    return ls;
                // check hash
                if (!check_hash(compressed, start_pos_hdr, header.Length + header_arg_data_len))
                    return ls;

                byte[] raw = null;

                // prepare
                if (header_arg_is_compressed == 0)
                {
                    // just copy
                    raw = new byte[header_arg_data_len];
                    for (int i = 0; i < raw.Length; i++)
                        raw[i] = compressed[i + start_pos_data];
                }
                else if (header_arg_is_compressed == 1)
                {
                    // decompress
                    raw = Decompress(compressed, start_pos_data, header_arg_data_len);
                }

                // check
                if (raw == null)
                    return ls;

                // restore
                if (raw != null)
                {
                    int pos = 0;
                    while (pos < raw.Length)
                    {
                        canMessage2 msg = buff_to_can_message(raw, ref pos);
                        if (msg == null)
                            pos++;
                        else
                            ls.Add(msg);
                    }

                    raw = null;
                }

                // update the buff pos
                buff_cur_pos = start_pos_data + header_arg_data_len + 16;
            }

            return ls;
        }


        public static byte[] Compress(byte[] data, int len)
        {
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
            {
                dstream.Write(data, 0, len);
            }
            return output.ToArray();
        }

        public static byte[] Decompress(byte[] data, int start_pos = 0, int count = -1)
        {
            if (count < 0)
                count = data.Length - start_pos;

            MemoryStream input = new MemoryStream(data, start_pos, count);
            MemoryStream output = new MemoryStream();
            using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
            {
                dstream.CopyTo(output);
            }
            return output.ToArray();
        }


        public static byte[] SerializeAndCompress(this object obj)
        {
            using (MemoryStream ms = new MemoryStream())
            {
                using (GZipStream zs = new GZipStream(ms, CompressionMode.Compress, true))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(zs, obj);
                }
                return ms.ToArray();
            }
        }

        public static T DecompressAndDeserialize<T>(this byte[] data)
        {
            using (MemoryStream ms = new MemoryStream(data))
            using (GZipStream zs = new GZipStream(ms, CompressionMode.Decompress, true))
            {
                BinaryFormatter bf = new BinaryFormatter();
                return (T)bf.Deserialize(zs);
            }
        }
    }
    #endregion

    // my simple calculator implementation
    // created to use insead of Ncalc, has much better performance
    public class MyCalc
    {

        // check is this digit?
        static private bool isDigit(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        // replace the exact substring entry
        static private string replace_exact_substring(string str_in, string str_replace)
        {
            string format = string.Format(@"\b{0}\b", str_replace);
            return Regex.Replace(str_in, format, str_replace);
        }

        // hex to dec
        static private string replaceHexToDecimal(string str)
        {
            if (str == null || str == string.Empty)
                return str;

            // make a copy
            string res = string.Copy(str);

            // replace
            var reg = Regex.Matches(res, "0x[a-fA-F0-9]+");
            foreach (Match v in reg)
            {
                string s_hex = v.ToString(); 

                if (res.Contains(s_hex))
                {
                    UInt32 int_dec = 0;
                    bool parsed = UInt32.TryParse(s_hex.Substring(2), 
                        NumberStyles.HexNumber, 
                        CultureInfo.InvariantCulture, 
                        out int_dec);

                    // replace the exact string
                    if (parsed)
                    {
                        string format = string.Format(@"\b{0}\b", s_hex);
                        res = Regex.Replace(res, format, int_dec.ToString());
                    }
                }  
            }

            return res;
        }

        // try to parse a dec number
        static private bool tryToParseDecInt(string s, out int val)
        {
            // clean
            val = 0;
            // check
            if (s == null || s == string.Empty)
                return false;
            // try to parse
            bool res = int.TryParse(s, NumberStyles.Integer,
                CultureInfo.InvariantCulture, out val);

            return res;
        }

        // remove spaces
        static public string removeSpaces(string str)
        {
            return str.Replace(" ", "");
        }

        // get left (pre) and right (post) parts of the expretion
        static private bool getExpressionParts(string expression, string separator, out string pre, out string post)
        {
            pre = null;
            post = null;

            int idx = expression.IndexOf(separator);
            bool has_pre = false, has_post = false;

            // post
            if (idx > 0)
            {
                int shift_pos = idx;
                int start_pos = shift_pos + separator.Length;
                int end_pos = -1;
                int tmp_pos = start_pos;
                int cnt_op = 0, cnt_cl = 0;

                // either a number or an expression
                bool is_expression = false;
                if (expression[tmp_pos] == '(')
                {
                    is_expression = true;
                    cnt_op = 1;
                    tmp_pos++;
                }

                if (is_expression)  // an expression
                {
                    while (tmp_pos < expression.Length && cnt_op != cnt_cl)
                    {
                        char ch = expression[tmp_pos];
                        if (ch == '(')
                            cnt_op++;
                        else if (ch == ')')
                            cnt_cl++;

                        tmp_pos++;
                    }
                    // finish
                    if (cnt_op == cnt_cl)
                        end_pos = tmp_pos;
                }
                else  // a number
                {
                    bool is_digit = true;
                    while (tmp_pos < expression.Length && is_digit)
                    {
                        char ch = expression[tmp_pos];
                        is_digit = isDigit(ch);

                        if (is_digit)
                            tmp_pos++;
                    }
                    // finish
                    if (tmp_pos <= expression.Length)
                        end_pos = tmp_pos;
                }

                // failed to find, finish
                if (end_pos <= 0)
                    return false;

                post = expression.Substring(start_pos, end_pos - start_pos);
                has_post = true;
            }

            // pre
            if (idx > 0 && has_post)
            {
                int shift_pos = idx;
                int start_pos = -1;
                int end_pos = idx - 1;
                int tmp_pos = end_pos;
                int cnt_op = 0, cnt_cl = 0;

                // either a number or an expression
                bool is_expression = false;
                if (expression[tmp_pos] == ')')
                {
                    is_expression = true;
                    cnt_cl = 1;
                    tmp_pos--;
                }

                if (is_expression)  // an expression
                {
                    while (tmp_pos >= 0 && cnt_op != cnt_cl)
                    {
                        char ch = expression[tmp_pos];
                        if (ch == '(')
                            cnt_op++;
                        else if (ch == ')')
                            cnt_cl++;

                        tmp_pos--;
                    }
                    // finish, unclude the last bracket
                    if (cnt_op == cnt_cl)
                        start_pos = tmp_pos + 1;
                }
                else  // a number
                {
                    bool is_digit = true;
                    while (tmp_pos >= 0 && is_digit)
                    {
                        char ch = expression[tmp_pos];
                        is_digit = isDigit(ch);

                        if (is_digit)
                            tmp_pos--;
                    }
                    // finish
                    if (tmp_pos >= 0)
                        start_pos = tmp_pos + 1;
                    if (is_digit && tmp_pos < 0)
                        start_pos = 0;
                }

                // check
                if (start_pos < 0 || start_pos > end_pos)
                    return false;

                pre = expression.Substring(start_pos, end_pos - start_pos + 1);
                has_pre = true;
            }

            return has_pre && has_post;
        }

        // try to replace an unary bitwise operator
        static private bool tryReplaceBitwiseUnaryOperator(string str_in, string str_oper, out string res)
        {
            // reset
            res = null;
            // check
            if (str_in == null || str_in.Length == 0)
                return false;

            // prepare
            string expression = string.Copy(str_in);

            while (expression.Contains(str_oper))
            {
                // get left and right parts
                string pre, post;
                if (!getExpressionParts(expression, str_oper, out pre, out post))
                    return false;

                // calculate right one
                object val_post = simpleCalc(post);
                if (val_post == null)
                    return false;

                // calculate a new value
                //uint val = Convert.ToUInt32(val_post);
                uint val = objToU32(val_post);
                string str_new;

                // handle
                if (str_oper == ">>")
                    str_new = "/" + (Convert.ToInt32(Math.Pow(2, val))).ToString();
                else if (str_oper == "<<")
                    str_new = "*" + (Convert.ToInt32(Math.Pow(2, val))).ToString();
                else if (str_oper == "~")
                    str_new = (~val).ToString();
                else
                    return false;

                // replace
                expression = expression.Replace(str_oper + post, str_new);
            }

            res = expression;
            return true;
        }

        // try to replace a binary bitwise operator
        static private bool tryReplaceBitwiseBinaryOperator(string str_in, string str_oper,out string res)
        {
            // reset
            res = null;
            // check
            if (str_in == null || str_in.Length == 0)
                return false;

            // prepare
            string expression = string.Copy(str_in);

            while (expression.Contains(str_oper))
            {
                // get left and right parts
                string pre, post;
                if (!getExpressionParts(expression, str_oper, out pre, out post))
                    return false;

                // calculate right one
                object val_post = simpleCalc(post);
                if (val_post == null)
                    return false;
                // calculate left one
                object val_pre = simpleCalc(pre);
                if (val_pre == null)
                    return false;

                // calculate a new value
                uint val = 0;
                //decimal dec1 = Convert.ToDecimal(val_pre);
                //decimal dec2 = Convert.ToDecimal(val_post);
                //UInt32 u1 = Convert.ToUInt32(Math.Floor(dec1));
                //UInt32 u2 = Convert.ToUInt32(Math.Floor(dec2));
                UInt32 u1 = objToU32(val_pre);
                UInt32 u2 = objToU32(val_post);

                if (str_oper == "&")
                    val = u1 & u2;
                else if (str_oper == "|")
                    val = u1 | u2;
                else if (str_oper == "^")
                    val = u1 ^ u2;
                else
                    return false;

                // replace
                string str_new = val.ToString();
                expression = expression.Replace(pre + str_oper + post, str_new);
            }

            res = expression;
            return true;
        }

        // try to replace a bitwise left shift operator
        static private bool tryReplaceBitwiseShiftLeft(string s, out string res)
        {
            return tryReplaceBitwiseUnaryOperator(s, "<<", out res);
        }

        // try to replace a bitwise right shift operator
        static private bool tryReplaceBitwiseShiftRight(string s, out string res)
        {
            return tryReplaceBitwiseUnaryOperator(s, ">>", out res);
        }

        // try to replace a bitwise inversion operator
        static private bool tryReplaceBitwiseInversion(string s, out string res)
        {
            return tryReplaceBitwiseUnaryOperator(s, "~", out res);
        }

        // try to replace bitwise AND (&) operator
        static private bool tryReplaceBitwiseAnd(string s, out string res)
        {
            return tryReplaceBitwiseBinaryOperator(s, "&", out res);
        }

        // try to replace bitwise OR (|) operator
        static private bool tryReplaceBitwiseOR(string s, out string res)
        {
            return tryReplaceBitwiseBinaryOperator(s, "|", out res);
        }

        // try to replace bitwise XOR (^) operator
        static private bool tryReplaceBitwiseXOR(string s, out string res)
        {
            return tryReplaceBitwiseBinaryOperator(s, "^", out res);
        }

        // simple calculation of the prepared string
        static private object simpleCalc(string str_in)
        {
            object res = null;

            if (str_in == null || str_in.Length == 0)
                return null;

            // make a copy
            string expression = string.Copy(str_in);

            // make sure this is not a number
            int tmp_int = 0;
            if (tryToParseDecInt(expression, out tmp_int))
                return tmp_int;

            // replace bitwise operators
            if (!tryReplaceBitwiseShiftLeft(expression, out expression))
                return null;
            if (!tryReplaceBitwiseShiftRight(expression, out expression))
                return null;
            if (!tryReplaceBitwiseInversion(expression, out expression))
                return null;
            if (!tryReplaceBitwiseAnd(expression, out expression))
                return null;
            if (!tryReplaceBitwiseOR(expression, out expression))
                return null;
            if (!tryReplaceBitwiseXOR(expression, out expression))
                return null;
        
            
            DataTable dt = new DataTable();
            try
            {
                res = dt.Compute(expression, "");
            }
            catch { }

            return res;
        }

        // evaluate a string expression
        static public object evaluate(string str_expression)
        {
            // check
            if (str_expression == null || str_expression.Length == 0)
                return null;

            int tmp_int = 0;
            // make sure this is not a number
            if (tryToParseDecInt(str_expression, out tmp_int))
                return tmp_int;

            // prepare
            string expression = string.Copy(str_expression);
            expression = removeSpaces(expression);
            expression = replaceHexToDecimal(expression);

            // make sure this is not a number
            if (tryToParseDecInt(str_expression, out tmp_int))
                return tmp_int;

            // check for unsupported characters
            var reg = new Regex("^[0-9x+\\-*\\/><\\(\\)|&^~]+$");
            if (!reg.IsMatch(expression))
                return null;

            // calculate
            return simpleCalc(expression);
        }

        // try to simplify string
        static public string simplify(string str_expression)
        {
            string expression = string.Copy(str_expression);

            // these two are safe
            // remove spaces
            expression = removeSpaces(expression);
            // hex to decimal
            expression = replaceHexToDecimal(expression);

            // and these don't
            string expression_tmp = string.Copy(expression);
            // bitwise shift operators 
            if (!tryReplaceBitwiseShiftLeft(expression, out expression_tmp))
                return expression;
            else
                expression = string.Copy(expression_tmp);

            if (!tryReplaceBitwiseShiftRight(expression, out expression_tmp))
                return expression;
            else
                expression = string.Copy(expression_tmp);

            return expression;
        }

        // convert the calculation result to int32
        static public Int32 objToI32(object ob)
        {
            if (ob == null)
                return 0;

            decimal dec = Convert.ToDecimal(ob);
            Int32 i32 = Convert.ToInt32(Math.Floor(dec));
            return i32;
        }

        // convert the calculation result to uint32
        static public UInt32 objToU32(object ob)
        {
            if (ob == null)
                return 0;

            decimal dec = Convert.ToDecimal(ob);
            UInt32 u32 = Convert.ToUInt32(Math.Floor(dec));
            return u32;
        }

        // convert the calculation result to byte
        static public byte objToByte(object ob)
        {
            var u32 = objToI32(ob);
            byte b = (byte)(u32 & 0xFF);
            return b;
        }

        // test func
        static private bool test()
        {
            bool res = true;

            string[] exp_1081 = {

                "((100*(9+1) + ( ((4*2+2) & 0xFF) << 3 ) + (3^2)) & (0xFFFF | 0xF))",
                "1000 + ( (4*2+2) << 3 ) + 1",
                "1000 + ( 10 << (4/2+1-1+1) ) + 1",
                "1000 + ( ((10 & 0xFF)/1) << ((2+1)*1/1) ) + 1",
                "1081",
                "1081 & 0xFFFFFFFF",
                "0xFFFFFFFF & 1081",
                "((1081)<<2)/4",
                "0<<5 + 1081"
            };

            object my = null;
            foreach (var item in exp_1081)
            {
                int res_val = 1081;
                my = evaluate(item);
                //ncalc = Tools.tryCalcString(item);

                if (my == null)
                {
                    Debug.WriteLine(string.Format("Failed to evalute {0}", item));
                    res = false;
                    continue;
                }
                if (Convert.ToInt32(my) != res_val)
                {
                    Debug.WriteLine(string.Format("Failed to evalute {0}, res = {1}", 
                        item, Convert.ToInt32(my)));
                    res = false;
                }
            }

            return res;
        }
    }

    // tools
    static public class Tools
    {
        static public string convertByteListToString(List<byte> bl, string prefix, string postfix = " ")
        {
            StringBuilder sb = new StringBuilder();

            if (bl != null)
            {
                for (int i = 0; i < bl.Count; i++)
                {
                    if (prefix != null)
                        sb.Append(prefix);

                    sb.Append(bl[i].ToString("X2"));

                    if (postfix != null && (i + 1) < bl.Count)
                        sb.Append(postfix);
                }
            }

            return sb.ToString();
        }

        static public string dt_to_string(DateTime dt)
        {
            if (dt == null)
                dt = DateTime.Now;
            return dt.ToString("yyyy.MM.dd HH:mm:ss");
        }

        static public bool swap_strings(ref string str1, ref string str2)
        {
            if (str1 == null || str2 == null)
                return false;

            string tmp = string.Copy(str1);
            str1 = string.Copy(str2);
            str2 = string.Copy(tmp);
            return true;
        }

        static public bool swap<T>(ref T val1, ref T val2) 
        {
            if (val1 == null || val2 == null)
                return false;
            T temp;
            temp = val1;
            val1 = val2;
            val2 = temp;
            return true;
        }

        static public string removeSpaces(string str)
        {
            return str.Replace(" ", "");
        }

        // replace the exact substring entry
        static public string replace_exact_substring(string str_in, string str_old, string str_new)
        {
            string format = string.Format(@"\b{0}\b", str_old);
            string t = Regex.Replace(str_in, format, str_new);
            return t;
        }

        static public string hex2bin(string hex, bool separate = true)
        {
            // convert
            long longValue = Convert.ToInt64(hex, 16);
            return hex2bin(longValue, separate);
        }

        static public string hex2bin(long longValue, bool separate = true)
        {
            string binRepresentation = Convert.ToString(longValue, 2);
            // allign
            while (binRepresentation.Length % 8 != 0)
                binRepresentation = "0" + binRepresentation;
            // separate
            if (separate)
            {
                int sepSymbols = binRepresentation.Length / 4 - 1;  // 4 symb per a separator
                for (int i = 1; i <= sepSymbols; i++)
                    binRepresentation = binRepresentation.Insert(4 * i, " ");
            }

            return binRepresentation;
        }

        static public bool isHex(char ch)
        {
            return ((ch >= '0' && ch <= '9') || (ch >= 'A' && ch <= 'F'));
        }

        static public bool isInt(char ch)
        {
            return ch >= '0' && ch <= '9';
        }

        static public byte[] hexStringToByteArray(string hex)
        {
            byte[] res = null;

            // remove leading and trailing spaces
            hex = hex.Trim();

            // try to split with spaces
            string[] s = hex.Split(' ');
            // and with commas
            if (s.Length == 0)
                s = hex.Split(',');

            if (s.Length != 0)
            {
                res = new byte[s.Length];

                for (int i = 0; i < s.Length; i++)
                {
                    string item = s[i];
                    byte tmp;
                    if (byte.TryParse(item, System.Globalization.NumberStyles.HexNumber, CultureInfo.InvariantCulture, out tmp))
                        res[i] = tmp;
                    else
                        return null;
                }

            }

            return res;
        }


        static public void textBoxIntOnlyEvent(object sender, KeyPressEventArgs e)
        {
            char ch = char.ToUpper(e.KeyChar);
            if (Tools.isHex(ch) || '\b' == ch)
                e.KeyChar = ch;
            else
                e.Handled = true;
        }

        static public void textBoxHexOnlyEvent(object sender, KeyPressEventArgs e)
        {
            char ch = char.ToUpper(e.KeyChar);
            if (Tools.isInt(ch) || '\b' == ch)
                e.KeyChar = ch;
            else
                e.Handled = true;
        }

        static public string strReplaceHexToDec(string str)
        {
            if (str == null || str == string.Empty)
                return str;

            // make a copy
            string res = string.Copy(str);

            // replace
            var reg = Regex.Matches(res, "0x[a-fA-F0-9]+");
            foreach (Match v in reg)
            {
                string s_hex = v.ToString();

                if (res.Contains(s_hex))
                {
                    UInt32 int_dec = 0;
                    bool parsed = UInt32.TryParse(s_hex.Substring(2),
                        NumberStyles.HexNumber,
                        CultureInfo.InvariantCulture,
                        out int_dec);

                    // replace the exact string
                    if (parsed)
                    {
                        string format = string.Format(@"\b{0}\b", s_hex);
                        res = Regex.Replace(res, format, int_dec.ToString());
                    }
                }
            }

            return res;
        }

        static public bool tryParseInt(string s, out int val)
        {
            int valBase = 10;

            // id
            if (s.Contains("0x"))
            {
                s = s.Substring(2);
                valBase = 16;
            }

            bool res = int.TryParse(s, valBase == 16 ? NumberStyles.HexNumber : NumberStyles.Integer,
                CultureInfo.InvariantCulture, out val);

            return res;
        }


        static public object tryCalcString(string s)
        {
            string sval = strReplaceHexToDec(s);
            object eval = null;
            int tmp = 0;

            // use a simple parser
            if (tryParseInt(s, out tmp))
            {
                return tmp;
            }
            // ncalc
            try
            {
                Expression e = new Expression(sval);
                eval = e.Evaluate();
            }
            catch
            {
            }

            return eval;
        }

        // to improve grid performance 
        static public void SetDoubleBuffered(Control c, bool value)
        {
            PropertyInfo pi = typeof(Control).GetProperty("DoubleBuffered", BindingFlags.SetProperty | BindingFlags.Instance | BindingFlags.NonPublic);
            if (pi != null)
            {
                pi.SetValue(c, value, null);

                MethodInfo mi = typeof(Control).GetMethod("SetStyle", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic);
                if (mi != null)
                {
                    mi.Invoke(c, new object[] { ControlStyles.UserPaint | ControlStyles.AllPaintingInWmPaint | ControlStyles.OptimizedDoubleBuffer, true });
                }

                mi = typeof(Control).GetMethod("UpdateStyles", BindingFlags.Instance | BindingFlags.InvokeMethod | BindingFlags.NonPublic);
                if (mi != null)
                {
                    mi.Invoke(c, null);
                }
            }
        }

        static private readonly int tsLenMin = 12;
        static private readonly int idLenMin = 12;
        static private readonly int dlcMinLen = 5;

        static public string ConvertMessageToString(canMessage2 msg, bool useTimeStamp = false)
        {
            string res = string.Empty;
            int dlc_diff = canMessageId.MaxDlc - msg.Id.Dlc;
            // ts
            if (useTimeStamp)
                res += msg.TimeStamp.ToString().PadRight(tsLenMin);
            // id
            res += ("0x" + msg.Id.GetIdAsString()).PadRight(idLenMin);
            // dlc
            res += msg.Id.GetDlcAsString().PadRight(dlcMinLen);
            // data
            res += msg.GetDataString(", ", "0x");
            // allign
            while(dlc_diff > 0)
            {
                res += "      "; // 6 symbols per byte
                dlc_diff--;
            }

            return res;
        }

        static public string ConvertMessageToString(List<canMessage2> ls, bool useTimeStamp = false)
        {
            string res = string.Empty;
            foreach (var m in ls)
                res += ConvertMessageToString(m, useTimeStamp) + Environment.NewLine;
            return res;
        }

        static public string ConvertMessageToStringFull(List<canMessage2> ls, List<int> intervals, List<int> count, bool useTimeStamp = false)
        {
            if (ls.Count != intervals.Count || ls.Count != count.Count)
                return string.Empty;

            string res = string.Empty;
            for (int i = 0; i < ls.Count; i++)
                res += ConvertMessageToString(ls[i], useTimeStamp) + 
                    "    " + intervals[i].ToString().PadRight(6) + 
                    "   " + count[i].ToString() +
                    Environment.NewLine;
            return res;
        }

        static public string getMessageHeaderString(bool useTimeStamp = false, bool usePeriod = false, bool useCount = false)
        {
            string sTs = useTimeStamp ? "timestamp".PadRight(tsLenMin) : string.Empty;
            string sPer = usePeriod ? "     msec"  : string.Empty;
            string sCnt = useCount  ? "     count" : string.Empty;
            return sTs +
                "ID".PadRight(idLenMin) + "DLC".PadRight(dlcMinLen) +
                "b0    b1    b2    b3    b4    b5    b6    b7 " + sPer + sCnt + 
                Environment.NewLine;
        }

        enum remotoDeviceId
        {
            remotoBasic3,
            remotoVanilla,
            remoto3,
            remotoBasic4,
        };
        
        static public string ConvertMessageToRemotoCmd (canMessage2 m)
        {
            string device = AppSettings.LoadRemotoDevice();
            if( !string.IsNullOrEmpty(device) )
            {
                Remoto.deviceTemplateList ls = Remoto.xlsDevices.getDeviceList();
                if( ls.Contains(device) )
                {
                    Remoto.deviceTemplate item = ls.Get(device);
                    if( !item.IsEmpty() )
                    {
                        string template = item.TemplateSend;
                        if( !string.IsNullOrEmpty(template))
                        {
                            // modify
                            if (template.Contains("_id"))
                                template = template.Replace("_id", "0x" + m.Id.GetIdAsString());
                            if (template.Contains("_dlc"))
                                template = template.Replace("_dlc", m.Id.GetDlcAsString());
                            if (template.Contains("_data"))
                                template = template.Replace("_data", m.GetDataString(", ", "0x"));

                            return template;
                        }
                    }
                }
            }
            return string.Empty;
        }

        static public string ConvertMessageToRemotoCmd (List<canMessage2> mList, bool useDelay = false)
        {
            string res = string.Empty;

            string device = AppSettings.LoadRemotoDevice();
            if (!string.IsNullOrEmpty(device))
            {
                Remoto.deviceTemplateList ls = Remoto.xlsDevices.getDeviceList();
                if (ls.Contains(device))
                {
                    Remoto.deviceTemplate item = ls.Get(device);
                    if (!item.IsEmpty())
                    {
                        string template = item.TemplateSend;
                        if (!string.IsNullOrEmpty(template))
                        {
                            for( int i = 0; i < mList.Count; i++ )
                            {
                                var m = mList[i];
                                string send = template;

                                // modify
                                if (send.Contains("_id"))
                                    send = send.Replace("_id", "0x" + m.Id.GetIdAsString());
                                if (send.Contains("_dlc"))
                                    send = send.Replace("_dlc", m.Id.GetDlcAsString());
                                if (send.Contains("_data"))
                                    send = send.Replace("_data", m.GetDataString(", ", "0x"));

                                res += send + Environment.NewLine;

                                if (useDelay) {
                                    string delay = item.TemplateDelay;
                                    if (!string.IsNullOrEmpty(delay) && i < mList.Count - 1)
                                    {
                                        int ms = mList[i+1].TimeStamp.TimeStamp - m.TimeStamp.TimeStamp;
                                        if (ms > 0)
                                        {
                                            delay = delay.Replace("_msec", ms.ToString());
                                            res += delay + Environment.NewLine;
                                        }
                                       
                                    }
                                }
                            }
                            return res;
                        }
                    }
                }
            }
            return string.Empty;
        }


        // 0000000000 00000440 00 8 40 00 80 00 00 00 00 00
        static string convertFromMaximsTraceToScript(string txt)
        {
            string res = string.Empty;


            return res;
        }

        public static class XmlSerializer<T>
        {
            // serialize
            public static string Serialize(T instance)
            {
                XmlSerializer s = new XmlSerializer(instance.GetType());
                using (StringWriter writer = new StringWriter())
                {
                    s.Serialize(writer, instance);
                    return writer.ToString();
                }
            }

            public static T Deserialize(string xml)
            {
                var serializer = new XmlSerializer(typeof(T));
                return (T)serializer.Deserialize(new StringReader(xml));
            }
        }


    }
}





namespace canAnalyzer
{
    /* todo: flow, diag (single & multi), long multi */
    class canCompare
    {
        /* find unique values */
        void compareListsByVal (List <canMessage2> ls1, List<canMessage2> ls2) 
        {

        }
    }
}