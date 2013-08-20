namespace JustEat.Simples.Api.Client
{
    public class ApiSettings : IApiSettings
    {
        public string AcceptCharset { get; set; }

        public string AcceptLanguage { get; set; }

        public string Host { get; set; }

        public string ContentType { get; set; }

        public string ProxyIapi { get; set; }
    }
}
