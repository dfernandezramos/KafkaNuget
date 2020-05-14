# Message Broker

This repository contains a message broker abstraction using Kafka to send events between microservices. Use the nuget package for it.

## Nuget usage
### Consumer

* In order to handle the events in the broker you will need to create an event handler class for each event. That class needs to inherit from **IServiceEventHandler** and implement its **Handle** method.

* In addition you will need to implement the event to be handled (notice that this event is a contract with the microservice that produces it and both classes need to have the same properties). This event needs to inherit from **IEvent** and have the **EventAttribute** with an event name.


In the project Startup class you also need to implement two new methods: **ConfigureMessageBroker** and **ConfigureEventHandlers**. Both need to get the **IServicesCollection** object by parameter. You need to call them in the **ConfigureServices** method.

* In the ConfigureEventHandlers you will need to register each event handler you have created. For example: `services.AddTransient<UserRegisteredEventHandler> ();`

* In the ConfigureMessageBroker you will need to configure the consumer this way:

```
services.Configure<EventConsumerConfiguration> (Configuration.GetSection ("Consumer"));
services.PostConfigure<EventConsumerConfiguration> (options => {
    options.RegisterConsumer<UserRegisteredEvent, UserRegisteredEventHandler> ();
});
services.AddSingleton<IHostedService, Consumer> ();
```

We also have added this section to our **AppSettings.json** file:

```
"Consumer": {
    "Server": "<MESSAGE_BROKER_SERVER_NAME>", <-- i.e. "mbserver"
    "GroupId": "<CONSUMER_GROUP_ID>", <-- i.e. "notifications-service"
    "Topics": [
      "<TOPIC_NAME>" <-- i.e. "NotificationsMicroservices"
    ]
}
```

This is an example of the Startup.cs class configuring a Consumer:

```
    public void ConfigureServices(IServiceCollection services)
    {
        ...

        ConfigureMessageBroker (services);
        ConfigureEventHandlers (services);

        ...
    }

    void ConfigureMessageBroker (IServiceCollection services)
    {
        services.Configure<EventConsumerConfiguration> (Configuration.GetSection ("MessageBrokerConsumer"));
        services.PostConfigure<EventConsumerConfiguration> (options => {
            options.RegisterConsumer<UserRegisteredEvent, UserRegisteredEventHandler> ();
        });
        services.AddSingleton<IHostedService, Consumer> ();
    }

    void ConfigureEventHandlers (IServiceCollection services)
    {
        services.AddTransient<UserRegisteredEventHandler> ();
    }
```


### Producer

In the project Startup class you will need to configure the producer by adding a new method **ConfigureMessageBroker** and calling it from the **ConfigureServices** method. You will need to pass the **IServicesCollection** object by parameter.

* In the **ConfigureMessageBroker** you need to add the following code:

```
services.Configure<EventProducerConfiguration> (Configuration.GetSection ("Producer"));
services.AddTransient<IProducer, Producer> ();
```

We also have added this section to our ***AppSettings.json** file:

```
"Producer": {
    "BootstrapServers": "<MESSAGE_BROKER_SERVER_NAME>", <-- i.e. "mbserver",
    "MessageSendMaxRetries": 2
}
```


* Then wherever you want to use the producer use dependency injection in the constructor to get the **IProducer** object and use it like this:
(We are following the previous example in the consumer for sending the UserRegisteredEvent to the NotificationsMicroservices topic. Remember to create the `UserRegisteredEvent.cs` class in this microservice too)

```
var @event = new UserRegisteredEvent {
    ConfirmationUrl = $"/auth/confirm?token={userConfirmToken.ConfirmToken}",
    Username = user.NickName,
    Email = user.Email
};
await messageBrokerProducer.Send (@event, "NotificationsMicroservices");
```

This is an example of the Startup.cs class configuring a Producer:

```
    public void ConfigureServices(IServiceCollection services)
    {
        ...

        ConfigureMessageBroker (services);

        ...
    }

    void ConfigureMessageBroker (IServiceCollection services)
    {
        services.Configure<EventProducerConfiguration> (Configuration.GetSection ("MessageBrokerProducer"));
        services.AddTransient<IProducer, Producer> ();
    }
```


### Local usage

In order to use this between microservices in local/debug mode you will need to run the following docker-compose file and set the "Server" and "BootstrapServers" as `localhost:9092` in the **AppSettings.json** file:

```
version: '3.4'

services:
  kafkaserver:
    image: "spotify/kafka:latest"
    ports:
      - 2181:2181
      - 9092:9092
    environment:
      ADVERTISED_HOST: localhost
      ADVERTISED_PORT: 9092
```

**Extra tip**: You can use `Kafka Tool` (http://www.kafkatool.com/) to check what is going on with your kafka local server.