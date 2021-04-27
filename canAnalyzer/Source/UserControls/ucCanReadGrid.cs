using System.Collections.Generic;
using System.Windows.Forms;


// CAN data grid user control
namespace canAnalyzer
{
    public partial class UcCanReadGrid : UserControl
    {
        // grid
        private canDataGrid grid;
        // filter
        public canFilter CanFilter { set { grid.CanFilter = value; } }
        // can send tool
        private CanMessageSendTool CanTool;

        // constructor
        public UcCanReadGrid(CanMessageSendTool can)
        {
            InitializeComponent();
            this.Dock = DockStyle.Fill;
            grid = new canDataGrid(this);

            CanTool = can;

            // menu
            contextMenu.Items.Add("Hide Backlighted");
            contextMenu.Items.Add("Hide all but Backlighted");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Hide Selected");
            contextMenu.Items.Add("Hide all but Selected");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Hide all");
            contextMenu.Items.Add("UnHide all");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Toggle Checkbox for All");
            contextMenu.Items.Add("Toggle Checkbox for Selected");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Copy Selected");
            contextMenu.Items.Add("Copy All");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Save Selected");
            contextMenu.Items.Add("Save All");
            contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Copy Selected as Script");
            contextMenu.Items.Add("Copy All as Script");
            contextMenu.Items.Add(new ToolStripSeparator());
            //contextMenu.Items.Add("Copy Selected for Remoto");
            //contextMenu.Items.Add(new ToolStripSeparator());
            contextMenu.Items.Add("Send Selected");

            // set it
            grid.contextMenuStrip = contextMenu;
        }

        // clear
        public void clear()
        {
            grid.clear();
        }

        // push a message list
        public void pushMessageList(List<canMessage2> ls)
        {
            grid.push(ls);
        }

        // push a message
        public void pushMessage (canMessage2 msg)
        {
            List<canMessage2> ls = new List<canMessage2>();
            ls.Add(msg);
            pushMessageList(ls);
        }

        // update checkboxes with a current filter state
        public void updateCheckboxesWithFilter ()
        {
            grid.updateCheckboxesWithFilter();
        }

        // get a list of selected messages
        public UniqueDataSetCan getSelected()
        {
            return grid.getSelectedMessages2();
        }

        // get a min possible width 
        public int getNecessaryWidth()
        {
            return grid.getWidthMin();
        }

        // get message list as a headered string
        private string getMessageString (bool selected = false)
        {
            List<canMessage2> ls = grid.getMessageList(selected);
            List<int> intervals = grid.getMessageIntervalList(selected);
            List<int> count = grid.getMessageCountList(selected);
            string txt = Tools.getMessageHeaderString(false, true, true) +
                Tools.ConvertMessageToStringFull(ls, intervals, count, false);
            return txt;
        }

        // get an interval value for a message
        public int getCanMessageInterval (canMessage2 msg)
        {
            return grid.getMessageInterval(msg);
        }

        // get an interval value for a message
        public int getCanMessageCounter(canMessage2 msg)
        {
            return grid.getMessageCounter(msg);
        }

        // context menu callback
        private void onContextMenuClicked(object sender, ToolStripItemClickedEventArgs e)
        {
            string selected = e.ClickedItem.Text.ToLower();

            // hide
            contextMenu.Hide();

            if (selected == "hide backlighted")
            {
                grid.hideBacklighedMessages();
            }
            if( selected == "hide all but backlighted")
            {
                grid.hideAllButBacklighted();
            }
            if (selected == "hide selected")
            {
                grid.hideSelected();
            }
            if( selected == "hide all but selected")
            {
                grid.hideAllMessagesButSelected();
            }
            if( selected == "unhide all")
            {
                grid.unhideAll();
            }
            if (selected == "hide all")
            {
                grid.hideAll();
            }
            if (selected == "toggle checkbox for all")
            {
                grid.toggleCheckProperty();
            }
            if (selected == "toggle checkbox for selected")
            {
                grid.toggleCheckPropertySelected();
            }
            if (selected == "copy all" || selected == "copy selected")
            {
                bool sel_only = selected.Contains("selected");
                string txt = getMessageString(sel_only);
                Clipboard.SetText(txt);
            }
            if (selected == "save all" || selected == "save selected")
            {
                // save
                SaveFileDialog dlg = new SaveFileDialog();
                dlg.DefaultExt = "txt";
                dlg.AddExtension = true;
                dlg.CheckPathExists = true;
                dlg.Filter = "CAN Analyzer message list|*txt";
                dlg.OverwritePrompt = true;
                if (dlg.ShowDialog() == DialogResult.OK)
                {
                    bool sel_only = selected.Contains("selected");
                    string txt = getMessageString(sel_only);
                    System.IO.File.WriteAllText(dlg.FileName, txt);
                }
            }
            if (selected == "copy all as script")
            {
                List<canMessage2> ls = grid.getMessageList(false);
                string txt = nsScriptParser.scriptParser.message2string(ls);
                Clipboard.SetText(txt);
            }
            if (selected == "copy selected as script")
            {
                List<canMessage2> ls = grid.getMessageList(true);
                string txt = nsScriptParser.scriptParser.message2string(ls);
                Clipboard.SetText(txt);
            }
            if (selected == "copy selected for remoto")
            {
                List<canMessage2> ls = grid.getMessageList(true);
                string txt = Tools.ConvertMessageToRemotoCmd(ls);
                Clipboard.SetText(txt);
            }
            if (selected == "send selected")
            {
                List<canMessage2> ls = grid.getMessageList(true);
                CanTool.SendCanMessage(ls);
            }
        }
    }
}
