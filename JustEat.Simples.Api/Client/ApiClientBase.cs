using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;

namespace JustEat.Simples.Api.Client
{
    public abstract class ApiClientBase
    {
        private readonly ApiSettings _apiSettings;

        protected ApiClientBase(ApiSettings apiSettings)
        {
            _apiSettings = apiSettings;
        }

        protected T GetJson<T>(string url, dynamic payload)
        {
            var request = CreateWebRequest(url);

            if (payload != null) {
                var postBody = JsonConvert.SerializeObject(payload);
                var bytes = Encoding.UTF8.GetBytes(postBody);

                request.ContentType = _apiSettings.ContentType;
                request.Method = "POST";
                request.ContentLength = bytes.Length;

                using (var s = request.GetRequestStream()) {
                    s.Write(bytes, 0, bytes.Length);
                }
            }

            string body;

            using (var response = (HttpWebResponse)request.GetResponse()) {
// ReSharper disable AssignNullToNotNullAttribute
                using (var sr = new StreamReader(response.GetResponseStream())) {
// ReSharper restore AssignNullToNotNullAttribute
                    body = sr.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<T>(body);
        }

        protected T GetJson<T>(string url)
        {
            var request = CreateWebRequest(url);

            string body;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (response.StatusCode != HttpStatusCode.OK)
                    throw new Exception(String.Format(
                    "Server error (HTTP {0}: {1}).",
                    response.StatusCode,
                    response.StatusDescription));
// ReSharper disable AssignNullToNotNullAttribute
                using (var sr = new StreamReader(response.GetResponseStream())) {
// ReSharper restore AssignNullToNotNullAttribute
                    body = sr.ReadToEnd();
                }
            }

            return JsonConvert.DeserializeObject<T>(body);
        }

        protected string BuildUrl(string template, params object[] values)
        {
            var temp = values.Where(v => v != null).ToArray();
            return _apiSettings.Host + string.Format(template, temp);
        }

        private WebRequest CreateWebRequest(string url)
        {
            string proxy = ConfigurationManager.AppSettings["Proxy-iapi"];
            var request = WebRequest.Create(url);
            if (!string.IsNullOrEmpty(proxy))
                request.Proxy = new WebProxy(proxy);

            request.Headers = new WebHeaderCollection
                              {
                                  { HttpRequestHeader.AcceptCharset, _apiSettings.AcceptCharset },
                                  { HttpRequestHeader.AcceptLanguage, _apiSettings.AcceptLanguage }
                              };
            return request;
        }
    }
}