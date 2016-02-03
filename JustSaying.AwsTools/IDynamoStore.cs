using System.Collections.Generic;
using Amazon.DynamoDBv2.DataModel;

namespace JustSaying.AwsTools
{
    public interface IDynamoStore
    {
        void DeleteRow<T>(string key, DynamoDBOperationConfig operationConfig);
        ICollection<T> Read<T>(IEnumerable<object> keys, DynamoDBOperationConfig operationConfig);
        T Read<T>(string key, DynamoDBOperationConfig operationConfig);
        void Save<T>(T obj, DynamoDBOperationConfig operationConfig);
    }
}
