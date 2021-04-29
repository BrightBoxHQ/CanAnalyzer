using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Diagnostics;
using System.Threading;

namespace canAnalyzer
{
    class canUdsParser
    {
        // data fields
        #region Fields
        
        // arguments
        private canMessage2 m_req_msg;
        private canMessageId m_resp_id;
        private CanMessageSendTool m_can_tool;

        // internal states
        private bool m_gotAtLeastOneResponse;                   // have we received the 1st response frame
        private bool m_flow_sent;                               // have we sent the flow control message?
        private bool m_finished;                                // have we received all the expected data?
        private int m_next_nibble;                              // next expected nibble
        private int m_expected_len;                             // expected len (based on the 1st response)
        private List<byte> m_response_data;                     // received data
        private byte m_expected_service;                        // expected response service (based on the request)

        // timeouts
        private long m_rx_timeout = 0;                          // rx timeout (unix)
        private long rx_timeout_1_single = 0;                   // 0 - use default
        private long rx_timeout_2_multi = 0;                    // 0 - use default
        private readonly long rx_timeout_default_single = 100;  // 50 + 50 just in case
        private readonly long rx_timeout_default_multi = 200;   // 150 + 50 just in case
        
        // negative response
        private readonly long rx_timeout_negative_wait = 5100;  // 5000 + 100 just in case
        private readonly byte neg_resp_code = 0x7F;             // 0x7F - negative response
        private readonly byte neg_reponse_wait_code = 0x78;     // wait

        // shoud we add dummy-bytes for txed messages?
        private bool tx_add_extra_bytes = true;                 // add dummy bytes to get DLC = tx_message_len_max
        private byte tx_add_extra_byte_value = 0x00;            // the byte value is 0
        private readonly int tx_message_len_max = 8;            // DLC = 8

        // prefixes to work with a gateway, as for BMW
        private List<byte> m_req_prefix = null;
        private List<byte> m_resp_prefix = null;

        // interval variable to use unit tests (stun-like)
        private bool m_self_test = false;

        public enum ErrorCode
        {
            Ok,
            InvalidArg,
            TxForbidden,
            TxFailed,
        };

        #endregion

        // public methods
        #region PublicMethods

        // update internal RX timeots, tmo1 - single message, tmo2 - multiframe
        public void SetRxTimeouts(long tmo1, long tmo2)
        {
            rx_timeout_1_single = tmo1;
            rx_timeout_2_multi = tmo2;
        }

        // set config for tx message autocomplete
        // example: we can send flow as '0x30,0,0' or '0x30,0,0,val,val,val,val,val'
        public void SetTxMesageAutocomplete(bool enable, byte val)
        {
            tx_add_extra_bytes = enable;
            tx_add_extra_byte_value = val;
        }

        // get the current timeout value (unix)
        public long GetTimeoutUnix()
        {
            return m_rx_timeout;
        }

        // is the current timeout expired?
        public bool IsTimeoutExpired()
        {
            var now = DateTimeOffset.Now.ToUnixTimeMilliseconds();
            var diff = now - m_rx_timeout;
            return now > m_rx_timeout;
        }

        // is successfully finished (all the expected data received) ?
        public bool IsSuccesfullyFinished()
        {
            return m_finished;
        }

        // returns a num of the expected bytes (based on the 1st received frame)
        public int GetNumOfExpectedBytes()
        {
            return m_expected_len;
        }

        // start UDS
        public ErrorCode sendRequest(CanMessageSendTool can_tool, canMessage2 req, canMessageId resp_id)
        {
            ErrorCode err = ErrorCode.Ok;

            // clean and set
            cleanData();

            // check the arguments
            err = checkStartArgs(can_tool, req, resp_id);

            // apply
            m_can_tool = can_tool;
            m_req_msg = req;
            m_resp_id = resp_id;
            
            // send the message
            if (err == ErrorCode.Ok)
            {
                m_expected_service = createExpectedService(req.Data[0]);

                var uds_req = createRequestMessage();
                
                if (uds_req == null)
                {
                    err = ErrorCode.InvalidArg;
                }
                else
                {
                    if (!sendRequest(uds_req))
                        err = ErrorCode.TxFailed;
                }
            }

            return err;
        }

        // start BMW
        public ErrorCode sendRequestBMW(CanMessageSendTool can_tool, byte ecu_id, List<byte> data)
        {
            const byte gateway_id = 0xF1;
        
            ErrorCode err = ErrorCode.Ok;

            // clean
            cleanData();

            if (data.Count == 0)
                return ErrorCode.InvalidArg;
            if (ecu_id == 0)
                return ErrorCode.InvalidArg;

            // set specific configs for BMW
            tx_add_extra_bytes = false;
            m_req_prefix = new List<byte> { ecu_id };
            m_resp_prefix = new List<byte> { gateway_id };

            // prepare the request and response
            canMessage2 req = new canMessage2(createEcuAddressBmw(gateway_id), false, data.ToArray());
            canMessageId resp_id = new canMessageId(createEcuAddressBmw(ecu_id), 1, false);

            // check the arguments
            err = checkStartArgs(can_tool, req, resp_id);

            // apply
            m_can_tool = can_tool;
            m_req_msg = req;
            m_resp_id = resp_id;

            // send the message
            if (err == ErrorCode.Ok)
            {
                m_expected_service = createExpectedService(data[0]);

                var uds_req = createRequestMessage();
                if (uds_req == null)
                {
                    err = ErrorCode.InvalidArg;
                }
                else
                {
                    if (!sendRequest(uds_req))
                        err = ErrorCode.TxFailed;
                }
            }

            return err;
        }

        // handle a list of received messages
        public void handleMessages(List<canMessage2> ls)
        {
            foreach (var msg in ls)
                handleMessage(msg);
        }

        // return received data, doesn't matter finished we or not 
        public List<byte> getResponse()
        {
            return m_response_data;
        }


        public int getExpectedEcuId()
        {
            return m_resp_id.Id;
        }

        #endregion

        // private methods
        #region PrivateMethods

        private int createEcuAddressBmw(byte ecu_id)
        {
            return 0x600 + (int)(ecu_id);
        }

        private byte createExpectedService(byte service)
        {
            return (byte)((int)(service) + 0x40);
        }

        private ErrorCode checkStartArgs(CanMessageSendTool can_tool, canMessage2 req, canMessageId resp_id)
        {
            ErrorCode err = ErrorCode.Ok;

            // check
            if (!m_self_test && (can_tool == null || !can_tool.IsSendingAllowed()))
            {
                // check the can tool (expect self_tests)
                err = ErrorCode.InvalidArg;
            }
            else if (req == null || resp_id == null)
            {
                // make sure we have not empty request and response
                err = ErrorCode.InvalidArg;
            }
            else if (req.Data.Count() == 0 || (req.Data.Count() + 1) > tx_message_len_max)
            {
                // check request data len (we do not support multi-frame sending)
                err = ErrorCode.InvalidArg;
            }
            else if (req.Id.Is29bit != resp_id.Is29bit)
            {
                // make sure request proto == response proto
                err = ErrorCode.InvalidArg;
            }
            else if (req.Data[0] == 0)
            {
                // requested service cannot be 0
                err = ErrorCode.InvalidArg;
            }

            return err;
        }

        // internal cleaner
        private void cleanData()
        {
            // parser
            m_flow_sent = false;
            m_finished = false;
            m_gotAtLeastOneResponse = false;
            m_next_nibble = -1;
            m_expected_len = 0;
            m_response_data = new List<byte>();
            m_expected_service = 0;

            // timeouts
            if (rx_timeout_1_single == 0)
                rx_timeout_1_single = rx_timeout_default_single;
            if (rx_timeout_2_multi == 0)
                rx_timeout_2_multi = rx_timeout_default_multi;

            m_rx_timeout = DateTimeOffset.Now.ToUnixTimeMilliseconds() + rx_timeout_1_single;

            // prefixes
            m_req_prefix = null;
            m_resp_prefix = null;
        }

        // send a request
        private bool sendRequest(canMessage2 req)
        {
            return m_self_test ? true : m_can_tool.SendCanMessage(req);
        }

        // send a flow control frame
        private bool sendFlow()
        {
            List<byte> data_flow = new List<byte> { 0x30, 0x00, 0x00 };
            List<byte> data = new List<byte>();

            // add the prefix if required
            if (m_req_prefix != null)
                data.AddRange(m_req_prefix);

            // add the message itself
            data.AddRange(data_flow);

            // add extra bytes if required
            while (tx_add_extra_bytes && data.Count < tx_message_len_max)
                data.Add(tx_add_extra_byte_value);

            // create the flow control message
            canMessage2 flow = new canMessage2(m_req_msg.Id.Id, m_req_msg.Id.Is29bit, data.ToArray());
            // send it
            return m_self_test ? true : m_can_tool.SendCanMessage(flow);
        }

        // handle a single message
        private void handleMessage(canMessage2 msg)
        {
            const int min_allowed_payload = 2;  // msg len + service

            // check id and mode
            if (msg.Id.Id != m_resp_id.Id)
                return;
            if (msg.Id.Is29bit != m_resp_id.Is29bit)
                return;
            // check the payload
            if (msg.Data == null || msg.Data.Count() < min_allowed_payload)
                return;
            // check the prefix
            if (m_resp_prefix != null)
            {
                if ((msg.Data.Count() + min_allowed_payload) < m_resp_prefix.Count())
                    return;
                for (int i = 0; i < m_resp_prefix.Count(); i++)
                {
                    if (m_resp_prefix[i] != msg.Data[i])
                        return;
                }
            }
            // we've finished -> do nothing
            if (m_finished)
                return;

            if (!m_gotAtLeastOneResponse)
            {
                // try to parse the 1st message
                m_gotAtLeastOneResponse = parseFirstMessage(msg);
            } else
            {
                // multiframe message
                parseMultiMessage(msg);
            }

            if (!m_finished)
            {
                m_finished = m_expected_len > 0 &&
                        m_response_data.Count() >= m_expected_len;
            }
        }

        // parse the initial response message, return true if parsed
        private bool parseFirstMessage(canMessage2 msg)
        {
            bool res = false;
            int exp_len = 0;

            int start_pos = 0;
            if (m_resp_prefix != null)
                start_pos = m_resp_prefix.Count();

            // 1st message
            // get the expected payload len
            if (start_pos < msg.Data.Length && msg.Data[start_pos] < 8)
                exp_len = msg.Data[start_pos];
            else if ((start_pos + 1) < msg.Data.Length && (msg.Data[start_pos] & 0x10) == 0x10)
                exp_len = ((msg.Data[start_pos] & (byte)0x0F) << 8) + msg.Data[start_pos + 1];

            if (exp_len == 0)
            {
                // something went wrond
                res = false;
            }
            else if (exp_len < 8) // up to 7 data bytes per message
            {
                int data_pos = start_pos + 1;
                int neg_resp_code_offset = 2;
                int neg_resp_code_pos = data_pos + neg_resp_code_offset;

                // single frame message, just extract payload
                m_expected_len = exp_len;
                m_next_nibble = -1;

                bool is_negative = neg_resp_code == msg.Data[data_pos] &&
                    msg.Data.Count() > neg_resp_code_pos;

                // check the service byte
                if (m_expected_service == msg.Data[data_pos] || is_negative)
                {
                    bool is_negative_wait_more = is_negative &&
                            msg.Data[data_pos + neg_resp_code_offset] == neg_reponse_wait_code;

                    if (is_negative_wait_more)
                    {
                        // the ECU is not ready, wait for a while
                        m_rx_timeout += rx_timeout_negative_wait;
                    }
                    else
                    {                    
                        for (int i = data_pos; i < msg.Data.Count() && m_response_data.Count() < m_expected_len; i++)
                            m_response_data.Add(msg.Data[i]);

                        res = true;
                    }                    
                }
            } else
            {
                int data_pos = start_pos + 2;

                m_expected_len = exp_len;
                m_next_nibble = 1;

                // check the service byte
                if (data_pos < msg.Data.Count() && m_expected_service == msg.Data[data_pos])
                {
                    // timeout
                    m_rx_timeout += rx_timeout_2_multi;

                    // send the flow control
                    if (!m_flow_sent)
                        m_flow_sent = sendFlow();

                    // multiframe message
                    for (int i = data_pos; i < msg.Data.Count(); i++)
                        m_response_data.Add(msg.Data[i]);

                    res = true;
                }
            }

            return res;
        }

        // message parser: multiframe message
        private void parseMultiMessage(canMessage2 msg)
        {
            int nibble_pos = 0;
            if (m_resp_prefix != null)
                nibble_pos = m_resp_prefix.Count();
            int data_pos = nibble_pos + 1;

            if (msg.Data.Count() <= data_pos)
                return;
            if ((msg.Data[nibble_pos] & 0x20) != 0x20)
                return;

            int nibble = msg.Data[nibble_pos] & 0x0F;
            if (nibble != m_next_nibble)
                return;

            // copy data
            for (int i = data_pos; i < msg.Data.Count() && m_response_data.Count() < m_expected_len; i++)
                m_response_data.Add(msg.Data[i]);

            // update the nibble
            m_next_nibble = nibble + 1;
            if (m_next_nibble > 0x0F)
                m_next_nibble = 0;

            // update timeout value
            m_rx_timeout += rx_timeout_2_multi;
        }

        // create a request message
        private canMessage2 createRequestMessage()
        {
            // we do not support TX multiftame messages
            if (m_req_msg == null || (m_req_msg.Data.Count() + 1) > tx_message_len_max)
                return null;

            List<byte> data = new List<byte>();

            // add the prefix if required
            if (m_req_prefix != null)
                data.AddRange(m_req_prefix);

            // the len of the payload and the payload itself
            data.Add((byte)(m_req_msg.Data.Count()));
            data.AddRange(m_req_msg.Data);

            // dummy bytes
            while (data.Count < tx_message_len_max && tx_add_extra_bytes)
                data.Add(tx_add_extra_byte_value);

            canMessage2 uds_req = new canMessage2(m_req_msg.Id.Id, m_req_msg.Id.Is29bit, data.ToArray());
            return uds_req;
        }

        #endregion

        // unit testing
        #region UnitTests

        // a test func to compare 2 lists (need it for unit testing)
        private bool SelfTestCompareLists(List<byte> lhs, List<byte> rhs)
        {
            if (lhs.Count() != rhs.Count())
                return false;

            for (int i = 0; i < lhs.Count(); i++)
            {
                if (lhs[i] != rhs[i])
                    return false;
            }

            return true;
        }

        // Simple unit tests
        public void SelfTest()
        {
            m_self_test = true;

            // test: clean
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                Debug.Assert(m_finished == false);
                Debug.Assert(m_gotAtLeastOneResponse == false);
                Debug.Assert(m_expected_len == 0);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), new List<byte>()));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test: parse signle message
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 };

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == 6);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test: parse short signle message
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x3E });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x7E };

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x01, 0x7E}));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test: parse multiframe message
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x22, 0xF1, 0x90 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x62, 0xF1, 0x90 };
                for (byte i = 0; i < 17; i++)
                    expected.Add(i);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x10, 17 + 3, 0x62, 0xF1, 0x90, 0, 1, 2 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x21, 3, 4, 5, 6, 7, 8, 9 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x22, 10, 11, 12, 13, 14, 15, 16}));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // test: parse a very long multiframe message
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x22, 0xF1, 0x90 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x62, 0xF1, 0x90 };
                for (byte i = 0; i < 17; i++)
                    expected.Add(i);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x10, 17 + 3, 0x62, 0xF1, 0x90, 0, 1, 2 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x21, 3, 4, 5, 6, 7, 8, 9 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x22, 10, 11, 12, 13, 14, 15, 16 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // test: parse multiframe messages, the last one is short
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x22, 0xF1, 0x90 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x62, 0xF1, 0x90 };
                for (byte i = 0; i < 18; i++)
                    expected.Add(i);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x10, 18 + 3, 0x62, 0xF1, 0x90, 0, 1, 2 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x21, 3, 4, 5, 6, 7, 8, 9 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x22, 10, 11, 12, 13, 14, 15, 16 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x23, 17 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // test: single message with unexpected messages
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 };

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x123, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x123, true,  new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E9, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E9, true, new byte[]  { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == 6);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // test: multiframe message with unexpected messages
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x22, 0xF1, 0x90 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x62, 0xF1, 0x90 };
                for (byte i = 0; i < 17; i++)
                    expected.Add(i);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x123, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x123, true, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E9, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E9, true, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x10, 17 + 3, 0x62, 0xF1, 0x90, 0, 1, 2 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x21, 3, 4, 5, 6, 7, 8, 9 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x22, 10, 11, 12, 13, 14, 15, 16 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x23, 17, 18, 19, 20, 21, 22, 23 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);

                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // test: incorrect service
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41 + 1, 0x00, 0x10, 0x20, 0x30, 0x40 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41 - 1, 0x00, 0x10, 0x20, 0x30, 0x40 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(IsSuccesfullyFinished() == false);
                Debug.Assert(m_gotAtLeastOneResponse == false);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), new List<byte> { }));
            }

            // negative response
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x03, 0x7F, 0x01, 0x13 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(IsSuccesfullyFinished());
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), new List<byte> {0x7F, 0x01, 0x13}));
            }

            // negative response 0x13 + correct response
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x03, 0x7F, 0x01, 0x13 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(IsSuccesfullyFinished());
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), new List<byte> { 0x7F, 0x01, 0x13 }));
            }

            // negative response 0x78 + correct response
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 };

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x03, 0x7F, 0x01, 0x78 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(IsSuccesfullyFinished());
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // timeout
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 };

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30, 0x40 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);

                Thread.Sleep((int)rx_timeout_1_single + 1);
                handleMessages(resp);
                Debug.Assert(IsTimeoutExpired());
            }

            // test: multiframe message 29b
            {
                canMessage2 req = new canMessage2(0x18DAC7F1, true, new byte[] { 0x22, 0xF1, 0xF2 });
                canMessageId resp_id = new canMessageId(0x18DAF1C7, 1, true);
                List<byte> expected = new List<byte> { 0x62, 0xF1, 0xF2, 0, 1, 2,3,4,5,6 };


                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x18DAF1C7, true, new byte[] { 0x10, 10, 0x62, 0xF1, 0xF2, 0, 1, 2 }));
                resp.Add(new canMessage2(0x18DAF1C7, true, new byte[] { 0x21,  3, 4, 5, 6, 7, 8, 9 }));
                resp.Add(new canMessage2(0x18DAF1C7, true, new byte[] { 0x22, 10, 11, 12, 13, 14, 15, 16 }));
                resp.Add(new canMessage2(0x18DAF1C7, true, new byte[] { 0x23, 17, 18, 19, 20, 21, 22, 23 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);

                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            // test BMW: parse signle message
            {
                List<byte> request = new List<byte> { 0x22, 0x01, 0x01 };
                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x06, 0x62, 0x01, 0x01, 0x20, 0x30, 0x40 }));
                List<byte> expected = new List<byte> { 0x62, 0x01, 0x01, 0x20, 0x30, 0x40 };

                Debug.Assert(sendRequestBMW(null, 0x60, request) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == 6);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test BMW: parse multi-frame message 1 (the last msg is short)
            {
                List<byte> request = new List<byte> { 0x22, 0x01, 0x01 };
                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x10, 20,   0x62, 0x01, 0x01, 0x10, 0x11}));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x21, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x22, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x23, 0x1E, 0x1F, 0x20 }));

                List<byte> expected = new List<byte> { 0x62, 0x01, 0x01 };
                for (int i = 0; i < (20 - 3); i++)
                    expected.Add((byte)(0x10 + i));

                Debug.Assert(sendRequestBMW(null, 0x60, request) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test BMW: parse multi-frame message 2 (with flow messages)
            {
                List<byte> request = new List<byte> { 0x22, 0x01, 0x01 };
                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x670, false, new byte[] { 0xF1, 0x10,   20, 0x62, 0x01, 0x01, 0, 0 }));
                resp.Add(new canMessage2(0x777, false, new byte[] { 0x01, 0x02 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x10,   20, 0x62, 0x01, 0x01, 0x10, 0x11 }));
                resp.Add(new canMessage2(0x670, false, new byte[] { 0xF1, 0x21, 0, 0, 0, 0, 0, 0 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x21, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 }));
                resp.Add(new canMessage2(0x670, false, new byte[] { 0xF1, 0x22, 0, 0, 0, 0, 0, 0 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x22, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D }));
                resp.Add(new canMessage2(0x670, false, new byte[] { 0xF1, 0x23, 0, 0, 0 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x23, 0x1E, 0x1F, 0x20 }));

                List<byte> expected = new List<byte> { 0x62, 0x01, 0x01 };
                for (int i = 0; i < (20 - 3); i++)
                    expected.Add((byte)(0x10 + i));

                Debug.Assert(sendRequestBMW(null, 0x60, request) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test BMW: parse multi-frame message 3 (the last msg is long)
            {
                List<byte> request = new List<byte> { 0x22, 0x01, 0x01 };
                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x10,  20, 0x62, 0x01, 0x01, 0x10, 0x11 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x21, 0x12, 0x13, 0x14, 0x15, 0x16, 0x17 }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x22, 0x18, 0x19, 0x1A, 0x1B, 0x1C, 0x1D }));
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x23, 0x1E, 0x1F, 0x20, 0xFF, 0xFF, 0xFF }));

                List<byte> expected = new List<byte> { 0x62, 0x01, 0x01 };
                for (int i = 0; i < (20 - 3); i++)
                    expected.Add((byte)(0x10 + i));

                Debug.Assert(sendRequestBMW(null, 0x60, request) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test BMW: negative response
            {
                List<byte> request = new List<byte> { 0x22, 0x01, 0x01 };
                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x660, false, new byte[] { 0xF1, 0x03, 0x7F, 0x01, 0x31}));
                List<byte> expected = new List<byte> { 0x7F, 0x01, 0x31 };

                Debug.Assert(sendRequestBMW(null, 0x60, request) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == expected.Count());
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test: too short single-frame message
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x01, 0x00 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x41, 0x00, 0x10, 0x20, 0x30 };

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x06, 0x41, 0x00, 0x10, 0x20, 0x30 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(!m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == 6);
                Debug.Assert(m_flow_sent == false);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
                Debug.Assert(!IsTimeoutExpired());
            }

            // test: too short multiframe message
            {
                canMessage2 req = new canMessage2(0x7E0, false, new byte[] { 0x22, 0xF1, 0x90 });
                canMessageId resp_id = new canMessageId(0x7E8, 1, false);
                List<byte> expected = new List<byte> { 0x62, 0xF1, 0x90 };
                for (byte i = 0; i < 15; i++)
                    expected.Add(i);

                List<canMessage2> resp = new List<canMessage2>();
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x10, 17 + 3, 0x62, 0xF1, 0x90, 0, 1, 2 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x21, 3, 4, 5, 6, 7, 8, 9 }));
                resp.Add(new canMessage2(0x7E8, false, new byte[] { 0x22, 10, 11, 12, 13, 14 }));

                Debug.Assert(sendRequest(null, req, resp_id) == ErrorCode.Ok);
                handleMessages(resp);
                Debug.Assert(!m_finished);
                Debug.Assert(m_gotAtLeastOneResponse);
                Debug.Assert(m_expected_len == 20);
                Debug.Assert(m_flow_sent == true);
                Debug.Assert(SelfTestCompareLists(getResponse(), expected));
            }

            m_self_test = false;

            Debug.WriteLine("UDS unit testing finished");
        }

        #endregion
    }
}
