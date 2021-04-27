using System.Collections.Generic;

namespace canAnalyzer
{
    public class UniqueDataSetCan
    {
        private HashSet<long> set;

        // constructor
        public UniqueDataSetCan()
        {
            set = new HashSet<long>();
        }

        // get an unique hash code based key
        private long GetAKey(canMessageId item)
        {
            return item.GetHashCodeUnique();
        }

        // add a new item if does not exist yet
        // returs true if we've added the item
        public bool Add(canMessageId item)
        {
            bool res = false;
            long key = GetAKey(item);
            if (!set.Contains(key))
            {
                set.Add(key);
                res = true;
            }
            return res;
        }

        // is there an item?
        public bool Contains(canMessageId item)
        {
            long key = GetAKey(item);
            return set.Contains(key);
        }

        // remove an item
        public void Remove(canMessageId item)
        {
            if (Contains(item))
                set.Remove(GetAKey(item));
        }

        // clear
        public void Clear()
        {
            set.Clear();
        }
    }

    // software message filter
    public class canFilter
    {
        // hash storage
        private UniqueDataSetCan list;

        // constructor
        public canFilter()
        {
            list = new UniqueDataSetCan();
        }

        // cleaner
        public void clear()
        {
            list.Clear();
        }

        // add1
        public void add(int canId, int dlc, bool is29bitId)
        {
            canMessageId msg = new canMessageId(canId, dlc, is29bitId);
            add(msg);
        }
        // add2
        public void add(canMessageId canId)
        {
            list.Add(canId);
        }

        // remove1
        public void remove(int canId, int dlc, bool is29bitId)
        {
            canMessageId msg = new canMessageId(canId, dlc, is29bitId);
            remove(msg);
        }
        // remove2
        public void remove(canMessageId canId)
        {
            list.Remove(canId);
        }

        // check1
        public bool Contains(int canId, int dlc, bool is29bitId)
        {
            canMessageId msg = new canMessageId(canId, dlc, is29bitId);
            return Contains(msg);
        }
        // check2
        public bool Contains(canMessageId canId)
        {
            return list.Contains(canId);
        }

    }
    // end of the class
    //-----------------------------------------------------------------------
}


namespace canAnalyzer
{

    public class canMessageMaskFilter
    {
        // message
        private class maskedMessage
        {
            private canMessageId Id;
            private byte[] Mask;

            public maskedMessage(canMessage2 msg)
            {
                Id = msg.Id;
                Mask = new byte[Id.Dlc];
            }

            // returns true if the message has at least one new data bit
            // othrewise returns false
            public bool update(canMessage2 msg)
            {
                bool res = false;
                for (int i = 0; i < Mask.Length; i++)
                {
                    int tmp = msg.Data[i] | Mask[i];
                    if (tmp != Mask[i])
                    {
                        res = true;
                        Mask[i] = (byte)tmp;
                    }
                }
                return res;
            }

            // 0x77 ,~0x88
            // 0x01 - not new
            // 0x08 - new

            // returns false if it should be filtered
            // returns true if it has at least one new bit
            public bool checkFreshness(canMessage2 msg)
            {
                for (int i = 0; i < Mask.Length; i++)
                {
                    int t1 = 0xFF & msg.Data[i];
                    int t2 = 0xFF & (~Mask[i]);
                    int tmp2 = t1 & t2;
                    int tmp = msg.Data[i] & (~Mask[i]);
                    if (tmp != tmp2)
                        break;
                    if (tmp != 0 )
                    {
                        return true;
                    }
                }
                return false;
            }

            public void reset()
            {
                for (int i = 0; i < Mask.Length; i++)
                    Mask[i] = 0;
            }
        }

        // dictionary
        private Dictionary<long, maskedMessage> list;
        // is enabled
        public bool Enabled {set; get;}

        // constructor
        public canMessageMaskFilter()
        {
            list = new Dictionary<long, maskedMessage>();
            Enabled = false;
        }

        public bool isFilterExists ()
        {
            return list.Count > 0;
        }

        // returns true if the message can be printed
        // returns false if the message should be filtered
        public bool update (canMessage2 msg)
        {
            long key = msg.GetHashCodeUnique();

            // exits?
            if( list.ContainsKey(key) )
            {
                return Enabled ? list[key].update(msg) : list[key].checkFreshness(msg);
            }
            else
            {
                // create it and return 'true' cuz the message is a new one
                if (Enabled)
                {
                    list.Add(key, new maskedMessage(msg));
                    list[key].update(msg);
                }
                return true;
            }
        }

        public void reset()
        {
            list.Clear();
        }
    }
}
