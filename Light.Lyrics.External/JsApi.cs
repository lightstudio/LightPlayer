using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LightLrcComponent
{
    public sealed class JsApi
    {
        List<ExternalLrcInfo> lrcList = new List<ExternalLrcInfo>(4);
        
        static JsApi()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        public void Log(string message)
        {
            System.Diagnostics.Debug.WriteLine(message);
        }

        public int IntMul(int x,int y)
        {
            unchecked
            {
                return x * y;
            }
        }

        public ExternalLrcInfo CreateLrc()
        {
            var lrc = new ExternalLrcInfo();
            lrcList.Add(lrc);
            return lrc;
        }

        internal ExternalLrcInfo[] GetLrcs()
        {
            var lrcs = lrcList.ToArray();
            lrcList.Clear();
            return lrcs;
        }

        public string DecodeAsCodePage(XMLHttpRequest req, int codepage)
        {
            return Encoding.GetEncoding(codepage).GetString(req.response);
        }

        public int CharToCodePage(string c, int codepage)
        {
            var bytes = Encoding.GetEncoding(codepage).GetBytes(c);
            if (bytes.Length == 1)
                return bytes[0] << 8;
            else if (bytes.Length == 2)
                return bytes[0] << 8 | bytes[1];
            else return 0;
        }
    }
}
