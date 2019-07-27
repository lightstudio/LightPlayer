using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;

namespace LightLrcComponent
{
    public delegate void XHREventHandler();

    public sealed class XMLHttpRequest
    {
        readonly Dictionary<string, string> headers = new Dictionary<string, string>();
        Uri uri;
        string httpMethod;
        private int _readyState;

        public int status { get; set; }

        public int readyState
        {
            get { return _readyState; }
            private set
            {
                _readyState = value;

                try { onreadystatechange?.Invoke(); } catch { }
            }
        }

        public byte[] response { get; private set; }

        public string responseText
        {
            get { return Encoding.UTF8.GetString(response); }
        }

        public string responseType
        {
            get; private set;
        }

        public bool withCredentials { get; set; }

        public XHREventHandler onreadystatechange { get; set; }

        public void setRequestHeader(string key, string value) => headers[key] = value;

        public string getResponseHeader(string key)
        {
            headers.TryGetValue(key, out string header);
            return header;
        }

        public void open(string method, string url, bool async)
        {
            httpMethod = method;
            uri = new Uri(url);

            readyState = 1;
        }

        public void send(string data)
        {
            SendAsync(data).Wait();
        }

        public void send()
        {
            SendAsync(null).Wait();
        }

        async Task SendAsync(string data)
        {
            using (var httpClient = new HttpClient())
            {
                foreach (var header in headers)
                {
                    if (header.Key.StartsWith("Content"))
                    {
                        continue;
                    }
                    httpClient.DefaultRequestHeaders.Add(header.Key, header.Value);
                }

                readyState = 2;

                HttpRequestMessage requestMessage = new HttpRequestMessage(new HttpMethod(httpMethod), uri);
                
                if(requestMessage.Method.Method == "POST")
                    requestMessage.Content = 
                        new StringContent(data, Encoding.UTF8, "application/x-www-form-urlencoded");

                HttpResponseMessage responseMessage = await httpClient.SendAsync(requestMessage);

                if (responseMessage != null)
                {
                    using (responseMessage)
                    {
                        status = (int)responseMessage.StatusCode;
                        using (var content = responseMessage.Content)
                        {
                            responseType = "text";
                            response = await content.ReadAsByteArrayAsync();
                            readyState = 4;
                        }
                    }
                }
            }
        }
    }
}
