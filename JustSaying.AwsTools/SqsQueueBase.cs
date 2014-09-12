using System;
using System.Collections.Generic;
using Amazon.SQS;
using Amazon.SQS.Model;
using JustSaying.AwsTools.QueueCreation;
using NLog;
using Newtonsoft.Json.Linq;
using JustSaying.Messaging.MessageSerialisation;

namespace JustSaying.AwsTools
{
    public abstract class SqsQueueBase
    {
        public string Arn { get; protected set; }
        public string Url { get; protected set; }
        public IAmazonSQS Client { get; private set; }
        public string QueueNamePrefix { get; protected set; }
        public ErrorQueue ErrorQueue { get; protected set; }
        internal int MessageRetentionPeriod { get; set; }
        internal int VisibilityTimeout { get; set; }
        internal RedrivePolicy RedrivePolicy { get; set; }
        
        private static readonly Logger Log = LogManager.GetLogger("JustSaying");

        public SqsQueueBase(IAmazonSQS client)
        {
            Client = client;
        }

        public abstract bool Exists();

        public virtual void Delete()
        {
            Arn = null;
            Url = null;

            if (!Exists())
                return;

            var result = Client.DeleteQueue(new DeleteQueueRequest { QueueUrl = Url });
            //return result.IsSetResponseMetadata();
            Arn = null;
            Url = null;
        }

        protected void SetQueueProperties()
        {
            var attributes = GetAttrs(new[]
            {
                JustSayingConstants.ATTRIBUTE_ARN, 
                JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY,
                JustSayingConstants.ATTRIBUTE_POLICY,
                JustSayingConstants.ATTRIBUTE_RETENTION_PERIOD,
                JustSayingConstants.ATTRIBUTE_VISIBILITY_TIMEOUT
            });
            Arn = attributes.QueueARN;
            MessageRetentionPeriod = attributes.MessageRetentionPeriod;
            VisibilityTimeout = attributes.VisibilityTimeout;
            RedrivePolicy = ExtractRedrivePolicyFromQueueAttributes(attributes.Attributes);
        }

        private RedrivePolicy ExtractRedrivePolicyFromQueueAttributes(Dictionary<string, string> queueAttributes)
        {
            if (!queueAttributes.ContainsKey(JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY))
                return null;
            return RedrivePolicy.ConvertFromString(queueAttributes[JustSayingConstants.ATTRIBUTE_REDRIVE_POLICY]);
        }

        protected GetQueueAttributesResult GetAttrs(IEnumerable<string> attrKeys)
        {
            var request = new GetQueueAttributesRequest { 
                QueueUrl = Url,
                AttributeNames = new List<string>(attrKeys)
            };
            
            var result = Client.GetQueueAttributes(request);

            return result;
        }

        public void AddPermission(SnsTopicBase snsTopic)
        {
            Client.SetQueueAttributes(
                new SetQueueAttributesRequest{
                    QueueUrl = Url,
                    Attributes = new Dictionary<string,string>{ {"Policy", GetQueueSubscriptionPilocy(snsTopic) } }
                });
                
            Log.Info(string.Format("Added Queue permission for SNS topic to publish to Queue: {0}, Topic: {1}", Arn, snsTopic.Arn));
        }

        public bool HasPermission(SnsTopicBase snsTopic)
        {
            var policyResponse = Client.GetQueueAttributes(
                new GetQueueAttributesRequest{
                 QueueUrl = Url,
                 AttributeNames = new List<string> { "Policy" }});

            if (string.IsNullOrEmpty(policyResponse.Policy))
                return false;

            return policyResponse.Policy.Contains(snsTopic.Arn);
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
    }
}
