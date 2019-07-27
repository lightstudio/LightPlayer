using Light.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Light.Utilities
{
    public class M3u
    {
        public static string Export(IEnumerable<PlaylistItem> items)
        {
            var ret = new StringBuilder("#EXTM3U\n");
            foreach (var item in items)
            {
                ret.AppendLine($"#EXTINF:0,{item.Title} - {item.Artist}\n{item.Path}");
            }
            return ret.ToString();
        }
        public static IEnumerable<string> Parse(IEnumerable<string> lines)
        {
            foreach (var line in lines)
            {
                if (!line.StartsWith("#") && !string.IsNullOrWhiteSpace(line))
                    yield return line;
            }
        }
    }
}
