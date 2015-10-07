using System;
using System.IO;
using JustSaying.Messaging.Documentation;
using JustSaying.Messaging.Interrogation;
using JustSaying.Models;
using NUnit.Framework;

namespace JustSaying.Messaging.UnitTests.Documentation
{
    [TestFixture]
    public class WhenGeneratingIndexPage
    {
        private readonly IAmJustDocumenting _documentor = new Documenter();

        [Test]
        public void ContentsContainsAllRegisteredSubscribersAndPublishers()
        {
            // arrange
            const string filename = "JustSaying.html";
            IInterrogationResponse interrogationResponse = new InterrogationResponse(
                 new [] { new Subscriber(typeof(TestMessage1)), new Subscriber(typeof(TestMessage2)) },
                 new[] { new Publisher(typeof(TestMessage3)) });

            // act
            _documentor.CreateIndexPage(filename, interrogationResponse);

            // assert
            Assert.That(File.Exists(filename), Is.True);
            var contents = File.ReadAllText(filename);
            var subscribersIndex = contents.IndexOf("Subscribers", StringComparison.Ordinal);
            var publishersIndex = contents.IndexOf("Publishers", StringComparison.Ordinal);
            Assert.That(publishersIndex, Is.GreaterThan(0));
            Assert.That(subscribersIndex, Is.GreaterThan(0));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage1", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage2", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage3", StringComparison.Ordinal), Is.GreaterThan(publishersIndex));
        }

        [Test]
        [TestCase(null)]
        [TestCase("")]
        public void ThrowsNullArgumentExceptionForNullOrEmptyPath(string path)
        {
            // arrange
            IInterrogationResponse interrogationResponse = new InterrogationResponse(null, null);

            // act, assert
            Assert.Throws<ArgumentNullException>(() => _documentor.CreateIndexPage(null, interrogationResponse));
        }

        [Test]
        public void ThrowsNullArgumentExceptionForNullInterrogationResponse()
        {
            // act, assert
            Assert.Throws<ArgumentNullException>(() => _documentor.CreateIndexPage("JustSaying.html", null));
        }

        [Test]
        public void HandlesNullSubscribers()
        {
            // arrange
            const string filename = "JustSaying.html";
            IInterrogationResponse interrogationResponse = new InterrogationResponse(
                 null,
                 new[] { new Publisher(typeof(TestMessage3)) });

            // act
            _documentor.CreateIndexPage(filename, interrogationResponse);

            // assert
            Assert.That(File.Exists(filename), Is.True);
            var contents = File.ReadAllText(filename);
            var subscribersIndex = contents.IndexOf("Subscribers", StringComparison.Ordinal);
            var publishersIndex = contents.IndexOf("Publishers", StringComparison.Ordinal);
            Assert.That(publishersIndex, Is.GreaterThan(0));
            Assert.That(subscribersIndex, Is.GreaterThan(0));
            Assert.That(contents.IndexOf("No subscribers registered", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage3", StringComparison.Ordinal), Is.GreaterThan(publishersIndex));
        }

        [Test]
        public void HandlesEmptySubscribers()
        {
            // arrange
            const string filename = "JustSaying.html";
            IInterrogationResponse interrogationResponse = new InterrogationResponse(
                 new Subscriber[0], 
                 new [] { new Publisher(typeof(TestMessage3)) });

            // act
            _documentor.CreateIndexPage(filename, interrogationResponse);

            // assert
            Assert.That(File.Exists(filename), Is.True);
            var contents = File.ReadAllText(filename);
            var subscribersIndex = contents.IndexOf("Subscribers", StringComparison.Ordinal);
            var publishersIndex = contents.IndexOf("Publishers", StringComparison.Ordinal);
            Assert.That(publishersIndex, Is.GreaterThan(0));
            Assert.That(subscribersIndex, Is.GreaterThan(0));
            Assert.That(contents.IndexOf("No subscribers registered", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage3", StringComparison.Ordinal), Is.GreaterThan(publishersIndex));
        }

        [Test]
        public void HandlesNullPublishers()
        {
            // arrange
            const string filename = "JustSaying.html";
            IInterrogationResponse interrogationResponse = new InterrogationResponse(
                 new[] { new Subscriber(typeof(TestMessage1)), new Subscriber(typeof(TestMessage2)) },
                 null);

            // act
            _documentor.CreateIndexPage(filename, interrogationResponse);

            // assert
            Assert.That(File.Exists(filename), Is.True);
            var contents = File.ReadAllText(filename);
            var subscribersIndex = contents.IndexOf("Subscribers", StringComparison.Ordinal);
            var publishersIndex = contents.IndexOf("Publishers", StringComparison.Ordinal);
            Assert.That(publishersIndex, Is.GreaterThan(0));
            Assert.That(subscribersIndex, Is.GreaterThan(0));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage1", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage2", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("No publishers registered", StringComparison.Ordinal), Is.GreaterThan(publishersIndex));
        }

        [Test]
        public void HandlesEmptyPublishers()
        {
            // arrange
            const string filename = "JustSaying.html";
            IInterrogationResponse interrogationResponse = new InterrogationResponse(
                new[] { new Subscriber(typeof(TestMessage1)), new Subscriber(typeof(TestMessage2)) },
                new Publisher[0]);

            // act
            _documentor.CreateIndexPage(filename, interrogationResponse);

            // assert
            Assert.That(File.Exists(filename), Is.True);
            var contents = File.ReadAllText(filename);
            var subscribersIndex = contents.IndexOf("Subscribers", StringComparison.Ordinal);
            var publishersIndex = contents.IndexOf("Publishers", StringComparison.Ordinal);
            Assert.That(publishersIndex, Is.GreaterThan(0));
            Assert.That(subscribersIndex, Is.GreaterThan(0));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage1", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("JustSaying.Messaging.UnitTests.Documentation.TestMessage2", StringComparison.Ordinal), Is.GreaterThan(subscribersIndex).And.LessThan(publishersIndex));
            Assert.That(contents.IndexOf("No publishers registered", StringComparison.Ordinal), Is.GreaterThan(publishersIndex));
        }
    }

    public class TestMessage1 : Message
    {
    }

    public class TestMessage2 : Message
    {
    }

    public class TestMessage3 : Message
    {
    }
}
