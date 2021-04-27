using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;

namespace canAnalyzer
{
    public enum parserResponse
    {
        noResponse,
        ok,
        failed
    }

    class canParser
    {
        public canFilter CanFilter { get; set; }
        public string m_version { get; set; }
        public bool confirm { get; set; }
        public int errCnt = 0;
        public bool TimestampError { get; set; }

        public parserResponse sendResponse { get; set; }


        // '1' -> 1, etc.
        public static int char2int(int b)
        {
            b -= '0';
            if (b >= 0x10)
                b = b - 0x10 + 9;
            return b;
        }
        // 1 -> '1', etc
        public static char int2char(int v)
        {
            int res = '0';
            if( v <= 0x0F )
            {
                res = v + '0';
                if (res > '9')
                    res = res - 9 + 0x10;
            }
            return (char)res;
        }

        // constructor
        public canParser(canFilter filter)
        {
            CanFilter = filter;
            TimestampError = false;
        }
        
        // parse serial number
        public string parseSerial(ref byte[] buff)
        {
            string res = String.Empty;

            if (buff.Length == 0)
                return res;

            // format
            // N0001.

            if (buff[0] == 'N' && buff.Length >= 6 && buff[5] == '\r')
            {
                // format: vMAMI
                int sn = 0;
                for (int i = 0, mul = 1000; i < 4; i++, mul /= 10)
                    sn += char2int(buff[1 + i]) * mul;
                res = sn.ToString("D4");
            }

            return res;
        }

        // parse version
        public string parseVersion(ref byte[] buff)
        {
            string res = String.Empty;

            if (buff.Length == 0)
                return res;

            // format
            // v0109. 
            // means v 1.09

            if (buff[0] == 'v' && buff.Length >= 6 && buff[5] == '\r')
            {
                // format: vMAMI
                int major = char2int(buff[1]) * 10 + char2int(buff[2]);
                int minor = char2int(buff[3]) * 10 + char2int(buff[4]);
                res = string.Format("{0}.{1}", major.ToString(), minor.ToString("D2"));
            }

            return res;
        }

        private int parse2ByteVal (char marker, ref List<byte> ls, ref int idx)
        {
            // format: F12
            const int len = 4;

            if (ls.Count < idx + len )
                return -1;

            if (ls[idx] != marker)
            {
                idx++;
                return -2;
            }

            byte eol = ls[idx + len-1];
            if (eol != '\r')
            {
                idx++;
                return -3;
            }

            int a1 = char2int(ls[++idx]);
            int a2 = char2int(ls[++idx]);

            idx+=2;

            return a1 << 4 | a2;

        }

        public int parseErrors(ref List<byte> ls, ref int idx)
        {
            // format: F12
            const int len = 3;

            if( ls.Count < idx + len - 1)
                return -1;

            if (ls[idx] != 'F')
            {
                idx++;
                return -2;
            }

            byte eol = ls[idx + len];
            if (eol != '\r')
            {
                idx++;
                return -3;
            }
                
            int a1 = char2int(ls[++idx]);
            int a2 = char2int(ls[++idx]);

            idx++;

            return a1 << 4 | a2;
        }

        public int parseErrors2(ref List<byte> ls, ref int idx)
        {
            // format: F12
            const int len = 3;

            if (ls.Count < idx + len - 1)
                return -1;

            if (ls[idx] != 'E')
            {
                idx++;
                return -2;
            }

            byte eol = ls[idx + len];
            if (eol != '\r')
            {
                idx++;
                return -3;
            }

            int a1 = char2int(ls[++idx]);
            int a2 = char2int(ls[++idx]);

            idx++;

            return a1 << 4 | a2;
        }

        // message parse result
        private enum parseResult
        {
            parseOk,            // done
            parseBuffTooLow,    // bytes num is not enought, wait for the next data frame
            parseWrongParam,    // param is wrong, try another one
            parseWrongFormat,   // data format mismatch
            parseNoTimeStamp,   // there is no timestamp
            parseFiltered,      // message has been filtered
        };

        // parse message to string before send it to the device
        public static string parseCanMessageForSend (canMessage2 msg, bool remote)
        {
            // format
            // tiiiLDDDDDDDDDDDDDDDD
            string res = string.Empty;

            // header
            if (remote)
                res += msg.Id.Is29bit ? "R" : "r";
            else
                res += msg.Id.Is29bit ? "T" : "t";

            // id
            int idLen = msg.Id.Is29bit ? 8 : 3;
            uint id = (uint)msg.Id.Id;

            for( int i = idLen - 1; i >= 0; i-- )
            {
                // the oldest byte should be first
                uint tmp = (id >> (4 * i)) & (uint)0x0F;
                res += int2char((byte)tmp);
            }

            // dlc
            res += int2char(msg.Id.Dlc);

            // data (std mode only)      
            for( int i = 0; i < msg.Id.Dlc && !remote; i++)
            {
                int b = msg.Data[i];
                int b1 = b & 0x0F;
                int b2 = b >> 4;

                char ch1 = int2char(b1);
                char ch2 = int2char(b2);

                res += ch2;
                res += ch1;
            }

            return res;
        }

        private int ts_prev = -1;
        private int ts_offset = -1;
        private long ts_unix_prev = -1;
        private int ts_mul = 0;

        public void reset()
        {
            ts_prev = -1;
            ts_offset = -1;
            ts_unix_prev = -1;
            ts_mul = 0;
        }

        private parseResult parseCanMessage(ref List<byte> ls, ref int idx, out canMessage2 msg)
        {
            // reset a message
            msg = null;

            // min length is: marker + 3 id + dlc
            const int minPossibleLen = 5;

            // check the lenght
            if (ls.Count - idx < minPossibleLen)
                return parseResult.parseBuffTooLow;

            // get a marker
            byte marker = ls[idx];

            int frameIdLen = 0;
            bool is29bitId = false;
            if (marker == 't')
            {    
                frameIdLen = 3;         // 11 bit message
            }
            else if (marker == 'T')     // 29 bit message
            {
                frameIdLen = 8;         // marker + 8b can id + dlc + dlc*2 + 4b timestamp (optionally) + endbyte       
                is29bitId = true;
            }
            else
                return parseResult.parseWrongParam;

            int posCanId = 1;
            int posDlc = posCanId + frameIdLen;
            int posData = posDlc + 1;

            if (ls.Count - idx <= posDlc)
                return parseResult.parseBuffTooLow;

            // get dlc
            int dlc = char2int(ls[posDlc + idx]);
            if (dlc < 0 || dlc > 8)
                return parseResult.parseWrongFormat;

            // calc expecting frame len and try to get eof
            // marker + can id + dlc + dlc*2 data + 4b timestamp + endbyte
            int expLen = 1 + frameIdLen + 1 + dlc * 2 + 4 + 1;
            if (ls.Count - idx < expLen)
                return parseResult.parseBuffTooLow;
            if (ls[expLen - 1 + idx] != '\r')
            {
                // it is possible that timestamp is disabled
                // check it
                int lenWithNoTs = expLen - 4;
                if (ls[lenWithNoTs - 1 + idx] == '\r')
                {
                    //ls.RemoveRange(0, lenWithNoTs);
                    idx += lenWithNoTs;
                    return parseResult.parseNoTimeStamp;
                }

                return parseResult.parseWrongFormat;
            }
            // the frame looks great, parse it
            int canId = 0;
            for (int i = 0; i < frameIdLen; i++)
                canId |= char2int(ls[posCanId + i + idx]) << ((frameIdLen - 1 - i) * 4);


            if (CanFilter.Contains(canId, dlc, is29bitId))
            {
                idx += expLen;
                return parseResult.parseFiltered;
            }

            byte[] data = new byte[dlc];
            for (int i = 0; i < dlc; i++)
            {
                int b = char2int(ls[posData + idx + i * 2]) << 4 | char2int(ls[posData + idx + i * 2 + 1]);
                data[i] = (byte)b;
            }

            int posTs = posData + dlc * 2;
            int ts = 0;
            for (int i = 0; i < 4; i++)
                ts |= char2int(ls[posTs + i + idx]) << ((4 - 1 - i) * 4);


            // there is a bug with timestamps: 
            // t 1 0 0 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2 5 F E
            // t 1 0 0 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2 5 0 0
            // t 1 0 0 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2 6 0 0
            // t 1 0 0 8 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 0 2 6 0 1

            // timestamp correction
            if (ts_prev >= 0 && ts < ts_prev)
            {
                if (((ts & 0x00FF) == 0) && ((ts & 0xF000) == (ts_prev & 0xF000)))
                {
                    // looks like correction is required
                    // how much should we add? 0x100
                    int tmp = ts;
                    ts += 0x100;
                    //Debug.WriteLine(string.Format("Ts correction: prev {0}, cur {1}, new {2}", 
                    //    ts_prev, tmp, ts));
                }
            }
            
            // create a message
            msg = new canMessage2(canId, is29bitId, data, ts);

            // calculate an absolute ts value
            int ts_since_start = 0;
            long now = DateTimeOffset.Now.ToUnixTimeMilliseconds();

            if (ts_unix_prev > 0)
            {
                ts_since_start = ts - ts_offset;
                ts_since_start += 60000 * ts_mul;

                long unix_diff = now - ts_unix_prev;
                // update mul
                if (unix_diff > 60000)
                {
                    while (unix_diff > 60000)
                    {
                        ts_mul++;
                        ts_since_start += 60000;
                        unix_diff -= 60000;
                    }
                }
                else if (ts < ts_prev)
                {
                    ts_mul++;
                    ts_since_start += 60000;
                }
            } 
            else
            {
                ts_offset = ts;
            }

            // set
            msg.timestamp_absolute = ts_since_start;

            // update 
            ts_unix_prev = now;
            ts_prev = ts;

            //Debug.WriteLine(string.Format("TS = {0}, can = {1}", ts_since_start, ts));

            // remove bytes
            idx += expLen;
            return parseResult.parseOk;
        }

        public bool sent = false;
        // test implementation
        public void parseBufferFast
            ( List<byte> dataIn, ConcurrentBag<canMessage2> dataOut, int maxMsgs = -1)
        {
            bool finish = false;
            int counter = 0;
            int dataIdx = 0;

            while (dataIn.Count > dataIdx && false == finish)
            {
                byte b = dataIn[dataIdx];
                while( dataIn.Count > dataIdx && b != 't' && b != 'T' && b != 'F' && b != 'E' && b != 'G' && b != '\r' && b != '\b')
                    b = dataIn[dataIdx++];
                /*// skip all the bytes untill the marker is received
                while (dataIn.Count > dataIdx && dataIn[dataIdx] != 't' && dataIn[dataIdx] != 'T' && 
                    dataIn[dataIdx] != 'F' && dataIn[dataIdx] != 'E' && dataIn[dataIdx] != 'G' && dataIdx[dat)
                    dataIdx++;
                */
                if (dataIdx >= dataIn.Count)
                    break;

                if( b == '\r')
                {
                    //Debug.WriteLine("Sent");
                    sent = true;
                    dataIdx++;
                    continue;
                }

                if (b == '\b')
                {
                    Debug.WriteLine("NOT Sent");
                    dataIdx++;
                    continue;
                }

                if ( dataIn[dataIdx] == 'F' )
                {
                    int err = parse2ByteVal('F', ref dataIn, ref dataIdx);
                    if (-1 == err)
                        break;
                    
                    if ( err > 0)
                    {
                        Debug.WriteLine("F = " + err.ToString("X2"));
                        updateErrorCounters(err);
                        // err = err;
                        //Debug.WriteLine("F = " + err.ToString("X2"));
                    }
                    continue;
                }

                if (dataIn[dataIdx] == 'E')
                {
                    int err = parse2ByteVal('E', ref dataIn, ref dataIdx);
                    if (-1 == err)
                        break;
                    //Debug.WriteLine("E = " + err.ToString("X2"));
                    if (err > 0)
                    {
                        //err = err;
                    }
                    continue;
                }

                if( dataIn[dataIdx] == 'G')
                {
                    int err = parse2ByteVal('G', ref dataIn, ref dataIdx);
                    if (-1 == err)
                        break;
                    // handle
                    RawRegDataHandle(err);

                    if (err > 0)
                    {
                        //Debug.WriteLine("G = " + err.ToString());
                    }

                    continue;
                }

                // message
                canMessage2 msg = null;
                parseResult res = parseCanMessage(ref dataIn, ref dataIdx, out msg);

                if (res == parseResult.parseOk)
                {
                    dataOut.Add(msg);
                    TimestampError = false;
                    if (maxMsgs > 0)
                        if (++counter >= maxMsgs)
                            finish = true;
                }
                else if (res == parseResult.parseNoTimeStamp)
                {
                    TimestampError = true;
                }
                else if (res == parseResult.parseFiltered)
                {
                    // do nothing
                }
                else if (res == parseResult.parseBuffTooLow)
                    finish = true;
                else
                    dataIdx++;  // dataIn.RemoveAt(0);
            }

            // extract the data we'd parsed
            dataIn.RemoveRange(0, dataIdx);
        }

        public enum CanRegisterReq {
            none,
            tx_error_counter,
            rx_error_counter,
        };
        public CanRegisterReq RawRegesterReq = CanRegisterReq.none;


        private canParserErrors canErrors = new canParserErrors();

        private void RawRegDataHandle(int value)
        {
            // check what data are we looking for
            switch (RawRegesterReq) {
                case CanRegisterReq.tx_error_counter:
                    canErrors.tx_err_cnt = value;
                    break;
                case CanRegisterReq.rx_error_counter:
                    canErrors.rx_error_cnt = value;
                    break;
                default:
                    break;
            }

            RawRegesterReq = CanRegisterReq.none;
        }


        public canParserErrors ErrorCounterGet()
        {
            return canErrors;
        }

        public void ErrorCounterClean()
        {
            canErrors.clean();
        }

        private void updateErrorCounters(int raw)
        {
            // Bit 0 Not used
            // Bit 1 Not used
            // Bit 2 Error warning
            // Bit 3 Data overrun

            // Bit 4 Not used
            // Bit 5 Error passive
            // Bit 6 Arbitration Lost
            // Bit 7 Bus error

            if ((raw & 0x04) != 0)
                canErrors.err_warn_cnt += 1;
            if ((raw & 0x08) != 0)
                canErrors.data_overrun_cnt += 1;

            if ((raw & 0x20) != 0)
                canErrors.error_passive_cnt += 1;
            if ((raw & 0x40) != 0)
                canErrors.arbitration_lost_cnt += 1;
            if ((raw & 0x80) != 0)
                canErrors.bus_error_cnt += 1;
        }
    }

    public class canParserErrors
    {
        // error flags
        public int err_warn_cnt = 0;
        public int data_overrun_cnt = 0;
        public int error_passive_cnt = 0;
        public int arbitration_lost_cnt = 0;
        public int bus_error_cnt = 0;

        // sja1000 error counters
        public int rx_error_cnt = 0;
        public int tx_err_cnt = 0;

        public void clean()
        {
            err_warn_cnt = 0;
            data_overrun_cnt = 0;
            error_passive_cnt = 0;
            arbitration_lost_cnt = 0;
            bus_error_cnt = 0;

            rx_error_cnt = 0;
            tx_err_cnt = 0;
        }

        public bool empty()
        {
            return err_warn_cnt > 0 ||
                    data_overrun_cnt > 0 ||
                    error_passive_cnt > 0 ||
                    arbitration_lost_cnt > 0 ||
                    bus_error_cnt > 0 ||
                    rx_error_cnt > 0 ||
                    tx_err_cnt > 0;
        }

        public bool isBusOff()
        {
            return rx_error_cnt == 0 && tx_err_cnt == 127;
        }
    };
}
// end of the namespace
//-----------------------------------------------------------------------
