using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Globalization;


// UI form
#region form

namespace canAnalyzer
{
    public partial class FrmToolValFinder : Form
    {
        // constructor
        public FrmToolValFinder()
        {
            InitializeComponent();

            guiConfig();
            gridConfig();

            clear();
            MinimumSize = new Size(Width, Height);
        }

        // gui config
        private void guiConfig()
        {
            Text = "Find a Value";

            gbSearchRange.Text = "Range Search";
            gbSearchVal.Text = "Find the Value";

            tbValue.MaxLength   = 18;
            tbValFrom.MaxLength = 18;
            tbValTo.MaxLength   = 18;

            ///tbValue.ShortcutsEnabled = false;
            //tbValFrom.ShortcutsEnabled = false;
            //tbValTo.ShortcutsEnabled = false;

            
            

            lblVal.Text = "Value";
            lblFrom.Text = "Min";
            lblTo.Text = "Max";

            btnSrchVal.Text = "Search";
            btnSrchRange.Text = "Search";

            cbMinBytes.Items.Add("Auto");
            cbMaxBytes.Items.Add("Auto");
            for (int i = 1; i <= 8; i++)
            {
                cbMinBytes.Items.Add(i.ToString());
                cbMaxBytes.Items.Add(i.ToString());
            }
            cbMinBytes.SelectedIndex = 0;
            cbMaxBytes.SelectedIndex = 0;

            lblMinBytes.Text = "Min";
            lblMaxBytes.Text = "Max";
            gbMixMaxBytes.Text = "Num of bytes in the value";

            cbUseFactorDiv.Checked = false;
            cbUseFactorMul.Checked = true;
            cbUseFactorDiv.Enabled = false;
            cbUseFactorMul.Enabled = false;


            gbSettings.Text = "Extra Settings";
            // factors
            lblFactors.Text = "Factors";
            cbUseFactors.Text = "Use Factors";
            tbFactors.Text = "10,16,256";
            // byte order
            cbBigEndian.Checked = true;
            cbBigEndian.Text = "Big-endian (L->R)";
            cbLittleEndian.Checked = true;
            cbLittleEndian.Text = "Little-endian (L<-R)";

            tbFactors.Enabled = cbUseFactors.Checked;

            cbUseFactors.CheckedChanged += onCheckedChanged;
            cbBigEndian.CheckedChanged += onCheckedChanged;
            cbLittleEndian.CheckedChanged += onCheckedChanged;
            cbUseFactorMul.CheckedChanged += onCheckedChanged;
            cbUseFactorDiv.CheckedChanged += onCheckedChanged;

            Font fontTextbox = new Font("Consolas", 8.5f);
            Font fontGroupBox = new Font("Consolas", 8f, FontStyle.Bold);
            Font fontLabel = new Font("Consolas", 8f, FontStyle.Italic);
            Font fontCheckbox = fontLabel;
            Font fontComboBox = fontTextbox;
            Font fontButtons = new Font("Consolas", 8f, FontStyle.Bold);

            tbFactors.Font = fontTextbox;
            tbValFrom.Font = fontTextbox;
            tbValTo.Font = fontTextbox;
            tbValue.Font = fontTextbox;

            gbSearchVal.Font = fontGroupBox;
            gbSettings.Font = fontGroupBox;
            gbSearchRange.Font = fontGroupBox;
            gbMixMaxBytes.Font = fontGroupBox;

            lblTo.Font = fontLabel;
            lblMinBytes.Font = fontLabel;
            lblMaxBytes.Font = fontLabel;
            lblFrom.Font = fontLabel;
            lblFactors.Font = fontLabel;
            lblVal.Font = fontLabel;

            cbBigEndian.Font = fontCheckbox;
            cbLittleEndian.Font = fontCheckbox;
            cbUseFactorDiv.Font = fontCheckbox;
            cbUseFactorMul.Font = fontCheckbox;
            cbUseFactors.Font = fontCheckbox;

            cbMaxBytes.Font = fontComboBox;
            cbMinBytes.Font = fontComboBox;

            btnSrchRange.Font = fontButtons;
            btnSrchVal.Font = fontButtons;

            // non-editable
            cbMinBytes.DropDownStyle = ComboBoxStyle.DropDownList;
            cbMaxBytes.DropDownStyle = ComboBoxStyle.DropDownList;
        }

        private void onCheckedChanged(object sender, EventArgs e)
        {
            cbUseFactorMul.Enabled = cbUseFactors.Checked;
            cbUseFactorDiv.Enabled = cbUseFactors.Checked;

            bool enable = cbBigEndian.Checked || cbLittleEndian.Checked;
            bool allowDoSearch = cbUseFactors.Checked ? 
                cbUseFactorMul.Checked || cbUseFactorDiv.Checked : 
                true;

            btnSrchRange.Enabled = allowDoSearch && enable;
            btnSrchVal.Enabled = allowDoSearch && enable;
            tbFactors.Enabled = cbUseFactors.Checked && allowDoSearch;
        }

        private void gridConfig()
        {
            // add columns
            grid.Columns.Add("ID",  "ID");          
            grid.Columns.Add("DLC", "DLC");
            m_colId = grid.Columns[0];
            m_colDlc = grid.Columns[1];

            m_colData = new DataGridViewColumn[8];

            for (int i = 0; i < 8; i++)
            {
                string s = string.Format("{0}", i);
                grid.Columns.Add(s, s);
                m_colData[i] = grid.Columns[grid.Columns.Count - 1];
                m_colData[i].Width = 35;
            }

            grid.Columns.Add("Value", "Value");
            m_colValue = grid.Columns[grid.Columns.Count - 1];

            m_colId.Width = 80;
            m_colDlc.Width = 50;
            m_colValue.Width = 140;

            grid.BorderStyle = BorderStyle.None;
            grid.RowHeadersVisible = false;
            grid.RowHeadersBorderStyle = DataGridViewHeaderBorderStyle.None;
            grid.ColumnHeadersBorderStyle = DataGridViewHeaderBorderStyle.Single;
            grid.SelectionMode = DataGridViewSelectionMode.CellSelect;


            
           //int w = 50;
           // foreach (DataGridViewColumn col in grid.Columns)
           //    w += col.Width;
           //Width = w;

        }

        private void addResult (canMessage2 msg, List<findToolRes> list)
        {
            foreach(var res in list)
            {
                // add a row
                grid.AllowUserToAddRows = true;
                grid.Rows.Add();
                grid.AllowUserToAddRows = false;

                // fill in
                DataGridViewRow row = grid.Rows[grid.RowCount - 1];

                row.ReadOnly = true;

                row.Cells[m_colId.Index].Value = msg.Id.GetIdAsString();
                row.Cells[m_colDlc.Index].Value = msg.Id.GetDlcAsString();

                int dataCounter = 0;
                foreach (byte b in msg.Data)
                    row.Cells[m_colData[dataCounter++].Index].Value = b.ToString("X2");

                row.Cells[m_colValue.Index].Value =
                    string.Format("{0} / 0x{1}", res.Value, res.Value.ToString("X"));

                // backlight
                int idx = m_colData[res.StartPos].Index;
                for (int i = 0; i < res.Lenght; i++)
                {
                    row.Cells[idx].Style.BackColor = Color.LightGreen;
                    row.Cells[idx].Style.SelectionForeColor = Color.LightGreen;
                    idx++;// = res.Dir == findToolRes.Direction.LeftToRight ? idx + 1 : idx - 1;
                }
            }
        }

        private void btnSrchVal_Click(object sender, EventArgs e)
        {
            clear();

            string sVal = tbValue.Text;
            int num;
            if (parseTextValue(sVal, out num))
                search(num, num, cbUseFactors.Checked, tbFactors.Text, cbUseFactorMul.Checked, cbUseFactorDiv.Checked);
        }

        private void btnSrchRange_Click(object sender, EventArgs e)
        {
            clear();

            string sVal1 = tbValFrom.Text;
            string sVal2 = tbValTo.Text;
            int num1, num2;

            if (parseTextValue(sVal1, out num1) && parseTextValue(sVal2, out num2) )
                search(num1, num2, cbUseFactors.Checked, tbFactors.Text, cbUseFactorMul.Checked, cbUseFactorDiv.Checked);
        }

        private bool parseTextValue (string sVal, out int res)
        {
            // as int
            bool parsed = int.TryParse(sVal, out res);

            // as hex
            if( !parsed && sVal.Length > 2 )
            {
                char[] _trim_hex = new char[] { '0', 'x' };
                parsed = int.TryParse(sVal.TrimStart(_trim_hex), NumberStyles.HexNumber, null, out res);
            }

            return parsed;
        }

        private int[] parseFactorString (string str)
        {
            str = str.Trim();
            str = str.Replace(" ", string.Empty);

            string[] sList = str.Split(',');

            if( sList.Length > 0)
            {
                int[] res = new int[sList.Length];
                for (int i = 0; i < sList.Length; i++)
                    res[i] = int.Parse(sList[i]);

                return res;
            }

            return null;
        }

        private findToolRes.Direction getSearchDirection()
        {
            if (cbBigEndian.Checked && cbLittleEndian.Checked)
                return findToolRes.Direction.All;
            else if (cbBigEndian.Checked)
                return findToolRes.Direction.LeftToRight;
            else if (cbLittleEndian.Checked)
                return findToolRes.Direction.RightToLeft;
            else
                return findToolRes.Direction.None;
        }

        private void search (int value1, int value2, bool useFactors, string sFactors, bool mulByFactor, bool divByFactor)
        {
            int bMin = cbMinBytes.Text == "Auto" ? -1 : int.Parse(cbMinBytes.Text);
            int bMax = cbMaxBytes.Text == "Auto" ? -1 : int.Parse(cbMaxBytes.Text);

            int[] factors = useFactors ? parseFactorString(sFactors) : null;

            findToolRes.Direction dir = getSearchDirection();

            foreach ( var msg in DataList)
            {
                // do we need to check it?
                if (!Filter.Contains(msg.Id))
                {
                    // basic search with no factors
                    List<findToolRes> res = new List<findToolRes>();
                    if (findTool.findValueInBuff(msg.Data, value1, value2, ref res, dir, bMin, bMax))
                        addResult(msg, res);

                    // factors
                    if (useFactors && factors != null && (mulByFactor || divByFactor))
                    {
                        foreach( int f in factors )
                        {
                            int tmp1 = value1 * f;
                            int tmp2 = value1 * f;
                            if (tmp1 != 0 && tmp2 != 0 && mulByFactor)
                            {
                                // mul
                                List<findToolRes> res1 = new List<findToolRes>();
                                if (findTool.findValueInBuff(msg.Data, value1 * f, value2 * f, ref res1, dir, bMin, bMax))
                                    addResult(msg, res1);
                            }

                            tmp1 = value1 / f;
                            tmp2 = value1 / f;
                            if (tmp1 != 0 && tmp2 != 0 && divByFactor)
                            {
                                // div
                                List<findToolRes> res2 = new List<findToolRes>();
                                if (findTool.findValueInBuff(msg.Data, value1 / f, value2 / f, ref res2, dir, bMin, bMax))
                                    addResult(msg, res2);
                            }
                        }
                    }
                }                
            }
        }

        private void clear ()
        {
            grid.Rows.Clear();
        }

        public List<canMessage2> DataList { get; set; }
        public canFilter Filter { get; set; }

        DataGridViewColumn m_colId, m_colDlc, m_colValue;

        private void FrmToolValFinder_Load(object sender, EventArgs e)
        {

        }

        private void groupBox1_Enter(object sender, EventArgs e)
        {

        }

        DataGridViewColumn[] m_colData;
    }
}

#endregion

// tools
#region findTool

namespace canAnalyzer
{
    // result class
    public class findToolRes
    {
        // search direction
        public enum Direction
        {
            None,
            LeftToRight,
            RightToLeft,
            All
        }

        // fields
        public int Value { get; set; }
        public int StartPos { get; set; }
        public int Lenght { get; set; }
        public Direction Dir { get; set; }

        // constructor1
        public findToolRes()
        {
            set(0, -1, -1, Direction.None);
        }

        // constructor2
        public findToolRes(int val, int pos, int len, Direction dir)
        {
            set(val, pos, len, dir);
        }

        // set
        public void set(int val, int pos, int len, Direction dir)
        {
            Value = val;
            StartPos = pos;
            Lenght = len;
            Dir = dir;
        }

        // is found
        public bool isFound()
        {
            return StartPos >= 0 && Lenght >= 0;
        }

        // empty
        static public findToolRes Empty()
        {
            findToolRes res = new findToolRes();
            return res;
        }
    }

    // find tool class
    static public class findTool
    {
        // do find
        static private bool find(byte[] data, int valFrom, int valTo, int bytes,
            findToolRes.Direction dir, ref List<findToolRes> resList)
        {
            // borders
            if (0 == bytes || bytes > data.Length)
                return false;

            int tmp = 0;
            bool found = false;

            bool dirBE = dir == findToolRes.Direction.LeftToRight || dir == findToolRes.Direction.All;
            bool dirLE = dir == findToolRes.Direction.RightToLeft || dir == findToolRes.Direction.All;

            // left -> right (big-endian)
            if (dirBE)
            {
                for (int attempt = 0; attempt < data.Length - bytes + 1; attempt++)
                {
                    // get
                    tmp = 0;
                    for (int i = 0; i < bytes; i++)
                        tmp = (tmp << 8) | data[attempt + i];

                    // check
                    if (tmp >= valFrom && tmp <= valTo)
                    {
                        resList.Add(
                            new findToolRes(tmp, attempt, bytes,
                                findToolRes.Direction.LeftToRight)
                        );
                        found = true;
                    }
                }
            }

            // right -> left (little-endian)
            if (bytes > 1 || !dirBE)
            {
                if (dirLE) { 
                    for (int attempt = 0; attempt < data.Length - bytes + 1; attempt++)
                    {
                        // get
                        tmp = 0;
                        for (int i = bytes - 1; i >= 0; i--)
                            tmp = (tmp << 8) | data[attempt + i];

                        // check
                        if (tmp >= valFrom && tmp <= valTo)
                        {
                            resList.Add(
                                new findToolRes(tmp, attempt, bytes,
                                    findToolRes.Direction.RightToLeft)
                            );
                            found = true;
                        }
                    }
                }
            }

            return found;
        }

        static public bool findValueInBuff(byte[] data, int valFrom, int valTo, 
            ref List<findToolRes> resList, findToolRes.Direction dir = findToolRes.Direction.All,
            int minBytes = -1, int maxBytes = -1)
        {
            if (data.Length == 0)
                return false;

            bool found = false;

            // auto
            if (-1 == maxBytes)
            {       
                // calc num of bytes
                int max = valFrom >= valTo ? valFrom : valTo;
                string s = max.ToString("X");
                int len = s.Length;
                maxBytes = 0 == len % 2 ? len / 2 : len/2 + 1;
            }

            // borders
            if (maxBytes < 1)
                maxBytes = 1;
            if (minBytes < 1)
                minBytes = 1;
            if (minBytes > maxBytes)
                maxBytes = minBytes;
            
            // swap
            if (valFrom > valTo)
            {
                int tmp = valFrom;
                valFrom = valTo;
                valTo = tmp;
            }

            for (int bytes = minBytes; bytes <= maxBytes; bytes++)
                found |= find(data, valFrom, valTo, bytes, dir, ref resList);

            return found;
        }

        static public bool findValueInBuff(byte[] data, int val, ref List<findToolRes> resList, 
            findToolRes.Direction dir = findToolRes.Direction.All, int maxBytes = -1)
        {
            return findValueInBuff(data, val, val, ref resList, dir, maxBytes);
        }

    }
}

#endregion
