using System.Runtime.Serialization;

namespace JustSaying.TestingFramework;

public class TestException : Exception
{
    public TestException()
    {
    }

    public TestException(string message) : base(message)
    {
    }

    public TestException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
