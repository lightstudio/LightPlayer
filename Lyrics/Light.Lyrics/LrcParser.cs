using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Light.Lyrics.Model;
using System.IO;

namespace Light.Lyrics
{
    static class LrcParser
    {
        static Lazy<LrcSentenceComparer> Comparer = new Lazy<LrcSentenceComparer>();

        public static ParsedLrc Parse(string lrcText)
        {
            return Parse(new StringReader(lrcText));
        }

        public static ParsedLrc Parse(Stream lrcStream, bool leaveOpen = true)
        {
            using (var reader = new StreamReader(lrcStream,
                Encoding.UTF8, false, 1024, leaveOpen))
            {
                return Parse(reader);
            }
        }

        public static ParsedLrc Parse(TextReader reader)
        {
            ParsedLrc lrc = new ParsedLrc();

            string line;

            // TextReader will handle the newline well.
            while ((line = reader.ReadLine()?.Trim()) != null)
            {
                var parts = ExtractMorpheme(line);
                if (parts == null || parts.Length < 1) continue;
                // Metadata line
                if (parts[0][2] == ':') SetMetadata(line, lrc);

                var content = parts[parts.Length - 1];
                for (int i = 0; i < parts.Length - 1; i++)
                {
                    var time = ParseTime(parts[i]);
                    if (time != -1)
                    {
                        lrc.Sentences.Add(new LrcSentence(time, content));
                    }
                }
            }

            // List<T>.Sort still creates wrapper around Comparison<T>.
            // A straightforward method is using IComparer<T>.
            lrc.Sentences.Sort(Comparer.Value);

            return lrc;
        }

        static string[] ExtractMorpheme(string line)
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
            if (lastBorderPos != line.Length - 1)
            {
                parts.Add(line.Substring(lastBorderPos + 1));
            }
            else
            {
                parts.Add(string.Empty);
            }

            return parts.ToArray();
        }

        static void SetMetadata(string line, ParsedLrc lrc)
        {
            string metadata = line.Substring(4, line.Length - 5);
            switch (line[1])
            {
                case 'a':
                    if (line[2] == 'l') lrc.Album = metadata;
                    if (line[2] == 'r') lrc.Artist = metadata;
                    break;
                case 't':
                    if (line[2] == 'i') lrc.Title = metadata;
                    break;
            }
        }

        static long ParseTime(string time)
        {
            string[] timeParts = time.Split(':', '.');
            if (timeParts == null || timeParts.Length < 3) return -1;

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

        class LrcSentenceComparer : IComparer<LrcSentence>
        {
            public int Compare(LrcSentence x, LrcSentence y)
            {
                if (y == null && x == null)
                    return 0;
                if (y == null)
                    return 1;
                if (x == null)
                    return -1;

                return x.Time.CompareTo(y.Time);
            }
        }
    }
}
