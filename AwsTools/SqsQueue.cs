using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;

namespace JustEat.AwsTools
{
    public class SqsQueue
    {
        public string QueueNamePrefix { get; private set; }
        public string Arn { get; private set; }
        public string Url { get; private set; }
        public AmazonSQS Client { get; private set; }

        public SqsQueue(string queueName, AmazonSQS client)
        {
            QueueNamePrefix = queueName;
            Client = client;
            Exists();
        }

        public bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest().WithQueueNamePrefix(QueueNamePrefix));
            if (result.IsSetListQueuesResult() && result.ListQueuesResult.IsSetQueueUrl())
            {
                Url = result.ListQueuesResult.QueueUrl.First();
                SetArn();
                return true;
            }

            return false;
        }

        public bool Create(int attempt = 0)
        {
            try
            {
                var result = Client.CreateQueue(new CreateQueueRequest().WithQueueName(QueueNamePrefix));
                if (result.IsSetCreateQueueResult() && !string.IsNullOrWhiteSpace(result.CreateQueueResult.QueueUrl))
                {
                    Url = result.CreateQueueResult.QueueUrl;
                    SetArn();
                    return true;
                }
            }
            catch (AmazonSQSException ex)
            {
                if (attempt >= 2)
                    throw;

                if (ex.ErrorCode == "AWS.SimpleQueueService.QueueDeletedRecently")
                {
                    // Ensure we wait for queue delete timeout to expire.
                    Thread.Sleep(60000);
                    Create(attempt ++);
                }
            }

            return false;
        }

        public void Delete()
        {
            Arn = null;
            Url = null;

            if (!Exists())
                return;

            var result = Client.DeleteQueue(new DeleteQueueRequest().WithQueueUrl(Url));
            //return result.IsSetResponseMetadata();
            Arn = null;
            Url = null;
        }

        private void SetArn()
        {
            Arn = GetAttrs(new[] { "QueueArn" }).QueueARN;
        }
        
        private GetQueueAttributesResult GetAttrs(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest().WithQueueUrl(Url);
            foreach (var key in attrKeys)
                request.WithAttributeName(key);

            var result = Client.GetQueueAttributes(request);

            return result.GetQueueAttributesResult;
        }

        public void AddPermission(SnsTopic snsTopic)
        {
            Client.SetQueueAttributes(new SetQueueAttributesRequest().WithQueueUrl(Url).WithPolicy(GetQueueSubscriptionPilocy(snsTopic)));
        }

        private string GetQueueSubscriptionPilocy(SnsTopic topic)
        {
            return @"{
                                                      ""Version"": ""2012-10-17"",
                                                      ""Id"": ""Sns_Subsciption_Policy"",
                                                      ""Statement"": 
                                                        {
                                                           ""Sid"":""Send_Message"",
                                                           ""Effect"": ""Allow"",
                                                           ""Principal"": {
                                                                ""AWS"": ""*""
                                                             },
                                                            ""Action"": ""SQS:SendMessage"",
                                                            ""Resource"": """ + Arn + @""",
                                                            ""Condition"" : {
															   ""ArnEquals"" : {
																  ""aws:SourceArn"":""" + topic.Arn + @"""
															   }
															}
                                                         }
                                                    }";
        }
        private static string GetQueueSubscriptionPilocyNoArn(SnsTopic topic)
        {
            return @"{
                        ""Version"": ""2012-10-17"",
                        ""Id"": ""Sns_Subsciption_Policy"",
                        ""Statement"": {
                            ""Sid"":""Send_Message"",
                            ""Effect"": ""Allow"",
                            ""Principal"": {
                                ""AWS"": ""*""
                                },
                            ""Action"": ""SQS:SendMessage""
                            }
                    }";
        }
    }
}
