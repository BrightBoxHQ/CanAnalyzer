using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;
using System.Threading;

// stages
// waiting until can is sleeping
// try all the messages 
namespace canAnalyzer
{
    public partial class ucActivationCodeSearcher : UserControl
    {
        private scanStage stage = scanStage.pause; 
        private decimal idStart, idStop, idCurrent;
        private DateTime dtLastCanMsg;
        private Thread worker;
        private int messageCountToSend;
        private int butSleepTmo;
        private int idCheckTmo;
        private CanMessageSendTool CanTool;
        private canMessage2 message;


        #region Constructor
        public ucActivationCodeSearcher(CanMessageSendTool canSendTool)
        {
            InitializeComponent();

            CanTool = canSendTool;

            // this
            this.Dock = DockStyle.Fill;
            this.Margin = new Padding(0);

            // trace
            tbTrace.Dock = DockStyle.Fill;
            tbTrace.Multiline = true;
            tbTrace.ReadOnly = true;
            tbTrace.Margin = new Padding(0);
            tbTrace.ScrollBars = ScrollBars.Vertical;

            // can id
            numIdFrom.Maximum = canMessage.idMax(rb29BitId.Checked);
            numIdTo.Maximum = canMessage.idMax(rb29BitId.Checked);

            numIdFrom.Hexadecimal = true;
            numIdTo.Hexadecimal = true;
            numIdFrom.Minimum = 0;
            numIdTo.Minimum = 0;

            numIdFrom.Value = canMessage.idMax(rb29BitId.Checked);
            numIdTo.Value = 0;


            tbBusSleep.KeyPress += Tools.textBoxHexOnlyEvent;
            tbCount.KeyPress += Tools.textBoxHexOnlyEvent;
            tbIdCheck.KeyPress += Tools.textBoxHexOnlyEvent;

            numIdFrom.Font = new Font("Consolas", 9.0f, FontStyle.Italic);
            numIdFrom.TextAlign = HorizontalAlignment.Right;

            numIdTo.Font = numIdFrom.Font;
            numIdTo.TextAlign = numIdFrom.TextAlign;

            btnStartStop.Text = "Start";

            tbCount.Text = "20";
            tbBusSleep.Text = "3000";
            tbData.Text = "00";
            tbIdCheck.Text = "500";

            tbTrace.Font = new Font("Consolas", 8.3f, FontStyle.Italic);

            // events
            rb11BitId.CheckedChanged += rbId_CheckedChanged;
            rb29BitId.CheckedChanged += rbId_CheckedChanged;

        }
        #endregion 


        private void guiStartStop ()
        {
            // invoke
            if (InvokeRequired)
            {
                this.Invoke(new Action(guiStartStop));
                return;
            }

            gbSettingsExtra.Enabled = stage == scanStage.pause;
            gbSettingsMain.Enabled = stage == scanStage.pause;
            btnStartStop.Text = stage == scanStage.pause ? "Start" : "Stop";
        }

        private void btnStartStop_Click(object sender, EventArgs e)
        {
            if (!CanTool.IsCommunication)
                return;

            if( stage == scanStage.pause )
            { 
                stage = scanStage.waitingForCanSleep;

                // id range
                idStart = numIdFrom.Value;
                idStop = numIdTo.Value;
                idCurrent = idStart;
                byte[] d = Tools.hexStringToByteArray(tbData.Text);

                if (d == null || d.Length == 0 || d.Length > canMessage.maxDataBytesNum())
                    return;

                // int only
                messageCountToSend = Convert.ToInt32(tbCount.Text, 10);
                butSleepTmo = Convert.ToInt32(tbBusSleep.Text, 10);
                idCheckTmo = Convert.ToInt32(tbIdCheck.Text, 10);

                message = new canMessage2((int)idCurrent, rb29BitId.Checked, d, 0);

                // trace
                if (!string.IsNullOrEmpty(tbTrace.Text))
                    trace(Environment.NewLine);
                    
                trace(Environment.NewLine + string.Format("Scan from {0} to {1}",
                    canMessage.getCanIdString((int)idStart, rb29BitId.Checked),
                    canMessage.getCanIdString((int)idStop, rb29BitId.Checked)));
                trace(string.Format("DLC = {0}, Data = {1}", 
                    message.Id.GetDlcAsString(),
                    message.GetDataString(" ")));
                trace("Waiting until the CAN bus is sleeping");

                worker = new Thread(onWorker);
                worker.Name = "Wrk";
                worker.Start();
            }
            else
            {
                // stop
                stage = scanStage.pause;
                trace("Aborted");
            }

            guiStartStop();
        }


        #region Trace
        private void trace (string str)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<string>(trace), new object[] {str});
                return;
            }

            this.tbTrace.AppendText(str + Environment.NewLine);

        }

        private void trace (decimal idx)
        {
            if (InvokeRequired)
            {
                this.Invoke(new Action<decimal>(trace), new object[] { idx });
                return;
            }

            string str = canMessage.getCanIdString((int)idx, rb29BitId.Checked);
            str = string.Format("{0} is sent ({1} messages)", str, messageCountToSend);
            this.tbTrace.AppendText(str + Environment.NewLine);
        }
        #endregion

        #region worker
        private void onWorker ()
        {
            // we're using this time for can sleep detection
            DateTime dtScanStarted = DateTime.Now;
            // and this one for trace only
            DateTime dtTrace = DateTime.Now;

            while( stage != scanStage.pause )
            {
                if( stage == scanStage.waitingForCanSleep )
                {
                    var diffInSeconds = (DateTime.Now - dtLastCanMsg).TotalMilliseconds;
                    if( diffInSeconds > butSleepTmo)
                    {
                        stage = scanStage.scanning;
                        dtScanStarted = DateTime.Now;
                        continue;
                    }

                    Thread.Sleep(500);
                }
                else if( stage == scanStage.scanning)
                {
                    // send
                    CanTool.SendCanMessage(message, messageCountToSend);
                    trace(idCurrent);

                    // wait
                    Thread.Sleep(messageCountToSend + messageCountToSend/2);

                    // check date time
                    if (dtScanStarted < dtLastCanMsg)
                    {
                        trace("A new CAN message has received");
                        trace("Waiting untill the CAN bus is sleeping again");
                        // a message has received
                        stage = scanStage.waitingForCanSleepAfterScanning;
                        continue;
                    }

                    // update
                    if ( idCurrent == idStop )
                    {
                        stage = scanStage.pause;
                        trace("Failed. The low border has reached.");
                        continue;
                    }
                    else
                    {
                        idCurrent = idStart > idStop ? idCurrent - 1 : idCurrent + 1;
                        //message.Id.Id = (int)idCurrent;
                        message = new canMessage2((int)idCurrent, message.Id.Is29bit, message.Data);
                    }
                }
                else if (stage == scanStage.waitingForCanSleepAfterScanning)
                {
                    var diffInSeconds = (DateTime.Now - dtLastCanMsg).TotalMilliseconds;
                    if (diffInSeconds > butSleepTmo)
                    {
                        stage = scanStage.checking;
                        dtScanStarted = DateTime.Now;
                        trace("Can Bus is sleeping again");
                        continue;
                    }

                    Thread.Sleep(500);
                }
                else if( stage == scanStage.checking)
                {
                    // send data in the reverse direction
                    CanTool.SendCanMessage(message, messageCountToSend);
                    trace(idCurrent);


                    // wait for idCheckTmo msec
                    trace("Waiting for " + idCheckTmo.ToString() + " msec");
                    Thread.Sleep(idCheckTmo);

                    // check date time
                    if (dtScanStarted < dtLastCanMsg)
                    {
                        // a message has received
                        trace("Done");
                        trace(string.Format("Activation ID is: {0}", message.Id.GetIdAsString()) );
                        stage = scanStage.pause;
                        continue;
                    }

                    idCurrent = idStart > idStop ? idCurrent + 1 : idCurrent - 1;
                    //message.Id = (int)idCurrent;
                    message = new canMessage2((int)idCurrent, message.Id.Is29bit, message.Data);
                }
            }

            double elapsed = (DateTime.Now - dtTrace).TotalMilliseconds / 1000.0d; ;
            string sElapsed = elapsed.ToString("0.000");

            trace(string.Format("Elapsed Time: {0} sec", sElapsed));

            // end of while
            guiStartStop();
        }
        #endregion

        private void rbId_CheckedChanged(object sender, EventArgs e)
        {
            numIdFrom.Maximum = canMessage.idMax(rb29BitId.Checked);
            numIdTo.Maximum = canMessage.idMax(rb29BitId.Checked);
        }

        public void pushMessageList (List<canMessage2> ls)
        {
            if (ls.Count > 0)
                dtLastCanMsg = DateTime.Now;
        }
    }

    #region Helpers
    internal enum scanStage
    {
        pause,
        waitingForCanSleep,
        scanning,
        waitingForCanSleepAfterScanning,
        checking,
    }
    #endregion
}
