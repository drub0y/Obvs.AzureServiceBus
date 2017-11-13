# Obvs.AzureServiceBus [![Join the chat at https://gitter.im/drub0y/Obvs.AzureServiceBus](https://badges.gitter.im/Join%20Chat.svg)](https://gitter.im/drub0y/Obvs.AzureServiceBus?utm_source=badge&utm_medium=badge&utm_campaign=pr-badge&utm_content=badge)

An Azure ServiceBus transport support for [the Obvs framework](https://github.com/christopherread/Obvs).

---

## Features

### Azure Service Bus Supported Messaging Entities

The following Azure ServiceBus messaging entities are currently supported by this transport:

 * Queues
 * Topics
 * Subscriptions
 
### Peek-Lock Message Control
As of Obvs 3.x, the library offers no out-of-the-box control over peek-lock style messages like those you might receive from Azure Service Bus. 
Therefore this library offers a small API-subset that is designed to be agnostic of Azure Service Bus at the surface level, yet gives you the 
full control you would expect over peek-lock style messages as if you were working with Azure Service Bus's `BrokeredMessage` class directly.

**Please note:** you *must* opt-in to `PeekLock` mode as the default is `ReceiveAndDelete`. This is to stay consistent with the Azure Service Bus API itself.

[You can read more on this subject here in the Wiki.](https://github.com/drub0y/Obvs.AzureServiceBus/wiki/Peek-Lock-Message-Processing-Pattern-Support)

---   

## Basic Scenario Examples

The following examples show how you would configure the service bus for a variety of basic scenarios.

### Sending commands from an Azure Service Bus Queue

Shows how to configure the bus to send commands to an Azure Service Bus Queue.

```C#
// Configure the bus with a queue
var serviceBusClient = ServiceBus<MyMessage, MyCommand, MyEvent, MyRequest, MyResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("My Service Bus")
                .WithConnectionString(ConfigurationManager.AppSettings["MyServiceBusConnectionString"])
                .UsingQueueFor<MyCommand>("my-commands")
                .SerializedAsJson()
                .AsClient()
                .CreateServiceBusClient();                

// Send a command via the configured client
await serviceBusClient.SendAsync(new MyFancyCommand());
```
### Sending events to an Azure Service Bus Topic

```C#
// Configure the bus with a topic
var serviceBus = ServiceBus<MyMessage, MyCommand, MyEvent, MyRequest, MyResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("My Service Bus")
                .WithConnectionString(ConfigurationManager.AppSettings["MyServiceBusConnectionString"])
                .UsingTopicFor<MyEvent>("my-events")
                .SerializedAsJson()
                .AsServer()
                .CreateServiceBus();
                
// Publish an event via the configured service bus
await serviceBus.PublishAsync(new MySpecialEvent());
```

### Receiving events from an Azure Service Bus Subscription using Peek-Lock mode

```C#
// Configure the bus with a specific subscription
var serviceBusClient = ServiceBus<MyMessage, MyCommand, MyEvent, MyRequest, MyResponse>.Configure()
                .WithAzureServiceBusEndpoint()
                .Named("My Service Bus")
                .WithConnectionString(ConfigurationManager.AppSettings["MyServiceBusConnectionString"])
                .UsingSubscriptionFor<MyEvent>("my-events", "my-subscription", MessageReceiveMode.PeekLock)
                .SerializedAsJson()
                .AsClient()
                .CreateServiceBusClient();
                
// Process all events that come in via my subscription
serviceBusClient.Events.Subscribe(myEvent =>
{
    // ... handle the incoming event with whatever domain specific logic here ...
    
    // Signal completion of the peek lock
    myEvent.GetPeekLockControl().CompleteAsync().Wait();     
});
```