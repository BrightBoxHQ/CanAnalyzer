using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Windows.Forms;
using System.Diagnostics;

namespace canAnalyzer
{

    public partial class UcSendMessage : UserControl
    {
        //private FrmMain parent;
        private canSender sender;

        private DataGridViewColumn m_colEnable;    // checkbox
        private DataGridViewColumn m_colId;        // can id
        private DataGridViewColumn m_colDLC;       // dlc
        private DataGridViewColumn m_colData;      // 1st data col (8 in summary)
        private DataGridViewColumn m_colTrigger;   // period
        private DataGridViewColumn m_colMods;
        private DataGridViewColumn m_colCount;     // counter

        private CanMessageSendTool CanTool;

        // restore
        public void onConnect()
        {
            foreach (DataGridViewRow r in grid.Rows) {
                bool start = Convert.ToBoolean(r.Cells[m_colEnable.Index].Value);
                startStopMessage(getMessageWithRow(r), start);
            }
        }

        // pause
        public void onDisconnect()
        {
            for (int i = 0; i < items.Count; i++)
                startStopMessage(items[i], false);
        }

        private DataGridViewRow getRow(canSendWorker item, bool useModMessage = false)
        {
            /*
            DataGridViewRow row = null;

            canMessage m = useModMessage ? item.msgBeforeMod : item.Params.Message;
            for( int rowIdx = 0; rowIdx < grid.Rows.Count; rowIdx++ )
            {
                row = grid.Rows[rowIdx];

                if( row.Cells[m_colId.Index].Value.ToString() == m.getCanIdString() && 
                    row.Cells[m_colDLC.Index].Value.ToString() == m.getDlcAsString() && 
                    row.Cells[m_colCount.Index].Value.ToString() == item.CountSent.ToString() )
                {
                    return row;
                }
            }

            return null;
            */
            return item.row;
        }

        public void sendMessage (canSendWorker item)
        {
            // send
            if( !CanTool.SendCanMessage(item.Params.Message, 1, item.Params.MessageType == canMessageType.rtr) )
            {
                stopAll();
                return;
            }
            // gui update
            var row = getRow(item);
            row.Cells[m_colCount.Index].Value = item.CountSent + 1;

        }

        public void updateItem (canSendWorker item)
        {
            canMessage2 m = item.Params.Message;
            var row = getRow(item, true);
            row.Cells[m_colId.Index].Value = m.Id.GetIdAsString();
            row.Cells[m_colData.Index].Value = m.GetDataString(" ");
        }



        public UcSendMessage(CanMessageSendTool canSendTool)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            this.Margin = new Padding(20, 20, 20, 20);
            CanTool = canSendTool;
            this.sender = new canSender(this);

            grid.Dock = DockStyle.Fill;

            grid.ContextMenuStrip = contextMenu;

            contextMenu.Items.Add("New message");
            contextMenu.Items.Add("Edit Selected");
            contextMenu.Items.Add("Delete Selected");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Enable All");
            contextMenu.Items.Add("Disable All");
            contextMenu.Items.Add("Delete All");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Copy Selected");

            contextMenu.Opening += ContextMenu_ContextMenuOpening;
            contextMenu.ItemClicked += onContextMenuClicked;


            items = new List<canSendWorker>();

            //gbAddMessage.Margin = new Padding(20);
            // features: refular and RTR message
            // trigger: timer, RTR response, on data

            // create grid: 
            //checkbox (en/dis), type, id, dlc, data, interval, count, sent, remain
            guiCreateGrid();
        }
        //-----------------------------------------------------------------------

        private void ContextMenu_ContextMenuOpening(object sender, CancelEventArgs e)
        {
            bool isRowSelected = grid.SelectedRows.Count > 0;

            contextMenu.Items[1].Enabled = isRowSelected;
            contextMenu.Items[2].Enabled = isRowSelected;
            contextMenu.Items[8].Enabled = isRowSelected;
        }

        private void onContextMenuClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string clicked = e.ClickedItem.Text.ToLower();

            if( "new message" == clicked)
            {
                FrmAddEditMessage f = new FrmAddEditMessage();
                if( DialogResult.OK == f.ShowDialog() )
                {
                    // get params
                    messageSendParams p = f.getParams();    
                    addRow(p);
                    // create an item
                    canSendWorker item = new canSendWorker(this.sender, p);
                    items.Add(item);
                    item.row = grid.Rows[grid.Rows.Count - 1];
                    item.start();
                }
            }

            if( "edit selected" == clicked )
            {

            }

            if( "delete selected" == clicked )
            {
                for( int i = grid.Rows.Count - 1; i >= 0; i-- )
                {
                    var row = grid.Rows[i];
                    if( row.Selected )
                    {
                        var item = getMessageWithRow(row);
                        item.stop();
                        grid.Rows.Remove(row);
                    }
                }
            }

            if( "enable all" == clicked )
            {
                startAll();
            }

            if( "disable all" == clicked )
            {
                stopAll();
            }

            if( "delete all" == clicked )
            {
                foreach (canSendWorker item in items)
                    item.stop();

                items.Clear();
                grid.Rows.Clear();
            }

            if( "copy selected" == clicked )
            {

            }

        }
        //-----------------------------------------------------------------------

        private void guiCreateGrid ()
        {
            // config
            grid.ColumnHeadersHeightSizeMode =
                DataGridViewColumnHeadersHeightSizeMode.DisableResizing;
            grid.Dock = DockStyle.Fill;
            grid.Location = new System.Drawing.Point(0, 0);
            grid.Name = "writter";
            grid.RowTemplate.Height = 21;
            grid.TabIndex = 0;
            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
            grid.MultiSelect = true;
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

            grid.Columns.Clear();

            // en
            DataGridViewCheckBoxColumn cb = new DataGridViewCheckBoxColumn();
            cb.Name = "En";
            cb.DisplayIndex = 0;
            grid.Columns.Add(cb);
            
            // data
            grid.Columns.Add("CAN ID", "CAN ID");       // both
            grid.Columns.Add("DLC", "DLC");             // both
            grid.Columns.Add("Data", "Data");           // either data or RTR
            grid.Columns.Add("Trigger", "Trigger");     // either time or trigger
            DataGridViewCheckBoxColumn cbMods = new DataGridViewCheckBoxColumn();
            cbMods.HeaderText = "Mods";
            grid.Columns.Add(cbMods);
            grid.Columns.Add("Count", "Count");         // how many bytes were sent

            int colIdx = 0;
            m_colEnable = grid.Columns[colIdx++];
            m_colId = grid.Columns[colIdx++];
            m_colDLC = grid.Columns[colIdx++];
            m_colData = grid.Columns[colIdx++];
            m_colTrigger = grid.Columns[colIdx++];
            m_colMods = grid.Columns[colIdx++];
            m_colCount = grid.Columns[colIdx++];

            // width
            m_colEnable.Width = 35;
            m_colId.Width = 95;
            m_colDLC.Width = 40;
            m_colTrigger.Width = 80;
            m_colData.Width = 200;
            m_colMods.Width = 45;
            m_colCount.Width = 65;

            // alignment
            foreach (DataGridViewColumn col in grid.Columns)
                col.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleCenter;

            grid.BackgroundColor = Color.White;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ScrollBars = ScrollBars.Vertical;
            
            // header
            foreach (DataGridViewColumn col in grid.Columns)
                col.HeaderCell.Style.Font = new Font("Calibri", 8.6f, FontStyle.Bold);
            // data
            grid.DefaultCellStyle.Font = new Font("Consolas", 9.0f, FontStyle.Italic);

            grid.CellMouseUp += myDataGrid_OnCellMouseUp;
           // grid.CellMouseDown += myDataGrid_OnCellMouseUp;

        }
        //-----------------------------------------------------------------------


        private void myDataGrid_OnCellMouseUp(object sender, DataGridViewCellMouseEventArgs e)
        {
            grid.ReadOnly = false;

            // End of edition on each click on column of checkbox
            if (e.ColumnIndex == m_colEnable.Index && e.RowIndex >= 0)
            {
                if (e.RowIndex >= 0 && e.RowIndex < grid.RowCount)
                {
                    DataGridViewRow row = grid.Rows[e.RowIndex];

                    row.Cells[m_colEnable.Index].ReadOnly = true;

                    //var val = row.Cells[m_colEnable.Index].Value;
                    bool start = Convert.ToBoolean(row.Cells[m_colEnable.Index].Value);

                    Debug.WriteLine(row.Cells[m_colEnable.Index].Value.ToString());

                    canSendWorker item = getMessageWithRow(row);
                    startStopMessage(item, start);

                    grid.EndEdit();

                    row.Cells[m_colEnable.Index].ReadOnly = false;
                    row.Cells[m_colEnable.Index].Value = start;
                }
            }

            grid.ReadOnly = false;
        }
        //-----------------------------------------------------------------------

        private canSendWorker getMessageWithRow(DataGridViewRow row)
        {
            foreach (canSendWorker i in items)
                if (i.row == row)
                    return i;

            return null;
        }
        //-----------------------------------------------------------------------

        private void stopAll()
        {
            for (int i = items.Count - 1; i >= 0; i-- )
                items[i].stop();
            for (int i = grid.Rows.Count - 1; i >= 0; i--)
                grid.Rows[i].Cells[m_colEnable.Index].Value = false;
        }
        //-----------------------------------------------------------------------

        private void startAll()
        {
            for (int i = items.Count - 1; i >= 0; i--)
                items[i].resume();
            for (int i = grid.Rows.Count - 1; i >= 0; i--)
                grid.Rows[i].Cells[m_colEnable.Index].Value = true;
        }
        //-----------------------------------------------------------------------

        private void startStopMessage(canSendWorker item, bool start)
        {
            if (start)
                item.resume();
            else
                item.stop();
        }
        //-----------------------------------------------------------------------

        private void addRow (messageSendParams p)
        {
            grid.AllowUserToAddRows = true;

            // cb, id, dlc, data, period, count
            object[] values = {
                true,                               // cb
                p.Message.Id.GetIdAsString(),         // id
                p.MessageType == canMessageType.data ? p.Message.Id.GetDlcAsString() : " ",         // dlc
                p.MessageType == canMessageType.data ? 
                    p.Message.GetDataString(" ") : "RTR",      // data
                p.TimerPeriod.ToString() + " ms",   // period
                p.Modifiers.enabled(),              // mods
                0,                                  // count //p.MessageCount
            };

            grid.Rows.Add(values);
            grid.AllowUserToAddRows = false;

            // grid.Rows[grid.Rows.GetLastRow(DataGridViewElementStates.None)].Cells[m_colTrigger.Index].Value = interval.ToString() + " ms";

            m_colData.DefaultCellStyle.Alignment = DataGridViewContentAlignment.MiddleLeft;

            // foreach row expect the 1st one (checbox) switch readonly on
            using (var r = grid.Rows[grid.Rows.GetLastRow(DataGridViewElementStates.None)])
                foreach (DataGridViewColumn col in grid.Columns)
                    r.Cells[col.Index].ReadOnly = m_colEnable != col;

        }


        List<canSendWorker> items;
    }



    public class canSender
    {
        private UcSendMessage parent;
        public canSender(UcSendMessage parent)
        {
            this.parent = parent;
        }

        public void sendMessage(canSendWorker item)
        {
            parent.sendMessage(item);
        }

        public void messageUpdated(canSendWorker item)
        {
            parent.updateItem(item);
        }
    }
}
