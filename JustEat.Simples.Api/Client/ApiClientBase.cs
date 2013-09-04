using System;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace JustEat.Simples.Api.Client
{
    public abstract class ApiClientBase
    {
        private readonly IApiSettings _apiSettings;
        private static IEnumerable<HttpStatusCode> _goodResponses = new[] { HttpStatusCode.OK, HttpStatusCode.Accepted };

        protected ApiClientBase(IApiSettings apiSettings)
        {
            _apiSettings = apiSettings;
        }

        protected T GetJson<T>(string url, dynamic payload)
        {
            var request = CreateWebRequest(url);
            request = SetUpWebRequest(request);

            if (payload != null)
            {
                var settings = new JsonSerializerSettings();

                if(CamelCasePropertyNames)
                {
                    settings.ContractResolver = new CamelCasePropertyNamesContractResolver();
                }

                var postBody = JsonConvert.SerializeObject(payload, settings);

                var bytes = Encoding.UTF8.GetBytes(postBody);

                request.ContentType = _apiSettings.ContentType;
                request.Method = "POST";
                request.ContentLength = bytes.Length;

                using (var s = request.GetRequestStream()) {
                    s.Write(bytes, 0, bytes.Length);
                }
            }

            return Request<T>(request);
        }

        protected virtual bool CamelCasePropertyNames
        {
            get { return false; }
        }

        protected T GetJson<T>(string url)
        {
            var request = CreateWebRequest(url);
            request = SetUpWebRequest(request);

            return Request<T>(request);
        }

        private T Request<T>(WebRequest request)
        {
            
            string body;

            using (var response = (HttpWebResponse)request.GetResponse())
            {
                if (!_goodResponses.Contains(response.StatusCode))
                    throw new Exception(String.Format("Server error (HTTP {0}: {1}).",
                                                      response.StatusCode,
                                                      response.StatusDescription
                                                      )
                                       );

                body = GetResponseString(response);
            }

            return body == null ? default(T) : JsonConvert.DeserializeObject<T>(body);
        }

        protected virtual string GetResponseString(HttpWebResponse response)
        {
            string body;
            // ReSharper disable AssignNullToNotNullAttribute
            using (var sr = new StreamReader(response.GetResponseStream()))
            {
                // ReSharper restore AssignNullToNotNullAttribute
                body = sr.ReadToEnd();
            }
            return body;
        }

        protected virtual string BuildUrl(string template, params object[] values)
        {
            var temp = values.Where(v => v != null).ToArray();
            return _apiSettings.Host + string.Format(template, temp);
        }

        protected virtual WebRequest CreateWebRequest(string url)
        {
            return WebRequest.Create(url);
        }

        protected virtual WebRequest SetUpWebRequest(WebRequest request)
        {
            string proxy = _apiSettings.ProxyIapi;

            if (!string.IsNullOrEmpty(proxy))
                request.Proxy = new WebProxy(proxy);

            request.Headers = new WebHeaderCollection
            {
                {HttpRequestHeader.AcceptCharset, _apiSettings.AcceptCharset},
                {HttpRequestHeader.AcceptLanguage, _apiSettings.AcceptLanguage}
            };
            return request;
        }
    }
}