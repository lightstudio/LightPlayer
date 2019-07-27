using System;
using System.Collections.Generic;

namespace Light.Lyrics.Analyzer
{
    public class LrcInfo
    {
        public String title;
        public String singer;
        public String album;
        public List<LrcItem> items = new List<LrcItem>(32);

        public int GetPositionFromTime(long ms)
        {
            if (items.Count == 0) return 0;
            if (ms < items[0].time) return 0;
            for (int i = 0; i < items.Count; i++)
            {
                if (ms < items[i].time)
                    return i - 1;
            }
            return items.Count - 1;
        }
    }  
}
