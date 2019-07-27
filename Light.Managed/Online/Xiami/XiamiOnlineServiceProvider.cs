/*
*   XiamiOnlineServiceProvider.cs
*
*   Date: 4th November, 2014 Author: David Huang
*   (C) 2014 Little Moe, LLC.  All Rights Reserved.
*/

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Light.Managed.Online.Xiami
{
    /// <summary>
    /// 虾米在线服务API
    /// </summary>
    public sealed class XiamiOnlineServiceProvider
    {
        private const string LookupUrl = "http://www.xiami.com/web/search-songs?key={0}+{1}";

        /// <summary>
        /// 异步搜索歌曲
        /// </summary>
        /// <param name="title">歌曲标题</param>
        /// <param name="singer">艺术家</param>
        /// <returns>歌曲信息列表（未载入详细信息）</returns>
        public async Task<IList<XiamiAudioInfo>> GetAudioInfoListAsync(string title, string singer)
        {
            if (CultureInfo.CurrentCulture.TwoLetterISOLanguageName != "zh")
            {
                throw new NotSupportedException();
            }

            var url = string.Format(LookupUrl, title, singer);
            var req = (HttpWebRequest) WebRequest.Create(url);
            using (var serverQueryResponse = await req.GetResponseAsync())
            {
                using (var serverResponseStream = serverQueryResponse.GetResponseStream())
                {
                    string serverResponseText;
                    using (var sr = new StreamReader(serverResponseStream, System.Text.Encoding.UTF8))
                        serverResponseText = sr.ReadToEnd();

                    var objarr = (JArray)JsonConvert.DeserializeObject(serverResponseText);
                    if (objarr == null)
                        return new List<XiamiAudioInfo>();
                    return 
                        (from obj in objarr
                         let id = obj["id"]
                         let songTitle = obj["title"]
                         let author = obj["author"]
                         select new XiamiAudioInfo((string)id, (string)songTitle, (string)author)).ToList();
                }
            }
        }
    }
}
