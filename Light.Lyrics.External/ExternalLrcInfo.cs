using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightLrcComponent
{
    public sealed class ExternalLrcInfo
    {
        public static ExternalLrcInfo[] EmptyArray { get; } = new ExternalLrcInfo[0];

        public object Opaque { get; set; }

        public string Title { get; set; }

        public string Artist { get; set; }

        public string Album { get; set; }

        public string Source { get; set; }
    }
}
