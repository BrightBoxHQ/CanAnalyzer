using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Windows.Forms;

// utils
namespace canAnalyzer
{
    // the region contains a class allows us to bind a CAN message and its row
    #region Row Find Tool
    // the container for a message and its unique datagridviewrow
    // this way allows us to dramatically improve performance
    public class gridRow
    {
        public canMessageId Msg { get; }
        public DataGridViewRow Row { get; }

        // constructor
        public gridRow(canMessageId message, DataGridViewRow row)
        {
            Msg = message;
            Row = row;
        }

        // hash code
        public long GetHashCodeUnique()
        {
            return Msg.GetHashCodeUnique();
        }
    }

    // the list for gridwors
    public class gridRowList
    {
        // dictionary
        // in 1 - hashcode based key
        // in 2 - gridrow
        private Dictionary<long, gridRow> dict;  

        public int Count { get { return dict.Count; } }

        // constructor
        public gridRowList()
        {
            dict = new Dictionary<long, gridRow>();
        }

        // add a new id and a row
        public void add(int id, int dlc, bool is29b, DataGridViewRow row)
        {
            canMessageId m = new canMessageId(id, dlc, is29b);
            add(new gridRow(m, row));
        }
        // add a new id and a row
        public void add(gridRow item)
        {
            var gr = find(item.Msg);
            if (null == gr)
                dict.Add(item.Msg.GetHashCodeUnique(), item);
        }

        // get a row with the can id
        // returns null in case the id doesn't exist
        public DataGridViewRow getRow(canMessageId m)
        {
            var gr = find(m);
            if (gr != null)
                return gr.Row;
            return null;
        }

        // try to find a gridRow
        public gridRow find(canMessageId m)
        {
            long key = m.GetHashCodeUnique();
            if(dict.ContainsKey(key))
                return dict[key];
            return null;
        }

        public gridRow GetAt (int idx)
        {
            if (idx >= 0 && idx < Count)
                return dict.ElementAt(idx).Value;
            return null;
        }

        // clear
        public void clear()
        {
            dict.Clear();
        }
    }
    #endregion

    // the region contains a grid cells backlight tool
    #region BackLight Tool
    // backligt message
    // we're utilizing it as a data container class for the backlight tool
    public class gridBackLightMessage
    {
        // grid row
        public gridRow Row { get; }
        // timestamp buffer
        public int[] Ts { get; }
        // data buffer
        private byte[] Data;
        // flag (is a row backlighted)
        public bool BacklightFlag { get; set; }
        // bl period (msec)
        public int Period { get; }
        // bl check step (msec)
        public int Step { get; }

        public gridBackLightMessage(gridRow msg, byte[] data, int period, int step)
        {
            // config
            Row = msg;
            Period = period;
            Step = step;

            // create the buffers
            Data = new byte[msg.Msg.Dlc];
            Ts = new int[msg.Msg.Dlc];

            for (int i = 0; i < Ts.Length; i++)
            {
                Ts[i] = Period;         // backlight all the data
                
                if ( i >= data.Length || i >= Data.Length)
                    throw new System.ArgumentException("gridBackLightMessage error", "gridBackLightMessage");
                else
                    Data[i] = data[i];
            }

            // reset the flag (it should by set by parent)
            BacklightFlag = false;
        }

        // decrement
        public void decrement()
        {
            for (int i = 0; i < Ts.Length; i++)
                Ts[i] = Ts[i] > 0 ? Ts[i] - Step : 0;
        }

        // do we need to backligth at least one cell?
        public bool areNewData()
        {
            foreach (var t in Ts)
                if (t > 0)
                    return true;
            return false;
        }

        // do update
        public bool update(List<byte[]> dataList)
        {
            bool areWewData = false;

            // for each data byte
            for (int bNum = 0; bNum < Row.Msg.Dlc; bNum++)
            {
                // check all the data buffers from the end
                for (int item = dataList.Count - 1; item >= 0; item--)
                {
                    var curByte = Data[bNum];
                    var newByte = dataList[item][bNum];
                    // is a new data?
                    if (curByte != newByte )
                    {
                        // apply
                        Data[bNum] = newByte;
                        Ts[bNum] = Period;
                        areWewData = true;
                        // skip this bNum and check the next one
                        break;
                    }
                }
            }
            
            return areWewData;
        }

        public long GetHashCodeUnique()
        {
            return Row.GetHashCodeUnique();
        }
    }

    // backlight tool
    public class gridBacklightTool
    {
        // timer
        private System.Windows.Forms.Timer tmrBacklight;
        // storage
        private Dictionary<long, gridBackLightMessage> dict;
        // intervals
        private readonly int Interval = 1000;
        private readonly int BacklightFor = 5000;
        // data offset
        private readonly int ColumnDataIdxOffset;


        // constructor
        public gridBacklightTool(canDataGrid parent, int dataOffsetIdx)
        {
            ColumnDataIdxOffset = dataOffsetIdx;
            dict = new Dictionary<long, gridBackLightMessage>();
        }

        // push a new message or update an existing one
        public void push (gridRow msg, List<byte[]> dataList)
        {
            long key = msg.GetHashCodeUnique();
            bool exists = dict.ContainsKey(key);

            if ( exists )
            {
                dict[key].update(dataList);
            }
            else
            {
                // create a new one
                gridBackLightMessage item = 
                    new gridBackLightMessage(msg, dataList[dataList.Count-1], BacklightFor, Interval);
                dict.Add(key, item);       
            }

            // backlight it immideately
            DoBacklight(dict[key]);
        }

        // start
        public void start()
        {
            tmrBacklight = new System.Windows.Forms.Timer();
            tmrBacklight.Tick += new EventHandler(OnTimedEvent);
            tmrBacklight.Interval = Interval;
            tmrBacklight.Enabled = true;
            // call the callback once
            OnTimedEvent(null, null);
        }

        // stop
        public void stop()
        {
            tmrBacklight.Enabled = false;
        }

        // clear
        public void clear()
        {
            dict.Clear();
        }

        // do backlight
        private void DoBacklight(gridBackLightMessage item)
        {
            // colors
            Color set = Color.Yellow;
            Color reset = item.Row.Row.Cells[ColumnDataIdxOffset - 1].Style.BackColor;

            int[] timestamps = item.Ts;

            item.BacklightFlag = item.areNewData();

            for ( int i = 0; i < timestamps.Length; i++)
            {
                var cell = item.Row.Row.Cells[ColumnDataIdxOffset + i];
                bool needToBl = timestamps[i] > 0;
                cell.Style.BackColor = needToBl ? set : reset;
                cell.Style.SelectionForeColor = needToBl ? set : reset;               
            }
        }

        // scan timer
        private void OnTimedEvent(object source, EventArgs e)
        {
            // for every message
            for (int i = 0; i < dict.Count; i++)
            {
                var item = dict.ElementAt(i).Value;

                if (item.areNewData())
                {
                    DoBacklight(item);
                    // update
                    item.decrement();
                }
                else if (item.BacklightFlag)
                {
                    DoBacklight(item);
                    item.BacklightFlag = false;
                }
            }
        }
    }

    #endregion

    // the region contains a data tip tool
    #region Data Tips Tool
    // tips class
    // allows us to take a look on some previous data
    public class GridTipTool
    {
        // private DataGridView grid;
        static public readonly int MaxValues = 5;
        static private readonly string strSeparator = "\r\n";

        static private string getCurValString(string txt)
        {
            string res = string.Empty;
            bool sepIsFound = false;

            int startIdx = txt.LastIndexOf(strSeparator);
            sepIsFound = startIdx >= 0;

            if (startIdx < 0 && !string.IsNullOrEmpty(txt))
                startIdx = 0;

            if (startIdx >= 0)
            {
                int endIdx = txt.IndexOf(' ', startIdx + 1);
                if (endIdx > startIdx)
                {
                    if (sepIsFound)
                        startIdx += strSeparator.Length;
                    res = txt.Substring(startIdx, endIdx - startIdx);
                }
            }

            return res;
        }

        static private int getCurVal(string txt)
        {
            string sVal = getCurValString(txt);
            int res = 0;
            return Tools.tryParseInt(sVal, out res) ? res : -1;
        }

        static private string removeOldData(string txt)
        {
            // remove
            //int total = Regex.Matches(txt, strSeparator).Count;

            int total = 0;
            int j = -1;
            while (true)
            {
                j = txt.IndexOf('\n', j + 1);
                if (j >= 0)
                    total++;
                else
                    break;
            }
            int itemsToRemove = total - MaxValues + 1;
            if (itemsToRemove > 0)
            {
                int idx = -1;
                for (int i = 0; i < itemsToRemove; i++)
                    idx = txt.IndexOf(strSeparator, idx + 1);
                txt = txt.Substring(idx + strSeparator.Length);
            }

            return txt;
        }

        static public void tipTextUpdate(DataGridViewRow row, int startCol, List<byte[]> list)
        {
            if (list == null || list.Count == 0)
                return;

            // get dlc
            int dlc = 0;
            if (list[0] != null && list[0].Length > 0)
                dlc = list[0].Length;

            // for each byte
            for (int i = 0; i < dlc; i++)
            {
                var cell = row.Cells[startCol + i];
                cell.ToolTipText = tipTextUpdate(cell, list, i);
            }
        }

        static public string tipTextUpdate(DataGridViewCell cell, List<byte[]> list, int index)
        {
            string tip = cell.ToolTipText;

            // check
            if (cell == null || list == null || list.Count == 0)
                return tip;

            int curVal = getCurVal(tip);

            // add all the data, and then remove necessary ones
            for (int i = 0; i < list.Count; i++)
            {
                var buff = list[i];
                if (buff == null || buff.Length <= index)
                    continue;

                int newVal = list[i][index];
                if (curVal != newVal)
                {
                    curVal = newVal;
                    tip += string.IsNullOrEmpty(tip) ? makeTipString(curVal) :
                        strSeparator + makeTipString(curVal);
                }
            }

            // remove
            tip = removeOldData(tip);

            return tip;
        }

        static private string makeTipString(string hex)
        {
            // format: hex bin dec
            return string.Format("0x{0} ({1})b {2}",
                        hex,
                        Tools.hex2bin(hex),
                        Convert.ToInt32(hex, 16)
                    );
        }

        static private string makeTipString(int val)
        {
            // format: hex bin dec
            return string.Format("0x{0} ({1})b {2}",
                        val.ToString("X2"),
                        Tools.hex2bin(val),
                        val
                    );
        }

    }
    #endregion

    // the region contains a parser class and some its utils (for performance)
    #region Data Parser
    // grid message
    public class gridMessage
    {
        // data string
        public string Data { get; set; }
        // period
        private canPeriod CanPeriod;
        // period val
        public int Period {
            get {
                return CanPeriod.MilliSec;
            }
        }
        // count
        public int Count { get { return counter/*DataListBuff.Count*/; } }
        // id
        public canMessageId Id { get; }
        // data list
        public List<byte[]> DataListBuff { get; }

        private int counter = 0;

        // constructor
        public gridMessage(canMessage2 msg)
        {
            // id
            Data = msg.GetDataString(" ");
            Id = msg.Id;
            // data
            DataListBuff = new List<byte[]>();
            DataListBuff.Add(msg.Data);
            // period
            CanPeriod = null;
            // counter 
            counter = 1;
        }

        // update
        public void update(canMessage2 msg)
        {

            // debug
            // if ( msg.Id.GetHashCodeUnique() != Id.GetHashCodeUnique() )
            // {
            //     int err = 0;
            // }

            // data
            if ( DataListBuff.Count < GridTipTool.MaxValues )
                DataListBuff.Add(msg.Data);

            // data string
            if (string.IsNullOrEmpty(Data))
                Data = msg.GetDataString(" ");

            counter++;
        }

        public void updatePeriod(canMessage2 msg)
        {
            if (null == CanPeriod)
                CanPeriod = new canPeriod(msg.TimeStamp);
            else  
                CanPeriod.Update(msg.TimeStamp);
        }

        // clear
        public void ClearDataBuff ()
        {
            DataListBuff.Clear();
            Data = string.Empty;
        }
    }

    public class gridList
    {
        // storage
        private Dictionary<long, gridMessage> dict;
        
        // get as list
        public List<gridMessage> List { get { return dict.Values.ToList(); } }

        // constructor
        public gridList()
        {
            dict = new Dictionary<long, gridMessage>();
        }

        // get a grid message
        public gridMessage getGridMessage (canMessage2 item)
        {
            long key = item.GetHashCodeUnique();
            if (dict.ContainsKey(key))
                return dict[key];
            return null;
        }

        // add a new item or update an existing one
        public void Add(canMessage2 item)
        {
            long key = item.GetHashCodeUnique();

            if (dict.ContainsKey(key))
                dict[key].update(item);
            else
                dict.Add(key, new gridMessage(item));
        }

        // update period
        public void UpdatePeriod(canMessage2 item)
        {
            long key = item.GetHashCodeUnique();
            if (dict.ContainsKey(key))
                dict[key].updatePeriod(item);
        }

        // clear
        public void ClearDataBuff()
        {
            foreach (var i in dict)
                i.Value.ClearDataBuff();
        }

        // clear
        public void Clear()
        {
            dict.Clear();
        }
    }

    // data parser
    public class GridParser 
    {
        private gridList res = new gridList();

        // parser
        public gridList Parse(ref List<canMessage2> ls)
        {
            res.ClearDataBuff();

            // for all the messages from the ending to the beginning
            for( int i = ls.Count - 1; i >= 0; i-- )
                res.Add(ls[i]);

            // and the from beginning to the end to get a period value
            for (int i = 0; i < ls.Count; i++)
                res.UpdatePeriod(ls[i]);
            
            return res;
        }

        // clear
        public void Clear()
        {
            res.Clear();
        }

        // get
        public gridList GetList ()
        {
            return res;
        }
    }
    #endregion
}

// grid
namespace canAnalyzer
{  
    // a specific CAN data grid class
    public class canDataGrid
    {
        // the region contains a class fields
        #region Fields

        // grid & columns
        private readonly DataGridView m_grid;                // grid
        // columns
        private readonly DataGridViewColumn m_colEnable;     // checkbox
        private readonly DataGridViewColumn m_colId;         // can id
        private readonly DataGridViewColumn m_colDLC;        // dlc
        private readonly DataGridViewColumn m_colData;       // 1st data col (8 in summary)
        private readonly DataGridViewColumn m_colPeriod;     // period
        private readonly DataGridViewColumn m_colCount;      // counter

        // context menu
        public ContextMenuStrip contextMenuStrip { set { m_grid.ContextMenuStrip = value; } }

        // filter
        public canFilter CanFilter { get; set; }    // filter
        private gridBacklightTool BacklightTool;    // backlight tool

        // row container
        private gridRowList RowList = new gridRowList();
        // parser
        private GridParser Parser = new GridParser();

        /* disable tools for debug purporses */
        private readonly bool use_tools = true;

        #endregion

        // the region contains a class constructor
        #region Constructor
        // constructor
        public canDataGrid(UserControl parent)
        {
            // create
            m_grid = new DataGridView();
            
            // config
            m_grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            m_grid.Dock = DockStyle.Fill;
            m_grid.Location = new System.Drawing.Point(0, 0);
            m_grid.Name = "can";
            m_grid.RowTemplate.Height = 25; // 21
            m_grid.TabIndex = 0;

            m_grid.BorderStyle = BorderStyle.None;
            m_grid.RowHeadersVisible = false;
            m_grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            m_grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            m_grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;

            
            // col: cb, id, dlc, data, period

            // enable
            DataGridViewCheckBoxColumn cb = new DataGridViewCheckBoxColumn();
            m_grid.Columns.Add(cb);
            m_colEnable = m_grid.Columns[m_grid.ColumnCount - 1];

            // ID
            m_grid.Columns.Add("CAN ID", "CAN ID");
            m_colId = m_grid.Columns[m_grid.ColumnCount - 1];
            // DLC
            m_grid.Columns.Add("DLC", "DLC");
            m_colDLC = m_grid.Columns[m_grid.ColumnCount - 1];
            // Data          
            for (int i = 0; i < 8; i++)
            {
                string title = i.ToString();
                m_grid.Columns.Add(title, title);
                m_grid.Columns[m_grid.ColumnCount - 1].Width = 32;
                if (0 == i)
                    m_colData = m_grid.Columns[m_grid.ColumnCount - 1];
            }

            // Period
            m_grid.Columns.Add("Period", "Period");
            m_colPeriod = m_grid.Columns[m_grid.ColumnCount - 1];
            // Count
            m_grid.Columns.Add("Count", "Count");
            m_colCount = m_grid.Columns[m_grid.ColumnCount - 1];

            // width
            m_colEnable.Width = 35;
            m_colId.Width = 95;
            m_colDLC.Width = 50;
            m_colPeriod.Width = 60;
            m_colCount.Width = 65;

            // alignment
            foreach (DataGridViewColumn col in m_grid.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            // restrictions
            m_grid.AllowUserToResizeRows = false;
            m_grid.AllowUserToResizeColumns = false;
            m_grid.AllowUserToAddRows = false;

            // style
            m_grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            m_grid.AdvancedCellBorderStyle.Top = DataGridViewAdvancedCellBorderStyle.None;
            m_grid.AdvancedCellBorderStyle.Left = DataGridViewAdvancedCellBorderStyle.None;
            m_grid.AdvancedCellBorderStyle.Right = DataGridViewAdvancedCellBorderStyle.None;

            m_grid.ColumnHeadersDefaultCellStyle.Alignment =
                DataGridViewContentAlignment.MiddleLeft;

            m_grid.CellMouseUp += myDataGrid_OnCellMouseUp;

            m_grid.BorderStyle = BorderStyle.None;
            m_grid.AlternatingRowsDefaultCellStyle.BackColor = Color.FromArgb(238, 239, 249);
            m_grid.CellBorderStyle = DataGridViewCellBorderStyle.SingleHorizontal;
            m_grid.DefaultCellStyle.SelectionBackColor = Color.DarkTurquoise;
            m_grid.DefaultCellStyle.SelectionForeColor = Color.Black;//Color.WhiteSmoke;
            m_grid.BackgroundColor = Color.White;
            m_grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;

            m_grid.ScrollBars = ScrollBars.Vertical;

            // header
            foreach (DataGridViewColumn col in m_grid.Columns)
                col.HeaderCell.Style.Font = new Font("Calibri", 8.6f, FontStyle.Bold);
            // data
            m_grid.DefaultCellStyle.Font = new Font("Consolas", 9.0f, FontStyle.Italic);

            parent.Controls.Add(m_grid);

            m_grid.SortCompare += grigSortCompare;
            m_grid.SelectionChanged += dataGridView1_SelectionChanged;

            m_grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.EnableResizing;
            m_grid.RowHeadersVisible = false;


            // tests for performance
            m_grid.RowHeadersWidthSizeMode = DataGridViewRowHeadersWidthSizeMode.DisableResizing; //or even better .DisableResizing. Most time consumption enum is DataGridViewRowHeadersWidthSizeMode.AutoSizeToAllHeaders
            // set it to false if not needed
            m_grid.RowHeadersVisible = false;
            m_grid.AutoSizeRowsMode = DataGridViewAutoSizeRowsMode.None;
            for (int i = 0; i < m_grid.Columns.Count; i++)
                m_grid.Columns[i].AutoSizeMode = DataGridViewAutoSizeColumnMode.None;


            // backlight tool
            if (use_tools)
            {
                BacklightTool = new gridBacklightTool(this, m_colData.Index);
                BacklightTool.start();
            }

            // this line is necessary because of performance
            Tools.SetDoubleBuffered(m_grid, true);
        }
        //-----------------------------------------------------------------------
        #endregion

        // the region contains add and update methods
        #region Modifiers

        public void updateCheckboxesWithFilter ()
        {
            foreach (DataGridViewRow row in m_grid.Rows)
            {
                canMessage2 m = row2message(row);
                bool show = !CanFilter.Contains(m.Id);
                hideShowRow(row, show);
            }

            doSort();
        }

        // push a message list
        public void push(List<canMessage2> ls)
        {
            if (ls.Count == 0)
                return;

            bool doSortInTheEnd = false;
            // parse
            gridList parsed = Parser.Parse(ref ls);

            // handle
            foreach (gridMessage msg in parsed.List)
            {
                // the message can be empty if we had it before but not now
                if (msg.DataListBuff.Count == 0)
                    continue;

                // get all the data we need
                string sId = msg.Id.GetIdAsString();
                string sDlc = msg.Id.GetDlcAsString();
                string[] data = msg.Data.Split(' ');
                int count = msg.Count;
                int period = msg.Period;

                // get a row for the message
                DataGridViewRow row = getMsgRow(msg.Id);
                bool isRowEnabled = isRowChecked(row);

                // exist? 
                // nope - we have a new message
                // yeap - just update it
                if (null == row)
                {
                    // add a new one
                    row = addNewRow(sId, sDlc, data, period, count);
                    doSortInTheEnd = true;
                    isRowEnabled = true;    // it's enabled now
                    // put the current message and the new row inside the row contaiter (for performance) 
                    RowList.add(new gridRow(msg.Id, row));
                }
                else
                {
                    // update it
                    if (isRowEnabled)
                        updateRow(row, data, period, count);
                }

                // tools
                if (isRowEnabled)
                {
                    // get data for the row
                    gridRow gr = RowList.find(msg.Id);

                    if (use_tools)
                    {
                        // tool 1 - backlight
                        BacklightTool.push(gr, msg.DataListBuff);
                        // tool 2 - tips
                        GridTipTool.tipTextUpdate(row, m_colData.Index, msg.DataListBuff);
                    }
                }
            }

            // sort
            if (doSortInTheEnd)
                doSort();
        }

        // add a new row
        private DataGridViewRow addNewRow(string sId, string sDlc, string[] data, int period, int count)
        {
            m_grid.AllowUserToAddRows = true;

            int dataIdx = 0;

            // cb, id, dlc, data, period, count
            object[] values = { true,
                sId,
                sDlc,
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                ( data.Length >= ++dataIdx ? data[dataIdx-1] : string.Empty ),
                period.ToString(),
                count.ToString()
            };
            m_grid.Rows.Add(values);

            m_grid.AllowUserToAddRows = false;
            m_grid.AllowUserToDeleteRows = false;

            var row = m_grid.Rows[m_grid.Rows.GetLastRow(DataGridViewElementStates.None)];
            // foreach row expect the 1st one (checbox) switch readonly on
            foreach (DataGridViewColumn col in m_grid.Columns)
                row.Cells[col.Index].ReadOnly = m_colEnable != col;

            return row;
        }

        // update an existing row
        private void updateRow(DataGridViewRow row, string[] data, int period, int count)
        {
            // update data cells if need
            for (int dataIdx = 0; dataIdx < data.Length; dataIdx++)
            {
                var dataCell = row.Cells[m_colData.Index + dataIdx];    // get a data cell
                bool update = dataCell.Value != null &&
                    dataCell.Value.ToString() != data[dataIdx];         // do we really need to modify it?
                // update
                if (update)
                    dataCell.Value = data[dataIdx];
            }

            // update the counter value
            row.Cells[m_colCount.Index].Value = count;
               // Convert.ToInt32(row.Cells[m_colCount.Index].Value) + count;

            // period
            if (row.Cells[m_colPeriod.Index].Value == null ||
                row.Cells[m_colPeriod.Index].Value.ToString() != period.ToString())
                row.Cells[m_colPeriod.Index].Value = period;
        }
        //-----------------------------------------------------------------------

        // cleaner
        public void clear ()
        {
            if (use_tools)
            {
                BacklightTool.stop();
                BacklightTool.clear();  // backlight
            }

            m_grid.Rows.Clear();    // grid
            RowList.clear();        // rows
            Parser.Clear();         // parser

            if (use_tools)
                BacklightTool.start();
        }
        //-----------------------------------------------------------------------

        #endregion

        // the region contains some internal utils
        #region Utils (Internal)

        // is a row checked
        private bool isRowChecked(DataGridViewRow row)
        {
            return row == null ? false : Convert.ToBoolean(row.Cells[m_colEnable.Index].Value);
        }
        //-------------------------------------------------------------------------------------------------

        // is a row selected
        private bool isSelected(canMessage msg)
        {
            var row = getMsgRow(msg.getCanIdString(), msg.getDlc());
            return row != null ? row.Selected : false;
        }
        //-----------------------------------------------------------------------

        // is a row backlighted
        private bool isRowBacklighted(DataGridViewRow row)
        {
            Color colorDefault = row.Cells[0].Style.BackColor;
            int counter = 0;

            for (int i = 0; i < 8; i++)
            {
                if (row.Cells[m_colData.Index + i].Style.BackColor != colorDefault)
                    counter++;
            }

            return counter > 0;
        }

        // hide (uncheck) | restore (check) a row
        private void hideShowRow(DataGridViewRow row, bool show)
        {
            row.Cells[m_colEnable.Index].Value = show;
            int canId = Convert.ToInt32(getMsgIdFromRow(row.Index), 16);
            int dlc = getMsgDlcFromRow(row.Index);

            bool is29b = getMsgIdFromRow(row.Index).Length > 3;

            if (show)
                CanFilter.remove(canId, dlc, is29b);
            else
                CanFilter.add(canId, dlc, is29b);
        }
        //-----------------------------------------------------------------------

        // get a row for an appropiate message
        private DataGridViewRow getMsgRow(string canId, int dlc)
        {
            canMessageId id = new canMessageId(canId, dlc);
            DataGridViewRow res = RowList.getRow(id);
            return res;
        }
        //-----------------------------------------------------------------------

        // get a row for an appropiate message
        private DataGridViewRow getMsgRow(canMessageId id)
        {
            DataGridViewRow res = RowList.getRow(id);
            return res;
        }
        //-----------------------------------------------------------------------

        // get dlc value
        private int getMsgDlcFromRow(int row)
        {
            if (row < m_grid.RowCount)
                return getMsgDlcFromRow(m_grid.Rows[row]);

            return 0;
        }
        //-----------------------------------------------------------------------

        // get a dlc value
        private int getMsgDlcFromRow(DataGridViewRow row)
        {
            string s = row.Cells[m_colDLC.Index].Value.ToString();
            return Convert.ToInt32(s);
        }
        //-----------------------------------------------------------------------

        // get an idx value
        private string getMsgIdFromRow(int rowIdx)
        {
            if (rowIdx < m_grid.RowCount)
                return getMsgIdFromRow(m_grid.Rows[rowIdx]);

            return String.Empty;
        }
        //-----------------------------------------------------------------------

        // get an idx value
        private string getMsgIdFromRow(DataGridViewRow row)
        {
            return row.Cells[m_colId.Index].Value.ToString();
        }
        //-----------------------------------------------------------------------

        // get data array from a row
        private byte[] getMsgDataFromRow(DataGridViewRow row)
        {
            List<byte> ls = new List<byte>();

            for( int i = 0; i < 8; i++ )
            {
                string sVal = row.Cells[m_colData.Index + i].Value.ToString();
                if (string.IsNullOrEmpty(sVal))
                    break;
                byte b = Convert.ToByte(sVal, 16);
                ls.Add(b);
            }

            return ls.ToArray();
        }
        //-----------------------------------------------------------------------

        // convert row to message
        private canMessage2 row2message(DataGridViewRow row)
        {
            canMessageId id = new canMessageId(getMsgIdFromRow(row), getMsgDlcFromRow(row));
            canMessage2 msg = new canMessage2(id, getMsgDataFromRow(row));
            return msg;
        }
        //-----------------------------------------------------------------------

        #endregion

        // the region contains some public utils
        #region Utils (Public)

        // returns a list of selected messages
        public UniqueDataSetCan getSelectedMessages2()
        {
            UniqueDataSetCan list = new UniqueDataSetCan();

            for ( int i = 0; i < RowList.Count; i++)
            {
                var row = RowList.GetAt(i);
                if (row.Row.Selected)
                    list.Add(row.Msg);
            }

            return list;
        }
        //-----------------------------------------------------------------------

        // get a min possible width value
        public int getWidthMin()
        {
            int w = 30; // for scroll
            foreach (DataGridViewColumn col in m_grid.Columns)
                w += col.Width;
            return w;
        }
        //-----------------------------------------------------------------------
        #endregion

        // the region contains callbacks for the context menu strip
        #region ContextMenu

        // hide selected messages
        public void hideSelected()
        {
            for (int idx = m_grid.Rows.Count - 1; idx >= 0; idx--)
            {
                var row = m_grid.Rows[idx];     // get
                if (row.Selected)
                {
                    hideShowRow(row, false);    // hide
                    row.Selected = false;       // clear selection
                }
            }
            doSort();
        }
        //-----------------------------------------------------------------------

        // hide all messages but selected
        public void hideAllMessagesButSelected()
        {
            for (int idx = m_grid.Rows.Count -1; idx >= 0; idx--)
            {
                var row = m_grid.Rows[idx];
                if (!row.Selected)
                {
                    hideShowRow(row, false);    // hide
                    row.Selected = false;       // clear selection
                }               
            }
            doSort();
        }
        //-----------------------------------------------------------------------

        // hide backlighted messages
        public void hideBacklighedMessages()
        {
            for( int idx = m_grid.Rows.Count - 1; idx >= 0; idx-- )
            {
                var row = m_grid.Rows[idx];
                if (isRowBacklighted(row))
                {
                    hideShowRow(row, false);
                }
            }
            doSort();
        }
        //-----------------------------------------------------------------------

        // hide all the messages but backlighted
        public void hideAllButBacklighted()
        {
            for (int idx = m_grid.Rows.Count - 1; idx >= 0; idx--)
            {
                var row = m_grid.Rows[idx];
                if (!isRowBacklighted(row))
                    hideShowRow(row, false);
            }
            doSort();
        }
        //-----------------------------------------------------------------------

        // restore all the messages
        public void unhideAll()
        {
            for (int idx = 0; idx < m_grid.Rows.Count; idx++)
            {
                var row = m_grid.Rows[idx];
                hideShowRow(row, true);
            }
            doSort();
        }
        //-----------------------------------------------------------------------

        // hide all the messages
        public void hideAll()
        {           
            for (int idx = m_grid.Rows.Count - 1; idx >= 0; idx-- )
            {
                var row = m_grid.Rows[idx];
                hideShowRow(row, false);
            }
            doSort();
        }
        //-----------------------------------------------------------------------

        // toggle 'checked' property
        public void toggleCheckProperty ()
        {
            foreach (DataGridViewRow row in m_grid.Rows)
            {
                hideShowRow(row, !isRowChecked(row));
            }

            doSort();
        }
        //-----------------------------------------------------------------------

        // toggle 'checked' property
        public void toggleCheckPropertySelected()
        {
            foreach (DataGridViewRow row in m_grid.Rows)
            {
                if (row.Selected)
                    hideShowRow(row, !isRowChecked(row));
            }

            doSort();
        }
        //-----------------------------------------------------------------------

        // get message list
        public List<canMessage2> getMessageList(bool selected = false)
        {
            List<canMessage2> ls = new List<canMessage2>();

            for (int idx = 0; idx < m_grid.Rows.Count; idx++)
            {
                var row = m_grid.Rows[idx];
                if (!selected || (selected && row.Selected))
                    ls.Add(row2message(row));
            }
            return ls;
        }
        //-----------------------------------------------------------------------

        // get an interval value for a message
        public int getMessageInterval(canMessage2 msg)
        {
            gridList list = Parser.GetList();
            var item = list.getGridMessage(msg);
            if (item == null)
                return -1;
            return item.Period;
        }
        //-----------------------------------------------------------------------

        // get an counter value for a message
        public int getMessageCounter(canMessage2 msg)
        {
            gridList list = Parser.GetList();
            var item = list.getGridMessage(msg);
            if (item == null)
                return -1;
            return item.Count;
        }
        //-----------------------------------------------------------------------

        // get a period list for selected messages
        public List<int> getMessageIntervalList(bool selected = false)
        {
            List<int> ls = new List<int>();

            for (int idx = 0; idx < m_grid.Rows.Count; idx++)
            {
                var row = m_grid.Rows[idx];
                if (!selected || (selected && row.Selected))
                    ls.Add(Convert.ToInt32(row.Cells[m_colPeriod.Index].Value));
            }
            return ls;
        }
        //-----------------------------------------------------------------------

        // get a period list for selected messages
        public List<int> getMessageCountList(bool selected = false)
        {
            List<int> ls = new List<int>();

            for (int idx = 0; idx < m_grid.Rows.Count; idx++)
            {
                var row = m_grid.Rows[idx];
                if (!selected || (selected && row.Selected))
                    ls.Add(Convert.ToInt32(row.Cells[m_colCount.Index].Value));
            }
            return ls;
        }
        //-----------------------------------------------------------------------

        #endregion

        // the region contains all the callbacks we have here
        #region Callbacks

        private void dataGridView1_SelectionChanged(object sender, EventArgs e)
        {
            if (m_grid.CurrentCell.ColumnIndex == 0)
                m_grid.CurrentCell.Selected = false;
        }
        //-----------------------------------------------------------------------
        // sorter
        private void grigSortCompare(object sender, DataGridViewSortCompareEventArgs e)
        {
            if (e.RowIndex1 >= 0 && e.RowIndex2 >= 0)
            {
                // 1. compare enabled
                object c1 = e.CellValue1;
                object c2 = e.CellValue2;

                DataGridViewRow r1 = m_grid.Rows[e.RowIndex1];
                DataGridViewRow r2 = m_grid.Rows[e.RowIndex2];

                // order
                int res = m_grid.SortOrder == SortOrder.Ascending ? -1 : 1;

                if (m_colEnable.Index == e.Column.Index)
                {
                    e.SortResult = c1 == c2 ? 0 : (Convert.ToBoolean(c1) == true ? res : -res);
                    e.Handled = true;
                    return;
                }

                // 1. compare enabled
                if (Convert.ToBoolean(r1.Cells[0].Value) != Convert.ToBoolean(r2.Cells[0].Value))
                {
                    e.SortResult = Convert.ToBoolean(r1.Cells[0].Value) == true ? res : -res;
                    e.Handled = true;
                    return;
                }

                // check for null
                if (null == c1 || null == c2)
                {
                    e.SortResult = c1 == c2 ? 0 : (c1 == null ? -1 : 1);
                }
                else
                {
                    // sort data period and data count cols as numbers
                    if (e.Column.Index == m_colPeriod.Index || e.Column.Index == m_colCount.Index)
                    {
                        int v1 = Convert.ToInt32(r1.Cells[e.Column.Index].Value.ToString(), 10);
                        int v2 = Convert.ToInt32(r2.Cells[e.Column.Index].Value.ToString(), 10);
                        e.SortResult = v1 == v2 ? 0 : (v1 > v2 ? 1 : -1);
                    }
                    // otherwise as strings
                    else
                        e.SortResult =
                            string.Compare(c1.ToString(), c2.ToString(), StringComparison.InvariantCulture);
                }

                // extra sort by canId in case 2 values equals
                if (0 == e.SortResult && e.Column.Index > m_colId.Index)
                {
                    string s1 = r1.Cells[1].Value.ToString();
                    string s2 = r2.Cells[1].Value.ToString();
                    e.SortResult =
                        string.Compare(s1, s2,
                            StringComparison.InvariantCulture);
                }
                e.Handled = true;
            }
        }
        //-----------------------------------------------------------------------

        private void myDataGrid_OnCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == m_colEnable.Index && e.RowIndex >= 0)
            {
                // make sure we have a correct row index
                if (e.RowIndex >= 0 && e.RowIndex < m_grid.RowCount)
                {
                    // get a row
                    DataGridViewRow row = m_grid.Rows[e.RowIndex];
                    // data
                    string sId = getMsgIdFromRow(row);
                    int id = Convert.ToInt32(sId, 16);
                    int dlc = getMsgDlcFromRow(row);
                    bool is29b = sId.Length > 3;
                    
                    // filter
                    if (isRowChecked(row))
                        CanFilter.add(id, dlc, is29b);
                    else
                        CanFilter.remove(id, dlc, is29b);

                    // sort
                    doSort();
                }
            }
        }
        //-----------------------------------------------------------------------

        // sorter
        private void doSort()
        {
            DataGridViewColumn col4Sort = 
                null == m_grid.SortedColumn ? m_colId : m_grid.SortedColumn;
            m_grid.Sort(col4Sort, m_grid.SortOrder == SortOrder.Ascending ? 
                ListSortDirection.Ascending : ListSortDirection.Descending);
        }
        //-----------------------------------------------------------------------

        #endregion
    }
    //-----------------------------------------------------------------------
}

