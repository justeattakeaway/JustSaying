using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.SQS.Model;
using JustSaying.Messaging.MessageHandling;
using Xunit;

namespace JustSaying.UnitTests.AwsTools.MessageHandling
{
    public class MessageContextReaderTests
    {
        [Fact]
        public void NullByDefault()
        {
            var reader = new MessageContextReader();
            var readData = reader.Read();

            Assert.Null(readData);
        }

        [Fact]
        public void CanStoreAndRetrieve()
        {
            var data = MakeTestMessageContext();
            MessageContextReader.Write(data);

            var reader = new MessageContextReader();
            var readData = reader.Read();

            AssertSame(data, readData);
        }

        [Fact]
        public async Task CanStoreAndRetrieveAsync()
        {
            var data = MakeTestMessageContext();
            MessageContextReader.Write(data);

            await Task.Delay(250)
                .ConfigureAwait(false);

            var reader = new MessageContextReader();
            var readData = reader.Read();

            AssertSame(data, readData);
        }

        [Fact]
        public async Task DifferentThreadsHaveDifferentContexts()
        {
            var data1 = MakeTestMessageContext();
            var data2 = MakeTestMessageContext();

            var t1 = Task.Run(async () => await RunWithData(data1));
            var t2 =  Task.Run(async () => await RunWithData(data2));

            await Task.WhenAll(t1, t2);
        }

        [Fact]
        public async Task MultiThreads()
        {
            var tasks = new List<Task>();

            for (int i = 0; i < 10; i++)
            {
                var data = MakeTestMessageContext();
                var task = Task.Run(async () => await RunWithData(data));
                tasks.Add(task);
            }

            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task ThreadContextDoesNotEscape()
        {
            var data1 = MakeTestMessageContext();

            var t1 = Task.Run(async () => await RunWithData(data1));

            var reader = new MessageContextReader();
            var localData = reader.Read();
            Assert.Null(localData);

            await t1;

            localData = reader.Read();
            Assert.Null(localData);

        }

        private static async Task RunWithData(MessageContext data)
        {
            MessageContextReader.Write(data);
            var reader = new MessageContextReader();

            for (int i = 0; i < 5; i++)
            {
                await Task.Delay(100 + i)
                    .ConfigureAwait(false);

                var readData = reader.Read();
                AssertSame(data, readData);
            }
        }

        private static void AssertSame(MessageContext expected, MessageContext actual)
        {
            Assert.NotNull(expected);
            Assert.NotNull(actual);

            Assert.Equal(expected, actual);
            Assert.Equal(expected.Message, actual.Message);
            Assert.Equal(expected.Message.Body, actual.Message.Body);
            Assert.Equal(expected.QueueUri, actual.QueueUri);
        }

        private static MessageContext MakeTestMessageContext()
        {
            var uniqueness = Guid.NewGuid().ToString();
            var queueUri = new Uri("http://test.com/" + uniqueness);

            var sqsMessage = new Message
            {
                Body = "test message " + uniqueness
            };

            return new MessageContext(sqsMessage, queueUri);
        }
    }
}
