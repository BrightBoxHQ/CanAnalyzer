using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace nsScriptParser
{
    #region results

    // list of supported commans
    public enum cmdType
    {
        send,           // send a CAN message
        sleep,          // sleep
        comment,        // comment
        trace,          // print a message
        breakpoint,     // breakpoint
        read,           // read a CAN message
        udsReqSend,     // send a request (std or BMW)
    }

    // command: Send a CAN message
    public class cmdSendParams
    {
        public int Id  { set; get; }
        public bool Is29BitId { set; get; }
        public string[] sData { set; get; }

        public cmdSendParams(int id, string[] sDataIn, bool is29b = false)
        {
            Id = id;
            sData = sDataIn;
            Is29BitId = is29b;
        }
    }

    // command: Read a CAN message
    public class cmdReadParams
    {
        // timeout value
        public readonly int Timeout;
        // CAN message id
        public readonly canAnalyzer.canMessageId Id;
        // data mask strings
        public readonly string [] DataMask;
        public readonly string DataMaskString;
        // save as a variable 
        public readonly bool UseAsVar;
        // variable idx
        public readonly int VarIdx = -1;
        // min allowed DLC (data len)
        public readonly int DlcMin;

        private readonly bool IsStrictIdValue;
        private readonly string IdFormat;
        private readonly bool AnyID;

        // constructor
        public cmdReadParams(string id, string[] dataMask, bool is29b = false, int tmo = 1000,
                             int dlcMin = -1, bool asVar = false, int varIdx = -1)
        {
            // params
            Id = new canAnalyzer.canMessageId(1, dataMask.Length, is29b);
            Timeout = tmo;
            UseAsVar = asVar;
            VarIdx = varIdx;
            DlcMin = dlcMin;

            IsStrictIdValue = false;
            IdFormat = id;
            AnyID = IdFormat.Trim() == "***";

            // mask
            DataMask = new string[Id.Dlc];
            for (int i = 0; i < Id.Dlc; i++)
                DataMask[i] = dataMask[i];

            // mask
            foreach (var s in DataMask)
                DataMaskString += s + ",";
            DataMaskString = DataMaskString.Remove(DataMaskString.Length - 1, 1);
        }

        // constructor
        public cmdReadParams(int id, string[] dataMask, bool is29b = false, int tmo = 1000, 
                             int dlcMin = -1, bool asVar = false, int varIdx = -1)
        {
            // params
            Id = new canAnalyzer.canMessageId(id, dataMask.Length, is29b);
            Timeout = tmo;
            UseAsVar = asVar;
            VarIdx = varIdx;
            DlcMin = dlcMin;

            IsStrictIdValue = true;
            IdFormat = string.Empty;
            AnyID = false;

            // mask
            DataMask = new string[Id.Dlc];
            for (int i = 0; i < Id.Dlc; i++)
                DataMask[i] = dataMask[i];

            // mask
            foreach (var s in DataMask)
                DataMaskString += s + ",";
            DataMaskString = DataMaskString.Remove(DataMaskString.Length-1, 1);
        }

        // do check the id
        public bool Check(canAnalyzer.canMessageId id)
        {
            // just compare 2 values
            if (IsStrictIdValue)
            {
                if (DlcMin == -1)
                    return id.Equals(Id);
                else
                    return id.Id == Id.Id && id.Id >= DlcMin;
            }
            else
            {
                if (id.Is29bit == Id.Is29bit)
                {
                    if (AnyID)
                        return true;
                }
            }

            return false;
        }
    }

    // command: trace (print)
    public class cmdTrace
    {
        public readonly string Format;
        public readonly string[] ArgList;

        public cmdTrace(string format, string[] args)
        {
            Format = format;
            ArgList = new string[args.Length];
            for (int i = 0; i < args.Length; i++)
                ArgList[i] = args[i];
        }
    }

    // command: request
    public class cmdUdsReqParams
    {
        public int IdReq { set; get; }
        public int IdResp { set; get; }

        public bool Is29BitId { set; get; }
        public string[] sData { set; get; }

        // save as a variable 
        public readonly bool UseAsVar;
        // variable idx
        public readonly int VarIdx = -1;
        // is BMW mode?
        public readonly bool IsBmw = false;
        public readonly byte BmwEcuId = 0;

        public cmdUdsReqParams(int req_id, int resp_id, string[] sDataIn, bool is29b, bool asVar, int varIdx)
        {
            IdReq = req_id;
            IdResp = resp_id;
            sData = sDataIn;
            Is29BitId = is29b;
            UseAsVar = asVar;
            VarIdx = varIdx;
            IsBmw = false;
            BmwEcuId = 0;
        }

        public cmdUdsReqParams(byte bmw_ecu, string[] sDataIn, bool asVar, int varIdx)
        {
            IsBmw = true;
            BmwEcuId = bmw_ecu;
            sData = sDataIn;
            UseAsVar = asVar;
            VarIdx = varIdx;

            IdReq = 0;
            IdResp = 0;
            Is29BitId = false;
        }
    }

    // parsed data
    public class scriptParserCmd
    {
        public cmdType cmd { set; get; } // check the type and use the required data

        public cmdSendParams paramsSend { set; get; }
        public cmdReadParams paramsRead { set; get; }
        public cmdTrace      paramsTrace { set; get; }
        public cmdUdsReqParams paramsUdsReq { set; get; }
        public int sleepTmo { set; get; }
        public bool error { set; get; }
        public string strCmd { set; get; }
    }

    #endregion

    // parser
    class scriptParser
    {
        private const string cmdSend = "send(";
        private const string cmdSend29 = "send29(";
        private const string cmdSend11 = "send11(";

        // CAN message to script
        static public string message2string(canAnalyzer.canMessage2 msg)
        {
            string res = string.Empty;

            string sData = msg.GetDataString(", ", "0x");
            res = string.Format("{0} 0x{1},  {2} );",
                msg.Id.Is29bit ? cmdSend29 : cmdSend,
                msg.Id.GetIdAsString(), 
                sData);

            return res;
        }

        // CAN message to script
        static public string message2string(List<canAnalyzer.canMessage2> ls)
        {
            string res = string.Empty;
            foreach (var m in ls)
                res += message2string(m) + Environment.NewLine;
            return res;
        }
        
        // parser: comment
        static public bool isComment(string s)
        {
            // format the text
            // lower
            s = s.ToLower();
            // remove all the symbols we do not need
            s = s.Replace(" ", string.Empty);
            s = s.Replace("\r", string.Empty);
            s = s.Replace("\n", string.Empty);
            s = s.Replace("\t", string.Empty);

            return s.IndexOf("//") == 0 || string.IsNullOrEmpty(s);
        }

        // parser: sleep message
        static private scriptParserCmd parseSleep(string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            if (funcName == "sleep")
            {
                int val = 0;
                if (canAnalyzer.Tools.tryParseInt(args, out val))
                {
                    // complete
                    cmd.error = false;
                    cmd.sleepTmo = val;
                    cmd.cmd = cmdType.sleep;
                }
            }
            return cmd;
        }

        // parser: breakpoint
        static private scriptParserCmd parseBreakpoint(string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            if(funcName == "breakpoint" && string.IsNullOrEmpty(args))
            {
                cmd.error = false;
                cmd.cmd = cmdType.breakpoint;
            }

            return cmd;
        }

        // parser: send a CAN message
        static private scriptParserCmd parseSend(string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            // remove spaces
            args = args.Replace(" ", "");

            if (funcName == "send" || funcName == "send11" || funcName == "send29")
            {
                bool is29bit = funcName.Contains("29");

                string[] paramList = args.Split(',');
                if (paramList.Length >= 2 && paramList.Length <= 9)
                {
                    // id, data (1..8)
                    int canID = -1;
                    int dlc = paramList.Length - 1;
                    string[] sData = new string[dlc];
                    bool failed = false;
                    object calc = null;

                    // ecalute: can id
                    //int val = -1;
                    calc = canAnalyzer.MyCalc.evaluate(paramList[0]);

                    failed = (calc == null) || 
                        (Convert.ToInt32(calc) > canAnalyzer.canMessageId.GetMaxId(is29bit));

                    if (!failed)
                    {
                        canID = canAnalyzer.MyCalc.objToI32(calc);
                    }
                    
                    // evaluate: data bytes
                    for (int i = 1; i < paramList.Length && !failed; i++)
                    {
                        string param = paramList[i];
                        string s_byte = null;
                        calc = canAnalyzer.MyCalc.evaluate(param);
                        failed = calc == null;

                        // just append
                        if (!failed)
                        {
                            byte b = canAnalyzer.MyCalc.objToByte(calc);
                            s_byte = b.ToString();
                        }
                        else
                        {
                            string s_tmp = null;
                            // may contain variables
                            if (param.Contains("var"))
                            {
                                const string pattern = @"var\d\[\s*\d\s*\]";
                                s_tmp = Regex.Replace(param, pattern, "1", RegexOptions.Singleline);
                            }
                            // may contain arrays
                            if (param.Contains("arr"))
                            {
                                const string pattern = @"arr\d\[\s*\d\s*\]";
                                s_tmp = Regex.Replace(param, pattern, "1", RegexOptions.Singleline);
                            }
                            // evaluate
                            if (s_tmp != null)
                            {
                                calc = canAnalyzer.MyCalc.evaluate(s_tmp);
                                failed = calc == null;
                                // append
                                if (!failed)
                                    s_byte = param.ToString();
                            }
                        }

                        // append
                        if (!failed)
                        {
                            if (s_byte != null)
                                sData[i - 1] = s_byte;
                            else
                                failed = true;
                        }
                    }

                    if (!failed)
                    {
                        cmd.cmd = cmdType.send;
                        cmd.paramsSend = new cmdSendParams(canID, sData, is29bit);
                        cmd.error = false;
                    }
                }
            }

            return cmd;
        }

        // parser: extract the timeout value
        static private int parseReadTimeout(string sval)
        {
            int res = -1;

            if (sval.Trim() == "inf")
            {
                return res = int.MaxValue;
            }
            else
            {
                var ob = canAnalyzer.MyCalc.evaluate(sval);
                if (ob != null)
                    res = canAnalyzer.MyCalc.objToI32(ob);
            }

            return res;
        }

        
        static private bool parseReadId(string sval, bool is29bit, out int canId, out string canIdFormat)
        {
            // reset
            canId = -1;
            canIdFormat = string.Empty;

            int canIdMax = canAnalyzer.canMessageId.GetMaxId(is29bit);

            // calculate
            var ob = canAnalyzer.MyCalc.evaluate(sval);
            if (ob != null)
            {
                int tmp = canAnalyzer.MyCalc.objToI32(ob);
                
                // check the borders
                if (tmp > 0 && tmp <= canIdMax)
                {
                    canIdFormat = sval;
                    canId = tmp;
                } 
            } else
            {
                /*
                if (sval.Contains('*'))
                {
                    // case 1: string == *** so just use any value
                    if (sval == "***")
                    {
                        canIdFormat = sval;
                    } 
                    else
                    {
                        // case 2: like 0x7*1
                        // make sure we can calculate it
                        ob = canAnalyzer.MyCalc.evaluate(sval.Replace('*', '0'));
                        if (ob != null)
                        {
                            int tmp = canAnalyzer.MyCalc.objToI32(ob);
                            if (tmp > 0 && tmp <= canIdMax)
                                canIdFormat = sval;
                        }
                           
                    }
                }
                */
            }

            return canIdFormat != string.Empty;
        }

        // format: read(0x316, 1000, **, 0x*F, 0x33, ***);
        //         var2 = read(0x123, ***);
        static private scriptParserCmd parseRead(string inString, string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            if (funcName == "read" || funcName == "read11" || funcName == "read29")
            {
                string inString_original = inString;

                // remove spaces
                args = args.Replace(" ", "");
                inString = inString.Replace(" ", "");

                bool is29bit = funcName.Contains("29");
                bool asVar = false;
                int varIdx = -1;

                // should we save a response as a variable?
                for (int i = 0; i <= 9 && !asVar; i++)
                {
                    string txt = string.Format("var{0}=", i);
                    asVar = inString.IndexOf(txt) == 0;
                    if (asVar)
                        varIdx = i;
                }

                // parse all the arguments
                string[] paramList = args.Split(',');
                if (paramList.Length > 2 && paramList.Length <= 10)
                {
                    // id, tmo, data mask (1..8)
                    int canID = -1;
                    string canIdPattern = string.Empty;
                    int tmo = -1;
                    string mask = args.Substring(paramList[0].Length + paramList[1].Length + 2); // +2 commas
                    string[] dataMask = mask.Split(',');
                    bool failed = false;

                    // get CAN id
                    if (!parseReadId(paramList[0], is29bit, out canID, out canIdPattern))
                    {
                        canID = -1;
                        canIdPattern = string.Empty;
                        failed = true;
                    }
                    // timeout
                    tmo = parseReadTimeout(paramList[1]);
                    if (tmo <= 0)
                        failed = true;

                    int minDLC = -1;
                    for (int i = 0; i < dataMask.Length && -1 == minDLC; i++)
                    {
                        if (dataMask[i].Contains("***"))
                            minDLC = i;
                    }


                    // get a var name
                    /*
                    var regVarName = Regex.Match(inString, @"var\s+(\w+)\s*=");
                    string varName = string.Empty;
                    if (regVarName.Success)
                        varName = regVarName.Groups[1].ToString();
                    */
                    if (!failed)
                    {
                        cmd.cmd = cmdType.read;
                        cmd.paramsRead = new cmdReadParams(canID, dataMask, is29bit, tmo, minDLC, asVar, varIdx);
                        cmd.error = false;
                    }
                }
            }

            return cmd;
        }

        // format:  printf("format", arg1, arg2, ..);
        // example: printf("rpm = %d", var[0] << 8 | var[1]);
        static private scriptParserCmd parseTrace(string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            if (funcName == "printf")
            {
                cmd.cmd = cmdType.trace;
                string[] argList = new string[args.Length - 1];

                var regFormat = Regex.Match(args, "\".*\"");
                if (regFormat.Success)
                {
                    string s = args.Replace(regFormat.Value, "");
                    s = s.Replace(" ", "");
                    if (s.Length > 0 && s[0] == ',')
                        s = s.Remove(0, 1);

                    string format = regFormat.Value;

                    List<string> ls = new List<string>(s.Split(','));

                    for (int i = 0; i < ls.Count; i++)
                        if (string.IsNullOrEmpty(ls[i]))
                            ls.RemoveAt(i);

                    // %x, %02x == X, X2
                    // %d
                    // %f

                    // List<string> lsFormat = new List<string>();


                    // check skobochki
                    foreach (string str in ls)
                    {
                        int open = 0, close = 0;
                        open = str.ToCharArray().Where(i => i == '(').Count();
                        close = str.ToCharArray().Where(i => i == ')').Count();
                        if (open != close)
                            return cmd;
                    }

                    // extra format parser
                    // {s0-s6}
                    // {0-6}
                    var regFormatVarsAsArray  = Regex.Matches(format, @"{(\d+)-(\d+)}");
                    foreach (Match v in regFormatVarsAsArray)
                    {
                        string s1 = v.Groups[1].ToString();
                        string s2 = v.Groups[2].ToString();
                        int i1 = Convert.ToInt32(s1);
                        int i2 = Convert.ToInt32(s2);

                        if(i2 > i1)
                        {
                            string newFormat = string.Empty;
                            for (int i = i1; i <= i2; i++) {
                                newFormat += "{" + i.ToString() + "}";
                            }
                            format = format.Replace(v.Groups[0].ToString(), newFormat);
                        }
                    }

                    var regFormatVarsStrAsArray = Regex.Matches(format, @"{s(\d+)-s(\d+)}");
                    foreach (Match v in regFormatVarsStrAsArray)
                    {
                        string s1 = v.Groups[1].ToString();
                        string s2 = v.Groups[2].ToString();
                        int i1 = Convert.ToInt32(s1);
                        int i2 = Convert.ToInt32(s2);

                        if (i2 > i1)
                        {
                            string newFormat = string.Empty;
                            for (int i = i1; i <= i2; i++)
                            {
                                newFormat += "{s" + i.ToString() + "}";
                            }
                            format = format.Replace(v.Groups[0].ToString(), newFormat);
                        }
                    }

                    cmd.cmd = cmdType.trace;
                    cmd.paramsTrace = new cmdTrace(format, ls.ToArray());
                    cmd.error = false;
                }
            }


            return cmd;
        }

        // format:  uds_req(req_id, resp_id, data..);
        // example: buff2 = uds_req(0x7E0, 0x7E8, 0x22, 0xF1, 0x90);
        static private scriptParserCmd parseSendUds(string inString, string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            // remove spaces
            args = args.Replace(" ", "");

            if (funcName == "uds_req" || funcName == "uds_req11" || funcName == "uds_req29")
            {
                bool is29bit = funcName.Contains("29");
                bool asVar = false;
                int varIdx = -1;

                // remove spaces
                args = args.Replace(" ", "");
                inString = inString.Replace(" ", "");

                // should we save a response as a variable?
                for (int i = 0; i < 10 && !asVar; i++)
                {
                    string txt = string.Format("arr{0}=", i);
                    asVar = inString.IndexOf(txt) == 0;
                    if (asVar)
                        varIdx = i;
                }

                string[] paramList = args.Split(',');
                if (paramList.Length > 2 && paramList.Length <= 9)
                {
                    // req_id, resp_id, data (1..7)
                    int req_id = 0, resp_id = 0;
                    int dlc = paramList.Length - 1;
                    List<string> sData = new List<string>();
                    bool failed = false;
                    object calc = null;

                    // ecalute: req id
                    calc = canAnalyzer.MyCalc.evaluate(paramList[0]);
                    failed = (calc == null) ||
                        (Convert.ToInt32(calc) > canAnalyzer.canMessageId.GetMaxId(is29bit));
                    if (!failed)
                        req_id = canAnalyzer.MyCalc.objToI32(calc);

                    // ecalute: resp id
                    calc = canAnalyzer.MyCalc.evaluate(paramList[1]);
                    failed = (calc == null) ||
                        (Convert.ToInt32(calc) > canAnalyzer.canMessageId.GetMaxId(is29bit));
                    if (!failed)
                        resp_id = canAnalyzer.MyCalc.objToI32(calc);

                    // evaluate: data bytes
                    for (int i = 2; i < paramList.Length && !failed; i++)
                    {
                        string param = paramList[i];
                        string s_byte = null;
                        calc = canAnalyzer.MyCalc.evaluate(param);
                        failed = calc == null;

                        // just append
                        if (!failed)
                        {
                            byte b = canAnalyzer.MyCalc.objToByte(calc);
                            s_byte = b.ToString();
                        }
                        else
                        {
                            string s_tmp = null;
                            // may contain variables
                            if (param.Contains("var"))
                            {
                                const string pattern = @"var\d\[\s*\d\s*\]";
                                s_tmp = Regex.Replace(param, pattern, "1", RegexOptions.Singleline);
                            }
                            // evaluate
                            if (s_tmp != null)
                            {
                                calc = canAnalyzer.MyCalc.evaluate(s_tmp);
                                failed = calc == null;
                                // append
                                if (!failed)
                                    s_byte = param.ToString();
                            }
                        }

                        // append
                        if (!failed)
                        {
                            if (s_byte != null)
                                sData.Add(s_byte);
                            else
                                failed = true;
                        }
                    }

                    if (!failed)
                    {
                        cmd.cmd = cmdType.udsReqSend;
                        cmd.paramsUdsReq = new cmdUdsReqParams(req_id, resp_id, sData.ToArray(), is29bit, asVar, varIdx);
                        cmd.error = false;
                    }
                }
            }

            return cmd;
        }

        // format:  bmw_req(ecu_id, data..);
        // example: buff2 = bmw_req(0x60, 0x22, 0xF1, 0x90);
        static private scriptParserCmd parseSendBmw(string inString, string funcName, string args)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;

            // remove spaces
            args = args.Replace(" ", "");

            if (funcName == "bmw_req" || funcName == "bmw_req11")
            {
                bool asVar = false;
                int varIdx = -1;

                // remove spaces
                args = args.Replace(" ", "");
                inString = inString.Replace(" ", "");

                // should we save a response as a variable?
                for (int i = 0; i < 10 && !asVar; i++)
                {
                    string txt = string.Format("arr{0}=", i);
                    asVar = inString.IndexOf(txt) == 0;
                    if (asVar)
                        varIdx = i;
                }

                string[] paramList = args.Split(',');
                if (paramList.Length > 1 && paramList.Length <= 5)
                {
                    // ecu_id, data (1..5)
                    int ecu_id = 0;
                    int dlc = paramList.Length - 1;
                    List<string> sData = new List<string>();
                    bool failed = false;
                    object calc = null;

                    // ecalute: req id
                    calc = canAnalyzer.MyCalc.evaluate(paramList[0]);
                    failed = calc == null;
                    if (!failed)
                        ecu_id = canAnalyzer.MyCalc.objToI32(calc);
                    if (!failed)
                        failed = ecu_id == 0 || ecu_id > 0xFF;

                    // evaluate: data bytes
                    for (int i = 1; i < paramList.Length && !failed; i++)
                    {
                        string param = paramList[i];
                        string s_byte = null;
                        calc = canAnalyzer.MyCalc.evaluate(param);
                        failed = calc == null;

                        // just append
                        if (!failed)
                        {
                            byte b = canAnalyzer.MyCalc.objToByte(calc);
                            s_byte = b.ToString();
                        }
                        else
                        {
                            string s_tmp = null;
                            // may contain variables
                            if (param.Contains("var"))
                            {
                                const string pattern = @"var\d\[\s*\d\s*\]";
                                s_tmp = Regex.Replace(param, pattern, "1", RegexOptions.Singleline);
                            }
                            // evaluate
                            if (s_tmp != null)
                            {
                                calc = canAnalyzer.MyCalc.evaluate(s_tmp);
                                failed = calc == null;
                                // append
                                if (!failed)
                                    s_byte = param.ToString();
                            }
                        }

                        // append
                        if (!failed)
                        {
                            if (s_byte != null)
                                sData.Add(s_byte);
                            else
                                failed = true;
                        }
                    }

                    if (!failed)
                    {
                        cmd.cmd = cmdType.udsReqSend;
                        cmd.paramsUdsReq = new cmdUdsReqParams((byte)(ecu_id), sData.ToArray(), asVar, varIdx);
                        cmd.error = false;
                    }
                }
            }

            return cmd;
        }

        // parse a script string
        static public scriptParserCmd parseString(string s)
        {
            // create a new cmd
            scriptParserCmd cmd = new scriptParserCmd();
            cmd.error = true;
            cmd.strCmd = s;

            // is comment?
            if (isComment(s))
            {
                cmd.cmd = cmdType.comment;
                cmd.error = false;
                return cmd;
            }

            // try to parse
            var reg = Regex.Match(s, @"\b(\w+)\s*\({1}\s*(.*)\)\s*;");
            if (reg.Success)
            {
                string funcName = reg.Groups[1].Value.ToLower();
                string funcParams = reg.Groups[2].Value;

                // try to parse as sleep cmd
                cmd = parseSleep(funcName, funcParams);
                // try to parse as send cmd
                if (cmd.error)
                    cmd = parseSend(funcName, funcParams);
                // try to parse as read cmd
                if (cmd.error)
                    cmd = parseRead(s, funcName, funcParams);
                // try to parse as trace cmd
                if (cmd.error)
                    cmd = parseTrace(funcName, funcParams);
                // breakpoint
                if (cmd.error)
                    cmd = parseBreakpoint(funcName, funcParams);
                // try to parse as send uds request cmd
                if (cmd.error)
                    cmd = parseSendUds(s, funcName, funcParams);
                if (cmd.error)
                    cmd = parseSendBmw(s, funcName, funcParams);
            }

            cmd.strCmd = s;
            return cmd;
        }

        // remove comments
        static private string removeComments(string txt)
        {
            // block test
            var blockComments = @"/\*(.*?)\*/";
            var lineComments = @"//(.*?)\r?\n";
            var strings = @"""((\\[^\n]|[^""\n])*)""";
            var verbatimStrings = @"@(""[^""]*"")+";

            string noComments = Regex.Replace(txt,
                blockComments + "|" + lineComments + "|" + strings + "|" + verbatimStrings,
                 me => {
                    if (me.Value.StartsWith("/*") || me.Value.StartsWith("//"))
                        return me.Value.StartsWith("//") ? Environment.NewLine : "";
                        // Keep the literal strings
                    return me.Value;
                 },
                RegexOptions.Singleline
            );
            // empty lines
            noComments = Regex.Replace(noComments, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            return noComments;
        }

        // remove if - else
        static private string removeIfElse(string txt)
        {
            var block = @"\#if\s*(\d+)(.*?)\#else(.*?)\#endif";

            var me = Regex.Matches(txt, block, RegexOptions.Singleline);
            if (me.Count > 0)
            {
                foreach (Match match in me)
                {
                    string full = match.Groups[0].ToString();
                    string sval = match.Groups[1].ToString();
                    string content_if = match.Groups[2].ToString();
                    string content_else = match.Groups[3].ToString();

                    int val = 0;
                    if (!canAnalyzer.Tools.tryParseInt(sval, out val))
                    {
                        // skip
                    }
                    else
                    {
                        txt = txt.Replace(full, val == 0 ? content_else : content_if);
                    }
                }
            }

            // empty lines
            txt = Regex.Replace(txt, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            return txt;
        }

        // remove if 0
        static private string removeIf0(string txt)
        {
            var block = @"#if\s*(\d+)(.*?)#endif";

            var me = Regex.Matches(txt, block, RegexOptions.Singleline);
            if (me.Count > 0)
            {
                foreach (Match match in me)
                {
                    string full = match.Groups[0].ToString();
                    string sval  = match.Groups[1].ToString();
                    string content = match.Groups[2].ToString();
                    int val = 0;
                    if (!canAnalyzer.Tools.tryParseInt(sval, out val))
                    {
                        // skip
                    } else
                    {
                        txt = txt.Replace(full, val == 0 ? Environment.NewLine : content);
                    }
                }
            }

            // empty lines
            txt = Regex.Replace(txt, @"^\s+$[\r\n]*", string.Empty, RegexOptions.Multiline);

            return txt;
        }

        // do replace
        static private string replace (string txt, string from, string to)
        {
            // string format = @"[\W]{1}(" + from + @")[\W]";
            string format = @"[\W{1}](" + from + @")";  // non-word symbol + the data to be replaced
            var m = Regex.Matches(txt, format);
            foreach (Match v in m)
            {
                string full = v.Groups[0].ToString();
                string match = v.Groups[1].ToString();
                int pos = txt.IndexOf(full);
                while (pos >= 0)
                {
                    txt = txt.Remove(pos + 1, match.Length);
                    txt = txt.Insert(pos + 1, to);
                    pos = txt.IndexOf(full);
                }
            }
            return txt;
        }

        // apply defines
        static private string applyDefines (string txt)
        {
            // replace the types
            txt = replace(txt, "U8_t", "int");
            txt = replace(txt, "uint8_t", "int");

            // parse int
            //string pattern = @"\bint\s+(\w+)\s*=\s*(\w+)\s*;";       
            string pattern = @"\bint\s+(\w+)\s*=\s*([a-zA-Z0-9]+.*);";
            var varInt = Regex.Matches(txt, pattern);

            bool success = true;

            while (success == true)
            {

                var reg = Regex.Match(txt, pattern);
                success = reg.Success;
                if (reg.Success)
                {
                    string svar = reg.Groups[1].ToString();
                    string sval = reg.Groups[2].ToString();

                    txt = txt.Replace(reg.Groups[0].ToString(), string.Empty);

                    // replace if possible
                    var ob = canAnalyzer.MyCalc.evaluate(sval);
                    if (ob != null)
                    {
                        int new_val = canAnalyzer.MyCalc.objToI32(ob);
                        txt = replace(txt, svar, new_val.ToString());
                    }
                }
            }
  
            return txt;
        }

        // hex to int - obsolete
        /*
        static private string hex2int (string txt)
        {
            string pattern = @"0x[0-9a-fA-F]+";
            var varInt = Regex.Matches(txt, pattern);

            foreach (Match s in varInt)
            {
                string hex = s.Groups[0].ToString();
                int val = 0;
                // replace if possible
                if (canAnalyzer.Tools.tryParseInt(hex, out val))
                    txt = replace(txt, hex, val.ToString());
            }
            return txt;
        }
        */
        
        // connect the string
        static public string connectStrings (string txt)
        {
            string res = string.Empty;

            string[] ls = txt.Split('\n');

            for(int i = 0; i < ls.Length; i++)
            {
                string s = ls[i].TrimEnd();
                if (s.EndsWith("\\"))
                {
                    s = s.Remove(s.Length - 1, 1);
                    res += s;
                }
                else
                    res += ls[i] + "\n";
            }

            return res;
        }
        
        // parser
        static public scriptParserCmd[] parseText (string txt)
        {
            // replace '\t'
            txt = txt.Replace('\t', ' ');
            // remove comments
            txt = removeComments(txt);
            // remove #if 0 else #endif
            txt = removeIfElse(txt);
            // remove #if 0
            txt = removeIf0(txt);
            // defines, as int = ..
            txt = applyDefines(txt);
            // connect strings
            txt = connectStrings(txt);

            // split
            string[] ls = txt.Split('\n');
            for (int i = 0; i < ls.Count(); i++)
                ls[i] += "\n";

            // parse
            scriptParserCmd[] cmd = new scriptParserCmd[ls.Length];
            for (int i = 0; i < ls.Length; i++)
                cmd[i] = parseString(ls[i]);

            return cmd;
        }
    }
}


