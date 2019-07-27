/*
*   XiamiAudioInfo.cs
*
*   Date: 4th November, 2014 Author: David Huang
*   (C) 2014 Little Moe, LLC.  All Rights Reserved.
*/

using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Windows.Data.Xml.Dom;
using HtmlAgilityPack;

namespace Light.Managed.Online.Xiami
{
    /// <summary>
    /// XiamiAudioInfo
    /// 虾米音乐详细信息
    /// </summary>
    public class XiamiAudioInfo
    {
        private const string SongUrl = "http://www.xiami.com/song/playlist/id/{0}";
        private const string ArtistProfileUrl = "http://www.xiami.com/artist/profile/id/{0}";

        static string DecodeXiamiDownloadString(string orString)
        {
            if (orString == null)
                return null;
            var l = int.Parse(orString.Substring(0, 1));
            var t = orString.Substring(1);
            var tn = t.Length;
            var ln = (int)Math.Floor(tn / (double)l);
            var r = tn % l;
            var text = "";
            for (var i = 0; i <= ln; i++)
            {
                for (var j = 0; j < l; j++)
                {
                    var n = j * ln + i;
                    if (j < r)
                        n += j;
                    else
                        n += r;
                    if (n < t.Length)
                        text += t[n];
                    else break;
                }
            }
            var url = text.Substring(0, tn);
            url = WebUtility.UrlDecode(url);
            url = url.Replace('^', '0');
            url = url.Replace('%', '|');
            return url;
        }
        static string SafeGetResultString(XmlElement result, string elementId)
        {
            try
            {
                return result.GetElementsByTagName(elementId)[0].ChildNodes[0].NodeValue as string;
            }
            catch
            {
                return "";
            }
        }

        public XiamiAudioInfo This => this;

        public string Title { get; private set; }
        public string SongId { get; private set; }
        public string AlbumId { get; private set; }
        public string AlbumName { get; private set; }
        public string ArtistId { get; private set; }
        public string Artist { get; private set; }

        public string ArtistIntroText { get; private set; }

        public string BackgroundImageUrl { get; private set; }
        public string DownloadUrl { get; private set; }
        public string LyricUrl { get; private set; }
        public string PictureUrl { get; private set; }
        public string AlbumPictureUrl { get; private set; }

        /// <summary>
        /// 构造虾米音乐详细信息。
        /// 需调用LoadFullInfoAsync以获取详细信息。
        /// </summary>
        /// <param name="songId">歌曲ID</param>
        /// <param name="title">歌曲标题</param>
        /// <param name="artist">艺术家</param>
        public XiamiAudioInfo(string songId, string title, string artist)
        {
            Title = title;
            Artist = artist;
            SongId = songId;
        }

        /// <summary>
        /// 异步获取歌曲完整信息
        /// 需在try catch块中使用以捕获网络异常。
        /// </summary>
        public async Task LoadFullInfoAsync()
        {
            var sUrl = string.Format(SongUrl, SongId);
            var sReq = (HttpWebRequest) WebRequest.Create(sUrl);

            string infText;
            using (var serverInitialQueryResponse = await sReq.GetResponseAsync())
            {
                using (var infStream = serverInitialQueryResponse.GetResponseStream())
                    using (var infSr = new StreamReader(infStream, System.Text.Encoding.UTF8))
                        infText = infSr.ReadToEnd();
            }
                
            var doc = new XmlDocument();
            doc.LoadXml(infText);

            var trackList = doc.GetElementsByTagName("trackList");

            foreach (var xmlNode in trackList)
            {
                var element = (XmlElement) xmlNode;
                var track = element.GetElementsByTagName("track");
                foreach (var xmlNode1 in track)
                {
                    var result = (XmlElement) xmlNode1;

                    AlbumId = SafeGetResultString(result, "album_id");
                    AlbumName = SafeGetResultString(result, "album_name");
                    AlbumPictureUrl = SafeGetResultString(result, "album_pic");
                    Artist = SafeGetResultString(result, "artist");
                    ArtistId = SafeGetResultString(result, "artist_id");
                    BackgroundImageUrl = SafeGetResultString(result, "background");
                    DownloadUrl = DecodeXiamiDownloadString(SafeGetResultString(result, "location"));
                    LyricUrl = SafeGetResultString(result, "lyric_url");
                    PictureUrl = SafeGetResultString(result, "pic");
                    Title = SafeGetResultString(result, "title");
                    SongId = SafeGetResultString(result, "song_id");
                }
            }
            try
            {
                var url = string.Format(ArtistProfileUrl, ArtistId);
                var req = (HttpWebRequest) WebRequest.Create(url);
                string text;

                using (var serverQueryResponse = await req.GetResponseAsync())
                {
                    using (var stream = serverQueryResponse.GetResponseStream())
                    using (var sr = new StreamReader(stream, System.Text.Encoding.UTF8))
                        text = sr.ReadToEnd();
                }

                var htmldoc = new HtmlDocument();
                htmldoc.LoadHtml(text);

                var main = htmldoc.GetElementbyId("main");
                if (main != null)
                {
                    var profilenode = main.ChildNodes["div"];
                    var innerhtml = profilenode.InnerHtml;
                    ArtistIntroText = Windows.Data.Html.HtmlUtilities.ConvertToText(innerhtml);
                }
            }
            catch (WebException)
            {
                // Ignored, assume network error
            }
        }

    }
}