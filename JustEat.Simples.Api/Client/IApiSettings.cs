namespace JustEat.Simples.Api.Client
{
    public interface IApiSettings
    {
        string AcceptCharset { get; set; }
        string AcceptLanguage { get; set; }
        string ContentType { get; set; }
        string Host { get; set; }
        string ProxyIapi { get; set; }
    }
}
