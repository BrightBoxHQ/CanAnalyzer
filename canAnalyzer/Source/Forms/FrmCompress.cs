using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.IO;
using System.Diagnostics;
using System.Threading;

/* trace compression */

namespace canAnalyzer
{
    public partial class FrmCompress : Form
    {
        // action
        private enum action {
            compress,
            decompress
        };
        // action
        private action m_act = action.compress;

        // thread and its flag
        private Thread m_convert_thread = null;
        private bool m_thread_stop_req = false;
        private bool m_form_close_req = false;

        // path
        private string str_path_source = string.Empty;
        private string str_path_dest = string.Empty;
        // stopwath for time measurement
        private Stopwatch m_sw = null;

        // constructor
        public FrmCompress()
        {
            InitializeComponent();

            Text = "Compression";

            // init UI
            uiInit();
        }

        // region: ui control
        #region user_interface_control

        // init UI
        private void uiInit()
        {
            // events
            // radio buttons
            rbActCompress.CheckedChanged += evtRadioButtonChanged;
            rbActDecompress.CheckedChanged += evtRadioButtonChanged;
            // buttons
            btnPathSource.Click += evtButtonClick;
            btnPathDest.Click += evtButtonClick;
            btnAct.Click += evtButtonClick;
            btnAbort.Click += evtButtonClick;
   
            // text
            gbSource.Text = "Source";
            gbDest.Text = "Destination";
            gbAct.Text = "Action";
            gbProgress.Text = "Progress";

            // radio buttons
            rbActCompress.Checked = false;
            rbActCompress.Enabled = false;
            rbActDecompress.Checked = true;

            // controls
            ui_enable_set(true);

            // progress
            pbProgress.Minimum = 0;
            pbProgress.Step = 1;

            // size
            MinimumSize = new Size(Width, Height);
        }

        // progress bar set
        private void ui_progress_set(int max, int val)
        {
            // invoke
            if (InvokeRequired)
            {
                BeginInvoke(new Action<int, int>(ui_progress_set), max, val);
                return;
            }

            // update
            if (max != pbProgress.Minimum)
                pbProgress.Maximum = max;

            pbProgress.Value = val;
        }

        // increment
        private void ui_progress_step()
        {
            // invoke
            if (InvokeRequired)
            {
                BeginInvoke(new Action(ui_progress_step));
                return;
            }

            int value = pbProgress.Value + 1;

            // To get around the progressive animation, we need to move the 
            // progress bar backwards.
            if (value == pbProgress.Maximum)
            {
                // Special case as value can't be set greater than Maximum.
                pbProgress.Maximum = value + 1;     // Temporarily Increase Maximum
                pbProgress.Value = value + 1;       // Move past
                pbProgress.Maximum = value;         // Reset maximum
            }
            else
            {
                pbProgress.Value = value + 1;       // Move past
            }

            pbProgress.Value = value;               // Move to correct value
        }

        // config access stuff
        private void ui_enable_set(bool enable)
        {
            // invoke
            if (InvokeRequired)
            {
                BeginInvoke(new Action<bool>(ui_enable_set), enable);
                return;
            }

            // update
            gbAct.Enabled = enable;
            gbDest.Enabled = enable;
            gbSource.Enabled = enable;
            gbProgress.Enabled = !enable;
        }

        // update the current state text
        private void ui_set_state_string(string text)
        {
            // invoke
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(ui_set_state_string), text);
                return;
            }

            txtState.Text = text;
        }

        #endregion

        // region: thread
        #region thread
        private void thread_start(string source, string dest)
        {
            m_thread_stop_req = false;

            str_path_source = source;
            str_path_dest = dest;

            m_convert_thread = new Thread(thread_handle);
            m_convert_thread.Name = "CAN compressor";
            m_convert_thread.Start();
        }

        private void thread_stop()
        {
            m_thread_stop_req = true;
        }

        private void thread_handle()
        {
            string s_res = null;

            m_sw = Stopwatch.StartNew();

            // started
            ui_enable_set(false);
            // reset the progress
            ui_progress_set(100, 0);

            if (m_act == action.decompress)
            {
                // do
                if (s_res == null)
                {
                    s_res = doDecompress(str_path_source, str_path_dest);
                }
            }
            else
            {
                ui_set_state_string("Not Implemented");
            }


            if (m_thread_stop_req)
                s_res = "Aborted";

            // kill
            m_sw.Stop();
            m_sw = null;

            // free
            GC.Collect();

            if (!m_form_close_req)
            {
                ui_set_state_string(m_thread_stop_req ? "Aborted" : "Finished");

                if (!m_thread_stop_req)
                    MessageBox.Show(s_res);

                ui_enable_set(true);
            }

            // reset the flag
            m_thread_stop_req = false;
        }
        #endregion

        // region: decompressor
        #region decompressor

        // do decompression
        private string doDecompress(string source, string dest)
        {
            if (string.IsNullOrEmpty(source) || string.IsNullOrEmpty(dest))
                return "Decompression failed. Invalid arguments";

            long msg_cnt = 0;
            List<canMessage2> ls = doDecompressToList(source, out msg_cnt);
            // free
            GC.Collect();

            string sres = "Decompression failed. No CAN messages.";
   
            if (ls != null && ls.Count > 0)
            {
                canTraceUtils.timestamp_offset ts = new canTraceUtils.timestamp_offset();

                ui_set_state_string("Converting...");

                // a workaround to reduce MAX RAM consumtion
                // do write, 100k entries at once
                int page_size = 100 * 1000;
                int page_cnt = (ls.Count / page_size) + 1;
                int written = 0;

                ui_progress_set(page_cnt, 0);

                for (int page = 0; page < page_cnt && !m_thread_stop_req; page++)
                {
                    bool header = page == 0;

                    int offset = page * page_size;
                    int len = ls.Count - page * page_size;
                    if (len > page_size)
                        len = page_size;

                    // execute
                    if (header == true)
                    {
                        // write + header
                        File.WriteAllText(dest,
                            canTraceUtils.mConverter.msg_list_to_string(
                                ls.GetRange(page * page_size, len), ts, true));
                    }
                    else
                    {
                        // append, no header
                        File.AppendAllText(dest,
                            canTraceUtils.mConverter.msg_list_to_string(
                                ls.GetRange(page * page_size, len), ts, false));
                    }
                    
                    //Thread.Sleep(200);
                    
                    written += len;

                    // progress
                    ui_progress_step();
                }


                string str_time_sec = "NULL";

                // timer
                if (m_sw != null)
                {
                    long msec = m_sw.ElapsedMilliseconds;
                    str_time_sec = string.Format("{0:0.0} sec",  (double)msec / 1000.0d);
                }

                if (written > 0 && !m_thread_stop_req)
                {
                    sres = string.Format("Decompression completed successfully.{0}" +
                        "Number of CAN messages: {1} (out of {2}).{3}" +
                        "Elapsed time: {4}",
                        Environment.NewLine, written, msg_cnt, Environment.NewLine, str_time_sec);
                }
                else
                    sres = "Decompression failed. Unknown error.";
            }

            return sres;
        }

        // do decompression: extract the data
        private List<canMessage2> doDecompressToList(string source, out long msg_cnt)
        {
            msg_cnt = 0;

            // read the file
            ui_set_state_string("Reading...");
            byte[] src = File.ReadAllBytes(source);
            // get a number of messages
            msg_cnt = Compression.CanMessageCompressedMessageCountGet(src);
            // decompress
            ui_set_state_string("Extracting...");
            List<canMessage2> res = Compression.CanMessagesDecompress(src);

            return res;
        }

        #endregion

        // region: events
        #region events

        // event: on form closing
        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            m_form_close_req = true;
            thread_stop();
        }

        // event: on radio button state changed
        private void evtRadioButtonChanged(object sender, EventArgs e)
        {
            if (rbActCompress.Checked == true)
                m_act = action.compress;
            else if (rbActDecompress.Checked == true)
                m_act = action.decompress;
        }

        // event: on button clicked
        private void evtButtonClick(object sender, EventArgs e)
        {
            Button btn = (Button)sender;

            // source path
            if (btn == btnPathSource)
            {
                string path = browseFileDialog(true);
                if (path != null && path != string.Empty)
                {
                    txtPathSource.Text = path;
                }
            }
            // destination path
            else if (btn == btnPathDest)
            {
                string path = browseFileDialog(false);
                if (path != null && path != string.Empty)
                {
                    txtPathDest.Text = path;
                }
            }
            // perform the action
            else if (btn == btnAct)
            {
                string source = txtPathSource.Text;
                string dest = txtPathDest.Text;
                string s_res = null;

                source = source.Replace(" ", "");
                dest = dest.Replace(" ", "");

                // check does the file exist
                if (s_res == null && !File.Exists(source))
                {
                    s_res = "Source file does not exist";
                }
                // check destination file
                if (s_res == null && string.IsNullOrEmpty(dest))
                {
                    dest = Path.ChangeExtension(source, ".txt");
                    if (string.IsNullOrEmpty(dest))
                        s_res = "Destination path is empty";
                }

                if (!string.IsNullOrEmpty(s_res))
                {
                    // message
                    MessageBox.Show(s_res);
                }
                else
                {
                    // start
                    thread_start(source, dest);
                }
            }
            else if (btn == btnAbort)
            {
                m_thread_stop_req = true;
            }
        }

        #endregion

        // file browser
        private string browseFileDialog(bool open)
        {
            // open dialog
            if (open)
            {
                OpenFileDialog dlg = new OpenFileDialog();
                dlg.CheckPathExists = true;

                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }
            // save dialog
            else
            {
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.CheckPathExists = true;
                dlg.OverwritePrompt = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    return dlg.FileName;
                }
            }

            return string.Empty;
        }
    }
}
