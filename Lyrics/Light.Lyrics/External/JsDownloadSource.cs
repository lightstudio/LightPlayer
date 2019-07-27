using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using LightLrcComponent;
using ChakraBridge;

namespace Light.Lyrics.External
{
    public class JsDownloadSource : IDisposable
    {
        ChakraHost host;
        JsApi api = new JsApi();
        XMLHttpRequest xml = new XMLHttpRequest();

        public string Name { get; set; }

        public JsDownloadSource(string jsContent, string name, bool debug = false, string debugSource = "")
        {
            host = new ChakraHost(debug, false);
            host.ProjectNamespace("Windows.Data.Xml.Dom");
            host.ProjectNamespace("Windows.Networking");
            host.ProjectNamespace("Windows.Web");
            host.ProjectNamespace("LightLrcComponent");
            host.ProjectObjectToGlobal(xml, "xmlhttp");
            host.ProjectObjectToGlobal(api, "api");
            host.RunScript(jsContent, debugSource);
            host.RemoveContextOnCurrentThread();
            Name = name;
        }

        public ExternalLrcInfo[] LookupLrc(string Title, string Artist)
        {
            lock (host)
            {
                try
                {
                    host.SetContextOnCurrentThread();
                    host.CallFunction("lookupLrc", Title, Artist);

                    var lrcs = api.GetLrcs();
                    for (int i = 0; i < lrcs.Length; i++) lrcs[i].Source = Name;
                    return lrcs;
                }
                catch
                {
                    //ignore
                    return ExternalLrcInfo.EmptyArray;
                }
                finally
                {
                    host.RemoveContextOnCurrentThread();
                }
            }
        }

        public async Task<ExternalLrcInfo[]> LookupLrcAsync(string Title, string Artist)
        {
            return await Task.Run(() => LookupLrc(Title, Artist));
        }

        public string DownloadLrc(ExternalLrcInfo result)
        {
            lock (host)
            {
                return (string)host.CallFunction("downloadLrc", result);
            }
        }

        public async Task<string> DownloadLrcAsync(ExternalLrcInfo result)
        {
            return await Task.Run(() => DownloadLrc(result));
        }

        public void Dispose()
        {
            host.Dispose();
        }
    }
}
