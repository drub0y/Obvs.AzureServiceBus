using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    internal sealed class NamespaceManagerWrapper : INamespaceManager
    {
        private readonly NamespaceManager _namespaceManager;

        public NamespaceManagerWrapper(NamespaceManager namespaceManager)
        {
            _namespaceManager = namespaceManager;
        }

        public Uri Address
        {
            get
            {
                return _namespaceManager.Address;
            }
        }
        public NamespaceManagerSettings Settings
        {
            get
            {
                return _namespaceManager.Settings;
            }
        }

        public bool QueueExists(string path)
        {
            return _namespaceManager.QueueExists(path);
        }

        public void CreateQueue(string path)
        {
            _namespaceManager.CreateQueue(path);
        }

        public void DeleteQueue(string path)
        {
            _namespaceManager.DeleteQueue(path);
        }

        public bool TopicExists(string path)
        {
            return _namespaceManager.TopicExists(path);
        }

        public void CreateTopic(string path)
        {
            _namespaceManager.CreateTopic(path);
        }

        public void DeleteTopic(string path)
        {
            _namespaceManager.DeleteTopic(path);
        }

        public bool SubscriptionExists(string topicPath, string subscriptionName)
        {
            return _namespaceManager.SubscriptionExists(topicPath, subscriptionName);
        }

        public void CreateSubscription(string topicPath, string subscriptionName)
        {
            _namespaceManager.CreateSubscription(topicPath, subscriptionName);
        }

        public void DeleteSubscription(string topicPath, string subscriptionName)
        {
            _namespaceManager.DeleteSubscription(topicPath, subscriptionName);
        }
    }
}
