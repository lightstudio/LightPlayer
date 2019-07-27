using System;

namespace Light.Lyrics.Analyzer
{
    public class LrcItem
    {
        public long time;
        public string content;
        public LrcItem(long time, string content)
        {
            this.time = time;
            this.content = content;
        }

        public override string ToString()
        {
            return time.ToString() + " ms, content: " + content;
        }
    }
}
