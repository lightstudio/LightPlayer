using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Light.Lyrics.Analyzer
{
    public class LrcAnalyzer : IComparer<LrcItem>
    {
        public LrcInfo Info { get; } = new LrcInfo();

        public LrcAnalyzer(string lrcText) : this(new StringReader(lrcText), false) { }
        /// <summary>
        /// Create LrcAnalyzer object with a stream.
        /// </summary>
        /// <param name="lrcTextStream">stream that contains the lrc text.</param>
        /// <param name="close">Close the stream after analyzing</param>
        public LrcAnalyzer(Stream lrcTextStream, bool close = false) :
            this(new StreamReader(lrcTextStream, Encoding.UTF8), !close)
        {
            if (close) lrcTextStream.Dispose();
        }

        internal LrcAnalyzer(TextReader reader, bool leaveOpen = true)
        {
            string line;

            // TextReader will handle the newline well.
            while ((line = reader.ReadLine()?.Trim()) != null)
            {
                ParseLine(line);
            }

            // List<T>.Sort still creates wrapper around Comparison<T>.
            // A straightforward method is using IComparer<T>.
            Info.items.Sort(this);

            if (!leaveOpen) reader.Dispose();
        }

        public int GetPositionFromTime(long ms)
        {
            if (Info.items.Count == 0) return 0;
            if (ms < Info.items[0].time) return 0;
            for (int i = 0; i < Info.items.Count; i++)
            {
                if (ms < Info.items[i].time)
                    return i - 1;
            }
            return Info.items.Count - 1;
        }

        public int Compare(LrcItem x, LrcItem y)
        {
            if (y == null && x == null)
                return 0;
            if (y == null)
                return 1;
            if (x == null)
                return -1;

            return x.time.CompareTo(y.time);
        }

        private void ParseLine(string line)
        {
            if (line.StartsWith("[ti:", StringComparison.Ordinal))
            {
                Info.title = line.Substring(4, line.Length - 5);
            }
            else if (line.StartsWith("[ar:", StringComparison.Ordinal))
            {
                Info.singer = line.Substring(4, line.Length - 5);
            }
            else if (line.StartsWith("[al:", StringComparison.Ordinal))
            {
                Info.album = line.Substring(4, line.Length - 5);
            }
            else if (line.StartsWith("[by:", StringComparison.Ordinal))
            {
                //ign
            }
            else
            {
                var sntnc = ExtractSentence(line);
                if (sntnc == null) return;
                var content = sntnc[sntnc.Length - 1];
                for (int i = 0; i < sntnc.Length - 1; i++)
                {
                    Info.items.Add(new LrcItem(ParseTime(sntnc[i]), content));
                }
            }
        }

        string[] ExtractSentence(string line)
        {
            List<string> parts = new List<string>(4);

            if (line.Length < 3 || line[0] != '[') return null;
            int borderPos;
            if ((borderPos = line.IndexOf(']')) < 2) return null;
            parts.Add(line.Substring(1, borderPos - 1));

            // Check if it has more timestamps
            int lastBorderPos;
            if (borderPos != (lastBorderPos = line.LastIndexOf(']')))
            {
                int nextPos;
                do
                {
                    nextPos = line.IndexOf(']', borderPos + 1);
                    // +2 because of ][
                    parts.Add(line.Substring(borderPos + 2, nextPos - borderPos - 2));
                    borderPos = nextPos;
                } while (nextPos < lastBorderPos);
            }
            parts.Add(line.Substring(lastBorderPos + 1));

            return parts.ToArray();
        }

        private long ParseTime(string time)
        {
            string[] timeParts = time.Split(':', '.');
            if (timeParts == null) return -1;

            try
            {
                return
                Convert.ToInt32(timeParts[0], 10) * 60000L +
                Convert.ToInt32(timeParts[1], 10) * 1000L +
                Convert.ToInt32(timeParts[2], 10);
            }
            catch (FormatException)
            {
                return -1;
            }
        }
    }
}
