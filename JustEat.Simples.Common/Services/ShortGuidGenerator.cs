using System;
namespace JustEat.Simples.Common.Services
{
    public class ShortGuidGenerator : IIdGenerator
    {
        public string NewId()
        {
            var encodedString = Convert.ToBase64String(Guid.NewGuid().ToByteArray())
               .Replace('/', '0')
               .Replace('=', '1')
               .Replace('+', '2');

            return encodedString.Substring(0, 22);
        }
    }
}
