using System;

namespace JustSaying.TestingFramework
{
    [Serializable]
    public class TestException : Exception
    {
        public TestException() { }
        public TestException(string message) : base(message)
        { }
    }
}
