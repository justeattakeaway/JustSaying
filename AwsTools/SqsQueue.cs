using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using Amazon.SQS;
using Amazon.SQS.Model;
using Newtonsoft.Json.Linq;
using SimplesNotificationStack.Messaging.MessageSerialisation;

namespace JustEat.AwsTools
{
    public abstract class SqsQueueBase
    {
        public string Arn { get; protected set; }
        public string Url { get; protected set; }
        public AmazonSQS Client { get; private set; }
        public string QueueNamePrefix { get; protected set; }

        public SqsQueueBase(AmazonSQS client)
        {
            Client = client;
        }

        public abstract bool Exists();

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

        protected void SetArn()
        {
            Arn = GetAttrs(new[] { "QueueArn" }).QueueARN;
        }

        protected GetQueueAttributesResult GetAttrs(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest().WithQueueUrl(Url);
            foreach (var key in attrKeys)
                request.WithAttributeName(key);

            var result = Client.GetQueueAttributes(request);

            return result.GetQueueAttributesResult;
        }

        public void AddPermission(SnsTopicBase snsTopic)
        {
            Client.SetQueueAttributes(new SetQueueAttributesRequest().WithQueueUrl(Url).WithPolicy(GetQueueSubscriptionPilocy(snsTopic)));
        }

        protected string GetQueueSubscriptionPilocy(SnsTopicBase topic)
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

        public static SimplesNotificationStack.Messaging.Messages.Message GetMessage(Message message)
        {
            // ToDo: This is 'rough temp code until I understand exectly how to treat / wrap SQS

            return SerialisationMap.GetMap(JObject.Parse(message.Body)["Subject"].ToString())
                .Deserialised(JObject.Parse(message.Body)["Message"].ToString());
        }
    }

    public class SqsQueueByName : SqsQueueBase
    {
        public SqsQueueByName(string queueName, AmazonSQS client)
            : base(client)
        {
            QueueNamePrefix = queueName;
            Exists();
        }

        public override bool Exists()
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
                    Create(attempt++);
                }
            }

            return false;
        }
    }

    public class SqsQueueByUrl : SqsQueueBase
    {
        public SqsQueueByUrl(string queueUrl, AmazonSQS client)
            : base(client)
        {
            Url = queueUrl;
        }

        public override bool Exists()
        {
            var result = Client.ListQueues(new ListQueuesRequest());
            if (result.IsSetListQueuesResult() && result.ListQueuesResult.IsSetQueueUrl() && result.ListQueuesResult.QueueUrl.Any(x => x == Url))
            {
                SetArn();
                // Need to set the prefix yet!
                return true;
            }

            return false;
        }
    }
}
