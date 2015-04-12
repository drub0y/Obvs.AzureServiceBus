using System;
using System.Threading.Tasks;
using Microsoft.ServiceBus;
using Microsoft.ServiceBus.Messaging;

namespace Obvs.AzureServiceBus.Infrastructure
{
    public interface INamespaceManager
    {
        Uri Address
        {
            get;
        }
        NamespaceManagerSettings Settings
        {
            get;
        }

        bool QueueExists(string path);
        void CreateQueue(string path);
        void DeleteQueue(string path);
        bool TopicExists(string path);
        void DeleteTopic(string path);
        void CreateTopic(string path);
        bool SubscriptionExists(string topicPath, string subscriptionName);
        void CreateSubscription(string topicPath, string subscriptionName);
        void DeleteSubscription(string topicPath, string subscriptionName);
    }
}
