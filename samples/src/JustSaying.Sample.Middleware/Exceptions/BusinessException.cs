namespace JustSaying.Sample.Middleware.Exceptions;

public class BusinessException : Exception
{
    public string MessageId { get; set; }
}
