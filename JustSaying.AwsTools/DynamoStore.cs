using System.Collections.Generic;
using System.Linq;
using Amazon.DynamoDBv2.DataModel;

namespace JustSaying.AwsTools
{
    public class DynamoStore : IDynamoStore
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

            keys.ToList().ForEach(batch.AddKey);

            batch.Execute();

            return batch.Results;
        }

        public void DeleteRow<T>(string key, DynamoDBOperationConfig operationConfig)
        {
            _context.Delete<T>(key, operationConfig);
        }
    }
}
