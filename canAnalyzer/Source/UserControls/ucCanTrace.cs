using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using System.IO;
using System.Threading;
using System.Text.RegularExpressions;
using System.Runtime.InteropServices;


namespace canAnalyzer
{
    public partial class UcCanTrace : UserControl
    {
        [DllImport("user32.dll")]
        private static extern int SendMessage(IntPtr hWnd, Int32 wMsg, bool wParam, Int32 lParam);
        private const int WM_SETREDRAW = 11;

        // mode
        public enum traceMode
        {
            traceAll,
            traceSelected,
            pause
        }

        // the region contains class fields
        #region Fields

        //private canTraceUtils.timestamp     ts;         // timestamp converter
        private canTraceUtils.timestamp_offset ts;
        private canTraceUtils.mConverter    conv;       // message to trace converter
        private canTraceUtils.sendWorker    sendWrkr;   // sender

        private traceMode mode;
        private CanMessageSendTool CanTool;

        private int MsgIdx = 1;

        private Mutex lockMutex = new Mutex();

        // data to store the full sesstion trace
        private List<canMessage2> storage_msg_list = new List<canMessage2>();
        private long storage_ts = 0;
        private string storage_file_path = string.Empty;
        private string storage_file_name = string.Empty;
        private string can_speed = string.Empty;

        #endregion

        // the region contains a class constructor
        #region Constructor

        // constructor
        public UcCanTrace(CanMessageSendTool can)
        {
            InitializeComponent();

            // parent
            CanTool = can;

            // grid
            createGrid();
            // menu
            createMenu();
            grid.ContextMenuStrip = menu;

            // send tool
            sendWrkr = new canTraceUtils.sendWorker(CanTool, this);
            
            mode = traceMode.pause;


            //ts = new canTraceUtils.timestamp();
            ts = new canTraceUtils.timestamp_offset();

            conv = new canTraceUtils.mConverter();
            conv.TS = ts;

            tbPlayFrom.KeyPress += Tools.textBoxIntOnlyEvent;
            tbPlayTo.KeyPress += Tools.textBoxIntOnlyEvent;

            updateMsgCounter();


            cbTraceMode.Items.Add("All");
            cbTraceMode.Items.Add("Selected");
            cbTraceMode.SelectedIndex = 0;

            // icons
            btnTrace.Image = Properties.Resources.icon_record;
            btnSendStep.Image = Properties.Resources.icon_step;
            btnPlay.Image = Properties.Resources.icon_play;
            btnClear.Image = Properties.Resources.icon_clear;

            btnSendStep.Enabled = false;
            btnPlay.Enabled = false;
            btnSendRange.Enabled = false;
            btnClear.Enabled = false;

            tbPlayFrom.Enabled = btnPlay.Enabled;
            tbPlayTo.Enabled = btnPlay.Enabled;
            lblSendFrom.Enabled = btnPlay.Enabled;
            lblSendTo.Enabled = btnPlay.Enabled;

            lblSendFrom.Font = new Font("Consolas", 9, FontStyle.Italic);
            lblSendTo.Font = lblSendFrom.Font;

            cbTraceMode.Font = new Font("Consolas", 8f);

            lblTotalMsgs.Font = new Font("Calibri", 9.0f);

            foreach (DataGridViewColumn col in grid.Columns)
                col.HeaderCell.Style.Font = new Font("Calibri", 9.0f, FontStyle.Bold);

            grid.DefaultCellStyle.Font = new Font("Consolas", 8.5f);//, FontStyle.Italic);

            // System.Windows.Forms.ToolTip ToolTip1 = new System.Windows.Forms.ToolTip();
            //  ToolTip1.SetToolTip(this.btnTrace, "Start/Stop Recording");

        }
        #endregion

        private void updateMsgCounter()
        {
            lblTotalMsgs.Text = string.Format("Total: {0}", grid.RowCount);
        }

        // create a grid
        private void createGrid()
        {   
            // config
            grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.Dock = DockStyle.Fill;
            grid.Location = new System.Drawing.Point(0, 0);
            grid.Name = "grid";
            grid.RowTemplate.Height = 21;
            grid.TabIndex = 0;
            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
            grid.ReadOnly = true;
            // restrictions
            grid.AllowUserToResizeRows = false;
            grid.AllowUserToResizeColumns = false;
            grid.AllowUserToAddRows = false;
            // style
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.AdvancedCellBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
            grid.AdvancedCellBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            grid.AdvancedCellBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;
            grid.ColumnHeadersDefaultCellStyle.Alignment =
               DataGridViewContentAlignment.MiddleLeft;

            grid.BorderStyle = BorderStyle.None;
            grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 239, 249);
            grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            grid.DefaultCellStyle.SelectionBackColor = Color.DarkTurquoise;
            grid.DefaultCellStyle.SelectionForeColor = Color.Black;

            grid.Dock = DockStyle.Fill;

            // columns
            grid.Columns.Add("Idx", "Idx");
            grid.Columns.Add("Ts", "Ts");
            grid.Columns.Add("CAN ID", "CAN ID");
            grid.Columns.Add("DLC", "DLC");
            for (int col = 0; col < 8; col++)
                grid.Columns.Add(col.ToString(), col.ToString());

            int colIdx = 0;
            grid.Columns[colIdx++].Width = 70;
            grid.Columns[colIdx++].Width = 90;
            grid.Columns[colIdx++].Width = 95;
            grid.Columns[colIdx++].Width = 50;
            for (int i = 0; i < 8; i++)
                grid.Columns[colIdx++].Width = 30;// 28;

            grid.ClipboardCopyMode = DataGridViewClipboardCopyMode.EnableWithoutHeaderText;

            // tests for performance
            grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing; //or even better .DisableResizing. Most time consumption enum is DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders

            // set it to false if not needed
            grid.RowHeadersVisible = false;

            grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            for( int i =0; i < grid.Columns.Count; i++)
                grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;


            Tools.SetDoubleBuffered(grid, true);


            grid.KeyDown += Grid_KeyDown;

            //grid.VirtualMode = true;
            //grid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.DisplayedCells;

            /*
            // test code
            object[] ob = new object[] { 1, 2, 3,4,5,6 };
            grid.Rows.Add(ob);
            grid.Rows.Add(ob);
            grid.Rows.Add(ob);
            */
            grid.AllowUserToDeleteRows = false;
        }

        private void Grid_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.F && e.Modifiers == Keys.Control)
            {
                // todo
                // search
                // int i = 0;
                // i = 5;
            }
        }

        private void selectRow (int idx)
        {
            if( InvokeRequired )
            {
                this.Invoke(new Action<int>(selectRow), new object[] { idx });
                return;
            }

           // idx -= 1;
            foreach (DataGridViewRow r in grid.Rows)
                r.Selected = false;
            if (idx < grid.RowCount)
            {
                grid.Rows[idx].Selected = true;
                //grid.FirstDisplayedScrollingRowIndex = idx;
                grid.CurrentCell = grid.Rows[idx].Cells[0];
            }
        }

        public void selectNextRow()
        {
            var sel = grid.SelectedRows;
            if (sel.Count >= 1)
            {
                var row = sel[0];
                int idx = row.Index + 1;
                selectRow(idx);
            }
        }

        // create a menu
        private void createMenu()
        {
            // menu
            //menu.Items.Add("Trace All");
           // menu.Items.Add("Trace Selected");
            //menu.Items.Add("Pause");
            //menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Copy Selected");
            menu.Items.Add("Copy All");
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Save Selected");
            menu.Items.Add("Save All");
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Send Selected");
            menu.Items.Add("Copy Selected as Script");
            menu.Items.Add(new ToolStripSeparator());
            //menu.Items.Add("Copy Selected for Remoto");
            menu.Items.Add("Load");
            menu.Items.Add(new ToolStripSeparator());
            menu.Items.Add("Clear All");

            // pause
            menu.Items[2].Enabled = false;
        }

        // cleaner
        public void clear()
        {
            // lock
            lockMutex.WaitOne();

            // clear
            grid.DataSource = null;
            grid.Rows.Clear();
            GC.Collect();
            grid.Refresh();

            // timestamp
            ts.reset();
            // mag idx
            MsgIdx = 1;
            
            // ui
            updateMsgCounter();
            updateButtons();

            // unlock
            lockMutex.ReleaseMutex();
        }

        // set the CAN speed value
        public void setCanSpeed(string speed)
        {
            can_speed = speed;
        }

        #region data_save

        private bool m_data_save_allowed = false;
        // data save mutex
        private Mutex m_file_mutex = new Mutex();

        // save 
        private void store(bool force = false)
        {
            // thresholds
            const long save_interval_ms = 60 * 1000; // every minute
            const int save_threshold = 200*1000;     // 200k messages

            // lock
            m_file_mutex.WaitOne();

            // do nothing if not allowed
            if (!m_data_save_allowed)
            {
                m_file_mutex.ReleaseMutex();
                return;
            }

            // do
            if (storage_msg_list.Count > 0)
            {
                // now
                DateTimeOffset now = DateTimeOffset.Now;
                long now_ts = now.ToUnixTimeMilliseconds();

                if (storage_ts == 0)
                {
                    storage_ts = now_ts + save_interval_ms;
                }

                // check the conditions
                if (storage_msg_list.Count >= save_threshold || now_ts >= storage_ts || force)
                {
                    bool file_just_created = false;

                    if (string.IsNullOrEmpty(storage_file_path))
                    {
                        storage_file_path = string.Format("{0}\\trace\\",
                            System.IO.Path.GetDirectoryName(Application.ExecutablePath));
                    }
                    if (string.IsNullOrEmpty(storage_file_name))
                    {
                        storage_file_name = string.Format("trace_{0:yyyy_MM_dd_HH_mm_ss}.ctd", now);
                        file_just_created = true;
                    }
                    
                    // make sure the dir exists
                    if (!Directory.Exists(storage_file_path))
                        Directory.CreateDirectory(storage_file_path);

                    // one more time
                    if (Directory.Exists(storage_file_path))
                    {
                        // compress
                        byte[] comp = Compression.CanMessagesCompress(storage_msg_list);
                        if (comp != null)
                        {
                            // store
                            using (var stream = new FileStream(storage_file_path + storage_file_name, FileMode.Append))
                            {
                                // append the title string
                                if (file_just_created)
                                {
                                    string title = string.Format(
                                        "User: {0}\nCAN Speed: {1}\n",
                                        Environment.UserName, can_speed);
                                    var buff = Encoding.ASCII.GetBytes(title + Environment.NewLine);
                                    stream.Write(buff, 0, buff.Length);
                                }
                                // data
                                stream.Write(comp, 0, comp.Length);
                            }
                        }
                    }

                    // clean
                    storage_msg_list.Clear();
                    storage_ts = now_ts + save_interval_ms;
                }
            }
            // unlock
            m_file_mutex.ReleaseMutex();
        }
        
        // the device has been just disconnected
        public void disconnected()
        {
            // force data save
            store(true);
            // clean the filename to create a new file for the next time
            m_file_mutex.WaitOne();
            storage_file_name = string.Empty;
            m_file_mutex.ReleaseMutex();
        }

        // enable / disable data saving
        public void save_data_allow(bool allow)
        {
            m_file_mutex.WaitOne();
            m_data_save_allowed = allow;
            m_file_mutex.ReleaseMutex();
        }

        #endregion

        public void stop()
        {
            // pause
            mode = traceMode.pause;
            // clean
            clear();
            // force store
            store(true);
        }

        // push a message list
        public void pushList (List<canMessage2> ls, UniqueDataSetCan selected)
        {
            // append new messages
            storage_msg_list.AddRange(ls);
            // try to store
            store();

            // paused? 
            // just do return
            if (traceMode.pause == mode)
                return;

            // lock
            lockMutex.WaitOne();

            // below we're using add range because it works much faster
            DataGridViewRow[] rows = null;
    
            // trace only selected messages
            if (mode == traceMode.traceSelected)
            {
                // step 1: get a number of rows we gonna push
                int cnt = 0;
                for (int i = 0; i < ls.Count; i++)
                    if (selected.Contains(ls[i].Id))
                        cnt++;

                // step 2: create the rows and then push them
                if( cnt > 0 )
                {
                    // create
                    rows = new DataGridViewRow[cnt];
                    // prepare
                    int rowPos = 0;
                    for( int i = 0; i < ls.Count; i++)
                    {
                        if (selected.Contains(ls[i].Id))
                        {
                            rows[rowPos] = new DataGridViewRow();
                            rows[rowPos++].CreateCells(grid, conv.msg2row(ls[i], MsgIdx++));
                        }
                    }
                    // add
                    grid.Rows.AddRange(rows);
                }
            }
            // trace all the data we've just received
            else
            {
                // create
                rows = new DataGridViewRow[ls.Count];
                // prepare
                int rowPos = 0;

                for (int i = 0; i < ls.Count; i++)
                {
                    // new DataGridViewRow is too slow but this is the fastest way
                    rows[rowPos] = new DataGridViewRow();
                    rows[rowPos++].CreateCells(grid, conv.msg2row(ls[i], MsgIdx++));
                }
                // add
                grid.Rows.AddRange(rows);
            }

            // unlock
            lockMutex.ReleaseMutex();
            // update
            updateMsgCounter();
            updateButtons();
        }


        private string getRowsAsAstring (bool selected)
        {
            string txt = conv.getHeaderString();

            foreach (DataGridViewRow row in grid.Rows)
                if( !selected || (selected && row.Selected) )
                    txt += Environment.NewLine + conv.row2str(row);

            return txt;
        }

        private string[] getRowsAsAstringArray(bool selected)
        {
            int cnt = 0;

            // check
            if (grid == null || grid.Rows == null)
                return null;

            // evaluate how much rows should be returned
            foreach (DataGridViewRow row in grid.Rows)
                if (!selected || (selected && row.Selected))
                    cnt++;

            // do
            if (cnt > 0)
            {
                // allocate
                string[] array = new string[cnt + 1];
                // add the header
                array[0] = conv.getHeaderString();
                // add the data
                int idx = 1;
                foreach (DataGridViewRow row in grid.Rows)
                    if (!selected || (selected && row.Selected))
                        array[idx++] = conv.row2str(row);
                return array;
            }

            return null;
        }

        // the region contains context menu callback
        #region Context Menu

        // context menu callback
        private void onContextMenuClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string selected = e.ClickedItem.Text.ToLower();

            // clear
            if (selected == "clear all")
            {
                clear();
            }

            // save or copy
            if( selected == "copy all" || selected == "copy selected" || selected == "save all" || selected == "save selected" )
            {
                menu.Hide();
                bool selected_only = selected.Contains("selected");

                // check
                if (grid != null && grid.Rows != null && grid.Rows.Count > 0)
                {
                    // make a copy
                    if (selected.Contains("copy"))
                    {
                        // get a row list
                        string[] arr = getRowsAsAstringArray(selected_only);
                        // make sure it is not empty
                        if (arr != null)
                        {
                            StringBuilder sb = new StringBuilder();
                            foreach (string str in arr)
                            {
                                sb.Append(str);
                                sb.Append(Environment.NewLine);
                            }
                            // put
                            Clipboard.SetText(sb.ToString());
                        }
                    }
                    else if (selected.Contains("save"))
                    {
                        // show the save dialog
                        SaveFileDialog dlg = new SaveFileDialog();
                        dlg.DefaultExt = "txt";
                        dlg.AddExtension = true;
                        dlg.CheckPathExists = true;
                        dlg.Filter = "CAN Analyzer trace list|*txt";
                        dlg.OverwritePrompt = true;
                        if (dlg.ShowDialog() == DialogResult.OK)
                        {
                            // get data
                            string[] array = getRowsAsAstringArray(selected_only);
                            // check
                            if (array != null && array.Length > 0)
                                System.IO.File.WriteAllLines(dlg.FileName, array);
                        }
                    }
                }
            }
            
            /*
            if( selected == "trace all" || selected == "trace selected" )
            {
                mode = selected == "trace all" ? traceMode.traceAll : traceMode.traceSelected;

                menu.Items[0].Enabled = mode == traceMode.pause;
                menu.Items[1].Enabled = mode == traceMode.pause;
                menu.Items[2].Enabled = mode != traceMode.pause;
            }
            if (selected == "pause" )
            {
                mode = traceMode.pause;

                menu.Items[0].Enabled = mode == traceMode.pause;
                menu.Items[1].Enabled = mode == traceMode.pause;
                menu.Items[2].Enabled = mode != traceMode.pause;
            }
            */

            // send 
            if (selected == "send selected")
            {
                List<canMessage2> ls = new List<canMessage2>();
                // check
                if (grid != null && grid.Rows != null && grid.Rows.Count != 0)
                {
                    foreach (DataGridViewRow row in grid.Rows)
                    {
                        if (row.Selected)
                            ls.Add(conv.row2msg(row));
                    }
                }

                // send
                if (ls.Count > 0)
                    sendWrkr.send(ls);
            }

            // copy as script
            if (selected == "copy selected as script")
            {
                List<canMessage2> ls = new List<canMessage2>();
                foreach (DataGridViewRow row in grid.Rows)
                    if (row.Selected)
                        ls.Add(conv.row2msg(row));

                string res = string.Empty;
                for( int i = 0; i < ls.Count; i++)
                {
                    res += nsScriptParser.scriptParser.message2string(ls[i]);
                    if (i != ls.Count - 1)
                    {
                        int tsDiff = ls[i + 1].TimeStamp.TimeStamp - ls[i].TimeStamp.TimeStamp;
                        if (tsDiff > 0)
                            res += Environment.NewLine + "sleep( " + tsDiff.ToString() + " );";
                        res += Environment.NewLine;
                    }
                }

                if (!string.IsNullOrEmpty(res))
                    Clipboard.SetText(res);
            }

            // copy for remoto (obsolete)
            /*
            if (selected == "copy selected for remoto")
            {
                List<canMessage2> ls = new List<canMessage2>();
                foreach (DataGridViewRow row in grid.Rows)
                    if (row.Selected)
                        ls.Add(conv.row2msg(row));

                string res = Tools.ConvertMessageToRemotoCmd(ls, true);
                if (!string.IsNullOrEmpty(res))
                    Clipboard.SetText(res);
            }
            */

            // load
            if (selected == "load")
            {
                menu.Hide();

                OpenFileDialog dlg = new OpenFileDialog();
                dlg.DefaultExt = "txt";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.Filter = "CAN Analyzer trace list|*txt";
                //dlg.InitialDirectory =
                //    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) +
                //    "\\" + Application.ProductName;


                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    string[] lines = File.ReadAllLines(dlg.FileName);

                    if (lines != null && lines.Length > 0)
                    {
                        int idx_cnt = 1;
                        for (int i = 0; i < lines.Length; i++)
                        {
                            object[] values = canTraceUtils.mConverter.str2row(lines[i], idx_cnt);

                            if (values != null)
                            {
                                grid.Rows.Add(values);
                                idx_cnt++;
                            }
                        }

                        updateMsgCounter();
                        updateButtons();
                    }
                    /*
                    //grid.Rows[3].DefaultCellStyle.BackColor = Color.LightBlue;
                    canMessage2[] m = new canMessage2[grid.RowCount];
                    for (int i = 0; i < m.Length; i++)
                        m[i] = conv.row2msg(grid.Rows[i]);

                    canTraceUtils.requestParser.requestResponse.parse(m);
                    */
                }                  
            }
        }
        #endregion


        private void btnSendStep_Click(object sender, EventArgs e)
        {
            // send selected message
            var sel = grid.SelectedRows;
            if( sel.Count >= 1)
            {
                var row = sel[0];
                canAnalyzer.canMessage2 m = conv.row2msg(row);
                
                if( CanTool.SendCanMessage(m) )
                    selectNextRow();
            }
        }

        private void btnPlay_Click(object sender, EventArgs e)
        {
            if (sendWrkr.IsRunning)
            {
                sendWrkr.stop(false);
                //btnPlay.Text = "Play";
            }
            else
            {
                //sendWrkr.stop();

                List<canMessage2> ls = new List<canMessage2>();
                foreach (DataGridViewRow row in grid.Rows)
                        ls.Add(conv.row2msg(row));

                if (ls.Count > 0)
                {
                    sendWrkr.send(ls);
                    //btnPlay.Text = "Pause";
                }
            }
        }

        private void btnSendRange_Click(object sender, EventArgs e)
        {
            int from = Convert.ToInt32(tbPlayFrom.Text) - 1;
            int to = Convert.ToInt32(tbPlayTo.Text) - 1;

            if( from >= 0 && from < to && to < grid.RowCount)
            {
                List<canMessage2> ls = new List<canMessage2>();
                for(int m = from; m <= to; m++)
                    ls.Add(conv.row2msg(grid.Rows[m]));

                if (ls.Count > 0)
                {
                    selectRow(from);
                    sendWrkr.send(ls);
                    //btnPlay.Text = "Pause";
                }
            }
        }

        private void updateButtons ()
        {
            bool isRunning = traceMode.pause != mode;

            btnTrace.Image = isRunning ? Properties.Resources.icon_pause : Properties.Resources.icon_record;
            btnSendStep.Enabled = /*!isRunning && */grid.RowCount > 0;
            btnPlay.Enabled = /*!isRunning && */grid.RowCount > 0;
            btnSendRange.Enabled = /*!isRunning &&*/ grid.RowCount > 0;
            btnClear.Enabled = /*!isRunning && */grid.RowCount > 0;

            tbPlayFrom.Enabled = btnPlay.Enabled;
            tbPlayTo.Enabled = btnPlay.Enabled;
            lblSendFrom.Enabled = btnPlay.Enabled;
            lblSendTo.Enabled = btnPlay.Enabled;
        }

        private void btnTraceAll_Click(object sender, EventArgs e)
        {
            // start
            if (traceMode.pause == mode)
            {
                mode = 0 == cbTraceMode.SelectedIndex ? traceMode.traceAll : traceMode.traceSelected;   
            }
            else
            {
                mode = traceMode.pause;
            }

            updateButtons();
        }

        private void cbTraceMode_SelectedIndexChanged(object sender, EventArgs e)
        {
            if( traceMode.pause != mode)
                mode = 0 == cbTraceMode.SelectedIndex ? traceMode.traceAll : traceMode.traceSelected;
        }

        private void tableLayoutPanel1_Paint(object sender, PaintEventArgs e)
        {

        }

        private void btnClear_Click(object sender, EventArgs e)
        {
            clear();
        }

        private void grid_CellContentClick(object sender, DataGridViewCellEventArgs e)
        {

        }



        class sessionWriter
        {
            List<canMessage2> msg_list = new List<canMessage2>();
        }
    }

}

namespace canTraceUtils
{
    class timestamp_offset
    {
        private int offset = -1;

        // update a value
        public int update(int ts)
        {
            if (offset < 0 || ts < offset)
                offset = ts;

            return ts > offset ? ts - offset : 0;
        }

        // reset to defaults
        public void reset()
        {
            offset = -1;
        }
    }

    /*
    // timestamp 
    class timestamp
    {
        #region Fields
        private int tsOffset;
        private int tsMul;
        private int tsPrev;
        #endregion

        #region Constructor
        public timestamp()
        {
            reset();
        }
        #endregion

        #region Public Methods

        // update a value
        public int update(int ts)
        {
            int ts_in = ts;

            if (tsOffset == -1)
            {
                tsOffset = ts;
                return 0;
            }

            ts -= tsOffset;
            ts += 59999 * tsMul;

            if (ts < tsPrev && tsPrev > 0)
            {
                tsMul++;
                ts += 59999;
            }

            tsPrev = ts;

            return ts;
        }

        // reset to defaults
        public void reset ()
        {
            tsOffset = -1;  
            tsMul = 0;
            tsPrev = -1;
        }
        #endregion
    }
    */

    class mConverter
    {
        #region Fields

        //public timestamp TS { set; get; }
        public timestamp_offset TS { set; get; }
        static private readonly int tsLenMin  = 12;
        static private readonly int idLenMin  = 12;
        static private readonly int dlcMinLen = 5;

        public timestamp_offset ts_offset { set; get; }

        #endregion

        // conver a message to a row
        public object[] msg2row(canAnalyzer.canMessage2 msg, int idx)
        {
            return msg2row(msg, TS, idx);
        }


        private static string msecTs2String (int ts)
        {
            string sTs = string.Format("{0}{1} s.",
                ts >= 10000 ? string.Empty : " ",       // extra space
                ((double)ts / 1000).ToString("F3"));
            return sTs;
        }
       
        // convert a message to a row (obsolete)
        /*
        static public object[] msg2row (canAnalyzer.canMessage2 msg, timestamp ts, int idx)
        {
            int dataIdx = 0;

            string[] data = msg.GetDataStringList().ToArray();

            int tsVal = ts.update(msg.TimeStamp.TimeStamp);

            // timestamp string
            string sTs = msecTs2String(tsVal);

            // ts, id, dlc, data
            object[] values = {
                idx,
                sTs,
                msg.Id.GetIdAsString(),
                msg.Id.GetDlcAsString(),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
            };

            return values;
        }
        */

        // convert a message to the row (new)
        static public object[] msg2row(canAnalyzer.canMessage2 msg, timestamp_offset ts, int idx)
        {
            int dataIdx = 0;

            string[] data = msg.GetDataStringList().ToArray();

            //int tsVal = ts.update(msg.TimeStamp.TimeStamp);
            int tsVal = ts.update(msg.timestamp_absolute);

            // timestamp string
            string sTs = msecTs2String(tsVal);

            // ts, id, dlc, data
            object[] values = {
                idx,
                sTs,
                msg.Id.GetIdAsString(),
                msg.Id.GetDlcAsString(),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
            };

            return values;
        }

        // convert objects to string
        public string obj2str(object[] ls, bool add_0x_prefix_for_id = false)
        {
            int cellIdx = 1;
            string prefix_0x = add_0x_prefix_for_id ? "0x" : string.Empty;
            string sTs = ls[cellIdx++].ToString().PadRight(tsLenMin);
            string sId = prefix_0x + ls[cellIdx++].ToString().PadRight(idLenMin - prefix_0x.Length);
            string sDlc = ls[cellIdx++].ToString().PadRight(dlcMinLen);

            string res = sTs + sId + sDlc;
            for (int i = cellIdx; i < ls.Length; i++)
            {
                if (ls[i] == null)
                    break;
                string s = ls[i].ToString();
                if (string.IsNullOrEmpty(s))
                    break;
                if (i > cellIdx)
                    res += ", ";
                res += "0x" + s;
            }

            return res;
        }

        // convert a row to a string
        public string row2str(DataGridViewRow row)
        {
            int cellIdx = 1;
            string sTs  = row.Cells[cellIdx++].Value.ToString().PadRight(tsLenMin);
            string sId  = ("0x" + row.Cells[cellIdx++].Value.ToString()).PadRight(idLenMin);
            string sDlc = row.Cells[cellIdx++].Value.ToString().PadRight(dlcMinLen);

            string res = sTs + sId + sDlc;
            for (int i = cellIdx; i < row.Cells.Count; i++)
            {
                if (row.Cells[i].Value == null)
                    break;

                string s = row.Cells[i].Value.ToString();
                if (string.IsNullOrEmpty(s))
                    break;
                if (i > cellIdx)
                    res += ", ";
                res += "0x" + s;
            }

            return res;          
        }

        static public object[] str2row(string str, int idx)
        {
            var reg = Regex.Match(str, @"(\d+\.\d{3} s.)\W+0x([0-9A-F]{3,})\W*(\d)");
            if (reg.Success)
            {
                string ts = reg.Groups[1].Value.ToString();
                string ecu = reg.Groups[2].Value.ToString();
                string dlc = reg.Groups[3].Value.ToString();
            
                var matches = Regex.Matches(str, @"0x[0-9A-F]*");

                if (matches.Count - 1 == Convert.ToInt32(dlc))
                {
                    object[] val = new object[matches.Count + 3];
                    val[0] = idx;
                    val[1] = ts;
                    val[2] = ecu;
                    val[3] = dlc;
                    for (int i = 1; i < matches.Count; i++)
                        val[i + 3] = matches[i].ToString().Replace("0x", "");

                    return val;
                }
            }
            else
            {
                /* parse the outdated format from the MAX's old canhacker */
                var regOld = Regex.Match(str, @"(\d{10}) \d{1} ([0-9a-fA-F]{8}) \d{2} (\d{1}) ([\w\W]*)");
                if (regOld.Success)
                {
                    string ts = regOld.Groups[1].Value.ToString();
                    string ecu = regOld.Groups[2].Value.ToString();
                    string dlc = regOld.Groups[3].Value.ToString();
                    string sdata = regOld.Groups[4].Value.ToString();
                    var matches = Regex.Matches(sdata, @"[0-9a-fA-F]{2}");

                    if (matches.Count == Convert.ToInt32(dlc))
                    {
                        int msec = Convert.ToInt32(ts) / 1000;

                        object[] val = new object[matches.Count + 4];
                        val[0] = idx;
                        val[1] = msecTs2String(msec);
                        val[2] = ecu.Substring(ecu.Length - 3, 3);
                        val[3] = dlc;
                        for (int i = 0; i < matches.Count; i++)
                            val[i + 4] = matches[i].ToString();

                        return val;
                    }
                }
            }

            return null;
        }

        // conver a row to a message
        public canAnalyzer.canMessage2 row2msg(DataGridViewRow row)
        {
            int cellIdx = 1;
            string sTs = row.Cells[cellIdx++].Value.ToString();
            sTs = sTs.Replace(" s.", "");
            int ts = (int)(Convert.ToDouble(sTs) * 1000);

            canAnalyzer.canMessageId id = new canAnalyzer.canMessageId(
                row.Cells[cellIdx++].Value.ToString(), 
                Convert.ToInt32(row.Cells[cellIdx++].Value.ToString())
            );

            List<byte> data = new List<byte>();

            for (int i = cellIdx; i < row.Cells.Count; i++)
            {
                object cell_val = row.Cells[i].Value;
                if (cell_val == null)
                    break;
                string sdata = cell_val.ToString();
                if (string.IsNullOrEmpty(sdata))
                    break;
                byte b = Convert.ToByte(row.Cells[i].Value.ToString(), 16);
                data.Add(b);
            }

            return new canAnalyzer.canMessage2(id, data.ToArray(), ts);
        }
        
        // header
        public string getHeaderString ()
        {
            return "timestamp".PadRight(tsLenMin) + 
                "ID".PadRight(idLenMin) + "DLC".PadRight(dlcMinLen) + 
                "b0    b1    b2    b3    b4    b5    b6    b7 ";
        }

        // conver a row to the message
        public canAnalyzer.canMessage2 row2message (DataGridViewRow row)
        {
            int cellIdx = 1 + 1;
            string sId = row.Cells[cellIdx++].Value.ToString();
            int id = Convert.ToInt32(sId, 16);

            // format
            bool is29b = sId.Length > 5;    // 0x7FF

            // dlc
            int dlc = Convert.ToInt32(row.Cells[cellIdx++].Value.ToString(), 10);

            // data
            byte[] data = new byte[dlc];
            for (int i = 0; i < dlc; i++)
                data[i] = Convert.ToByte(row.Cells[cellIdx + i].Value.ToString(), 16);

            // timestamp
            string sTs = row.Cells[0].Value.ToString();
            sTs = sTs.Replace(" ", string.Empty);
            sTs = sTs.Substring(0, sTs.IndexOf("s."));
            int ts = (int)(Convert.ToDouble(sTs) * 1000.0d);

            // convert
            canAnalyzer.canMessageId canId = new canAnalyzer.canMessageId(id, dlc, is29b);
            canAnalyzer.canMessage2 canMsg = new canAnalyzer.canMessage2(canId, data, ts);

            return canMsg;
        }


        static public canAnalyzer.canMessage2 str2msg(string str)
        {

            if (str == null || str == string.Empty)
                return null;

            object[] ob = str2row(str, 0);

            if (ob == null || ob.Length == 0)
                return null;


            string sts = ob[1].ToString().Replace(" s.", string.Empty);
            int ts = (int)(Convert.ToDouble(sts) * 1000);
            string sid = ob[2].ToString();
            int id = Convert.ToInt32(sid, 16);
            bool is29bit = sid.Length > 3;
            int dlc = Convert.ToInt32(ob[3]);

            byte[] data = new byte[dlc];
            for (int i = 0; i < data.Length; i++)
                data[i] = Convert.ToByte(ob[4 + i].ToString(), 16);

            canAnalyzer.canMessage2 msg = new canAnalyzer.canMessage2(id, is29bit, data, 0);
            msg.timestamp_absolute = ts;

            return msg;


        }

        // convert message list to string
        public static string msg_list_to_string(List<canAnalyzer.canMessage2> ls, timestamp_offset usr_ts = null, bool add_header = true)
        {
            // converter
            mConverter conv = new canTraceUtils.mConverter();
            // timestamp
            timestamp_offset ts = usr_ts == null ? new canTraceUtils.timestamp_offset() : usr_ts;
            conv.TS = ts;
            // builder
            StringBuilder sb = new StringBuilder();

            // append the header string
            if (add_header)
            {
                sb.Append(conv.getHeaderString());
                sb.Append(Environment.NewLine);
            }

            // append the messages (fast edition)
            for (int i = 0; i < ls.Count; i++)
            {
                var msg = ls[i];

                // update timestamp
                int new_ts = ts.update(msg.timestamp_absolute);

                // pad positins
                int sb_start_pos = sb.Length;
                int pad_cnt;

                // 1) time
                if (new_ts < 10000)
                    sb.Append(' ');
                sb.AppendFormat("{0} s.", ((double)new_ts / 1000).ToString("F3"));

                pad_cnt = sb.Length - sb_start_pos;
                sb.Append(' ', tsLenMin - pad_cnt);

                // 2) CAN id
                sb_start_pos = sb.Length;
                sb.Append("0x");
                sb.Append(msg.Id.GetIdAsString());

                pad_cnt = sb.Length - sb_start_pos;
                sb.Append(' ', idLenMin - pad_cnt);

                // 3) CAN dlc
                sb_start_pos = sb.Length;
                sb.Append(msg.Id.GetDlcAsString());

                pad_cnt = sb.Length - sb_start_pos;
                sb.Append(' ', dlcMinLen - pad_cnt);

                // 4) data, works faster then 'getdatastring'
                if (msg.Data != null && msg.Data.Length > 0)
                {
                    for (int d = 0; d < msg.Data.Length - 1; d++)
                    {
                        sb.Append("0x");
                        sb.Append(msg.Data[d].ToString("X2"));
                        sb.Append(", ");
                    }
                    // the last one
                    sb.Append("0x");
                    sb.Append(msg.Data[msg.Data.Length - 1].ToString("X2"));
                }

                sb.Append(Environment.NewLine);
            }

            // to string
            return sb.ToString();
        }

        // convert a message to a row
        public object[] get_test_row(canAnalyzer.canMessage2 msg, int idx)
        {
            object[] test = { "1", "123s.", "7DF", "8", "00", "01", "03", "04", "05", "06", "07" };
            return test;
        }
    }

    class sendWorker
    {
        private Thread worker;
        private bool stopRequest;
        private List<canAnalyzer.canMessage2> msgList;
        private canAnalyzer.CanMessageSendTool CanTool;
        private readonly canAnalyzer.UcCanTrace parent;

        public bool IsRunning { get { return worker != null && worker.IsAlive; } }

        public sendWorker(canAnalyzer.CanMessageSendTool can, canAnalyzer.UcCanTrace Parent)
        {
            CanTool = can;
            parent = Parent;
        }

        public void stop (bool block = true)
        {
            stopRequest = true;

            if (block)
            {
                while (IsRunning)
                    Thread.Sleep(100);
            }
        }

        public void send (List<canAnalyzer.canMessage2> ls)
        {
            stop();

            msgList = ls;

            worker = new Thread(onWorker);
            worker.Name = "traceWorker";
            worker.Start();
        }

        public void send(canAnalyzer.canMessage2 m)
        {
            stop();

            msgList = new List<canAnalyzer.canMessage2>(1);
            msgList.Add(m);

            worker = new Thread(onWorker);
            worker.Name = "traceWorker";
            worker.Start();
        }

        private void onWorker()
        {
            stopRequest = false;
            int msgIdx = 0;
            bool allMessagesAreSent = false;

            while ( !stopRequest && !allMessagesAreSent )
            {
                // get
                canAnalyzer.canMessage2 msg = msgList[msgIdx];
                // send
                CanTool.SendCanMessage(msg);
                // check
                if ( msgIdx == msgList.Count - 1 )
                {
                    allMessagesAreSent = true;
                    continue;
                }

                int delayMs = msgList[msgIdx + 1].TimeStamp.TimeStamp - msg.TimeStamp.TimeStamp;
                if (delayMs > 0)
                {
                    const int delayStep = 50;
                    if( delayMs < delayStep)
                        Thread.Sleep(delayMs);
                    else
                    {
                        while( !stopRequest )
                        {
                            if (delayMs > delayStep)
                            {
                                Thread.Sleep(delayStep);
                                delayMs -= delayStep;
                            }
                            else
                            {
                                Thread.Sleep(delayMs);
                                break;
                            }
                        }
                    }
                }

                if (stopRequest)
                    break;

                msgIdx++;

                parent.selectNextRow();
            }

            stopRequest = false;
            Thread.Sleep(10);

            //worker.Abort();
           // if(allMessagesAreSent)
                //MessageBox.Show(string.Format("{0} messages were sent", msgList.Count), "Tracer");
        }
    }
}

