using System;
using System.Windows.Forms;

namespace canAnalyzer
{

    public class canMessageTrigger
    {
        public int Id {set; get;}
        public int Dlc { set; get; }
        public bool Is29bitId { set; get; }

    }

    // features: interval, increment id, increment any byte, count before stop,
    public class canSendWorker
    {
        private System.Windows.Forms.Timer timer;

        public int  CountSent           { get { return totalSent; } }   
        public bool IsActive            { get { return timer.Enabled;  } }

        private int totalSent = 0;                  // total sent
        private int remainSendToCheckStop = 0;     // count before stop
        //private bool isRunning = false;

        private canSender sender;

        public messageSendParams Params { set; get; }
        public canMessage2 msgBeforeMod { set; get; }

        public DataGridViewRow row { set; get; }

        // constructor
        #region Constructors
        public canSendWorker(canSender sender, messageSendParams p)
        {
            this.sender = sender;

            Params = p;

            // create the timer
            timer = new System.Windows.Forms.Timer();
            timer.Tick += new EventHandler(callback);
            timer.Enabled = false;

            totalSent = 0;
            remainSendToCheckStop = 0;
        }
        #endregion


        public void sendMessage (int count = 1)
        {
            if (Params != null && Params.Message != null)
            {
                while (count-- > 0)
                {
                    sender.sendMessage(this);
                    totalSent++;
                    remainSendToCheckStop--;
                }
            }
        }

        public void resume()
        {
            // there are 3 cases:
            // 1. limited (or not) sending with no timer
            // 2. timer
            // 3. wait until the data received (another func)

            // timer
            if (Params.TriggerStart == canTriggerStart.timer)
            {
                if (Params.MessageCount > 0)
                {
                    // send the 1st message imideately
                    sendMessage();

                    if (0 != remainSendToCheckStop)
                    {
                        timer.Interval = Params.TimerPeriod;
                        timer.Enabled = true;
                    }
                }
            }
        }

        public void start ()
        {
            // update counter
            updateRemainToStopCount();
            // resume
            resume();
        }

        public void stop ()
        {
            timer.Enabled = false;
        }

        private void updateRemainToStopCount()
        {
            remainSendToCheckStop = Params.TriggerStop == canTriggerStop.counter ?
                Params.MessageCount : int.MaxValue;
        }

        private void checkStopModifyConditions()
        {
            // stop
            if( Params.PostCondition == paramActAfterStop.stop | !Params.Modifiers.enabled() )
            {
                stop();
            }
            else
            {
                // modify 
                modifyMessage();
                start();
            }
        }

        private void modifyMessage()
        {
            canMessage2 m = Params.Message;

            // id
            int newId = m.Id.Id;
            if (Params.Modifiers.modId)
                newId = newId < m.Id.GetMaxId() ? newId + 1 : 0;

            // data
            byte[] data = m.Data;
            for (int i = 0; i < data.Length; i++)
                if( Params.Modifiers.modB[i])
                    data[i]++;

            
            msgBeforeMod = m;
            Params.Message = new canMessage2(newId, m.Id.Is29bit, data);
            sender.messageUpdated(this);
        }

        private void callback(object source, EventArgs e)
        {
            // send
            if (remainSendToCheckStop > 0)
                sendMessage();
            else
            {
                checkStopModifyConditions();
            }
        }
    }


}
