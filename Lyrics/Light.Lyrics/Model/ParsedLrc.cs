using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Lyrics.Model
{
    public class ParsedLrc
    {
        public string Album { get; internal set; }

        public string Artist { get; internal set; }

        public string Title { get; internal set; }

        public List<LrcSentence> Sentences { get; } = new List<LrcSentence>(32);

        public int GetPositionFromTime(long ms)
        {
            if (Sentences.Count == 0 || ms < Sentences[0].Time)
                return 0;

            for (int i = 0; i < Sentences.Count; i++)
            {
                if (ms < Sentences[i].Time)
                    return i - 1;
            }

            return Sentences.Count - 1;
        }
    }
}
