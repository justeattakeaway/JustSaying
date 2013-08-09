using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;

namespace JustEat.Simples.NotificationStack.AwsTools
{
    public class DynamoStore
    {
        private readonly DynamoDBContext _context;

        public DynamoStore(DynamoDBContext dynamoContext)
        {
            _context = dynamoContext;
        }

        public void Save<T>(T obj, DynamoDBOperationConfig operationConfig)
        {
            _context.Save(obj, operationConfig);
        }

        public T Read<T>(string key, DynamoDBOperationConfig operationConfig)
        {
            return _context.Load<T>(key, operationConfig);
        }

        public ICollection<T> Read<T>(IEnumerable<object> keys, DynamoDBOperationConfig operationConfig)
        {
            var batch = _context.CreateBatchGet<T>(operationConfig);

            if (batch == null)
                throw new Exception(String.Format(CultureInfo.InvariantCulture,
                                                        "{0}, CreateBatchGet<{1}> returned null",
                                                        GetType().Name, typeof(T)));

            keys.ToList().ForEach(batch.AddKey);

            batch.Execute();

            return (batch.Results);
        }


        public void DeleteRow<T>(string key, DynamoDBOperationConfig operationConfig)
        {
            _context.Delete<T>(key, operationConfig);
        }

    }
}
