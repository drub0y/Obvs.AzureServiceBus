using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;
using Moq;
using Obvs.AzureServiceBus.Configuration;
using Obvs.AzureServiceBus.Infrastructure;
using Obvs.Configuration;
using Obvs.MessageProperties;
using Obvs.Serialization;
using Obvs.Types;
using Xunit;

namespace Obvs.AzureServiceBus.Tests
{
    public class MessagingEntityVerifierFacts
    {
        private readonly Mock<INamespaceManager> _mockNamespaceManager;

        public MessagingEntityVerifierFacts()
        {
            _mockNamespaceManager = new Mock<INamespaceManager>();
            _mockNamespaceManager.Setup(nsm => nsm.QueueExists(It.IsAny<string>()))
                .Returns(true);
            _mockNamespaceManager.Setup(nsm => nsm.TopicExists(It.IsAny<string>()))
                .Returns(true);
            _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists(It.IsAny<string>(), It.IsAny<string>()))
                .Returns(true);
        }

        public class ExistingMessageTypeMessagingEntitiesFacts : MessagingEntityVerifierFacts
        {
            [Fact]
            public void VerifyExistingMessagingEntityThatDoesNotExistShouldThrow()
            {
                _mockNamespaceManager.Setup(nsm => nsm.QueueExists("commands"))
                    .Returns(false);

                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                Action action = () => messagingEntityVerifier.EnsureMessagingEntitiesExist(new [] { new MessageTypeMessagingEntityMappingDetails(typeof(ICommand), "commands", MessagingEntityType.Queue, MessagingEntityCreationOptions.VerifyAlreadyExists) });

                var exceptionAssertion = action.ShouldThrow<MessagingEntityDoesNotAlreadyExistException>();

                exceptionAssertion.And.Path.Should().Be("commands");
                exceptionAssertion.And.MessagingEntityType.Should().Be(MessagingEntityType.Queue);
            }

            [Fact]
            public void UseExistingMessagingEntityShouldNotTryToCreateTheMessagingEntity()
            {
                _mockNamespaceManager.Setup(nsm => nsm.QueueExists("commands"))
                    .Returns(true);

                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                messagingEntityVerifier.EnsureMessagingEntitiesExist(new[] { new MessageTypeMessagingEntityMappingDetails(typeof(ICommand), "commands", MessagingEntityType.Queue, MessagingEntityCreationOptions.CreateIfDoesntExist) });

                _mockNamespaceManager.Verify(nsm => nsm.QueueExists("commands"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.CreateQueue("commands"), Times.Never());
            }
        }

        public class TemporaryMessagingEntityFacts : MessagingEntityVerifierFacts
        {
            [Fact]
            public void UseTemporaryMessagingEntityThatAlreadyExistsWithoutSpecifyingCanDeleteIfAlreadyExistsShouldThrow()
            {
                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                Action action = () => messagingEntityVerifier.EnsureMessagingEntitiesExist(new[] { new MessageTypeMessagingEntityMappingDetails(typeof(ICommand), "commands", MessagingEntityType.Queue, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary) });

                var exceptionAssertion = action.ShouldThrow<Obvs.AzureServiceBus.Configuration.MessagingEntityAlreadyExistsException>();

                exceptionAssertion.And.Path.Should().Be("commands");
                exceptionAssertion.And.MessagingEntityType.Should().Be(MessagingEntityType.Queue);
            }

            [Fact]
            public void UseTemporaryMessagingEntityThatAlreadyExiststSpecifyingRecreateOptionShouldRecreate()
            {
                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                messagingEntityVerifier.EnsureMessagingEntitiesExist(new[] { new MessageTypeMessagingEntityMappingDetails(typeof(ICommand), "commands", MessagingEntityType.Queue, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary) });

                _mockNamespaceManager.Verify(nsm => nsm.DeleteQueue("commands"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.CreateQueue("commands"), Times.Once());
            }

            [Fact]
            public void UseTemporarySubscriptionForTopicThatAlreadyExistsShouldCreateSubscription()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(true);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(false);

                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                messagingEntityVerifier.EnsureMessagingEntitiesExist(new[] { new MessageTypeMessagingEntityMappingDetails(typeof(IEvent), "events/subscriptions/test-subscription", MessagingEntityType.Subscription, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary) });

                _mockNamespaceManager.Verify(nsm => nsm.TopicExists("events"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.CreateSubscription("events", "test-subscription"), Times.Once());
            }

            [Fact]
            public void UseTemporarySubscriptionForTopicThatDoesntAlreadyExistThrows()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(false);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(false);

                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                Action action = () => messagingEntityVerifier.EnsureMessagingEntitiesExist(new[] { new MessageTypeMessagingEntityMappingDetails(typeof(IEvent), "events/subscriptions/test-subscription", MessagingEntityType.Subscription, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary) });

                var exceptionAssertion = action.ShouldThrow<MessagingEntityDoesNotAlreadyExistException>();

                exceptionAssertion.And.Path.Should().Be("events");
                exceptionAssertion.And.MessagingEntityType.Should().Be(MessagingEntityType.Topic);
            }

            [Fact]
            public void UseTemporarySubscriptionForTemporaryTopicShouldCreateTopicAndSubscription()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(false);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(false);

                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                messagingEntityVerifier.EnsureMessagingEntitiesExist(new[]
                {
                    new MessageTypeMessagingEntityMappingDetails(typeof(IEvent), "events", MessagingEntityType.Topic, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary),
                    new MessageTypeMessagingEntityMappingDetails(typeof(IEvent), "events/subscriptions/test-subscription", MessagingEntityType.Subscription, MessagingEntityCreationOptions.CreateIfDoesntExist | MessagingEntityCreationOptions.CreateAsTemporary)
                });

                _mockNamespaceManager.Verify(nsm => nsm.CreateTopic("events"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.CreateSubscription("events", "test-subscription"), Times.Once());
            }

            [Fact]
            public void UseTemporarySubscriptionThatAlreadyExistsShouldRecreateSubscription()
            {
                _mockNamespaceManager.Setup(nsm => nsm.TopicExists("events"))
                    .Returns(true);

                _mockNamespaceManager.Setup(nsm => nsm.SubscriptionExists("events", "test-subscription"))
                    .Returns(true);

                MessagingEntityVerifier messagingEntityVerifier = new MessagingEntityVerifier(_mockNamespaceManager.Object);

                messagingEntityVerifier.EnsureMessagingEntitiesExist(new[] { new MessageTypeMessagingEntityMappingDetails(typeof(IEvent), "events/subscriptions/test-subscription", MessagingEntityType.Subscription, MessagingEntityCreationOptions.CreateAsTemporary | MessagingEntityCreationOptions.RecreateExistingTemporary) });

                _mockNamespaceManager.Verify(nsm => nsm.DeleteSubscription("events", "test-subscription"), Times.Once());
                _mockNamespaceManager.Verify(nsm => nsm.CreateSubscription("events", "test-subscription"), Times.Once());
            }
        }
    }
}
