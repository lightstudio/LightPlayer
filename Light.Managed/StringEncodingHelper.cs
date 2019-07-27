//using System.IO;
//using System.Runtime.InteropServices.WindowsRuntime;

//namespace Light.NETCore.Text
//{
//    public static class StringEncodingHelper
//    {
//        private static IdentifyEncoding enc = new IdentifyEncoding();

//        public static string GetStringFromBytes([ReadOnlyArray] byte[] bytes)
//        {
//            if (bytes == null) return "";
//            var ms = new System.IO.MemoryStream();
//            ms.Write(bytes, 0, bytes.Length);
//            ms.Seek(0, SeekOrigin.Begin);
//            var encName = enc.GetEncodingName(ms);
//            ms.Seek(0, SeekOrigin.Begin);
//            if (encName == null)
//            {
//                StreamReader sr = new StreamReader(ms);
//                var tx = sr.ReadToEnd();
//                sr.Dispose();
//                return tx;
//            }
//            try
//            {
//                if (encName == "gb2312" || encName == "big5")
//                {

//                    using (var sr = new System.IO.StreamReader(ms, DbcsEncoding.GetDbcsEncoding(encName)))
//                    {
//                        return sr.ReadToEnd();
//                    }
//                }
//                using (var sr = new System.IO.StreamReader(ms, System.Text.Encoding.GetEncoding(encName)))
//                {
//                    return sr.ReadToEnd();
//                }
//            }
//            catch
//            {
//                StreamReader sr = new StreamReader(ms);
//                var tx = sr.ReadToEnd();
//                sr.Dispose();
//                return tx;
//            }
//        }
//    }
//}