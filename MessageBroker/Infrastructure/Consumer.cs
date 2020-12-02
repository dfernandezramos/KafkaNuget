using Confluent.Kafka;
using MessageBroker.Contracts;
using MessageBroker.Infrastructure.Factories;
using MessageBroker.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Infrastructure
{
	/// <summary>
	/// The message broker consumer class. On ExecuteAsync will run threads for consuming from each topic.
	/// </summary>
	public class Consumer : BackgroundService
	{
		readonly IConsumerFactory consumerFactory;
		readonly IServiceProvider serviceProvider;
		readonly EventConsumerConfiguration options;
		readonly ILogger<Consumer> logger;
		CancellationTokenSource cancellationTokenSource;

		public Consumer (IOptions<EventConsumerConfiguration> options, IServiceProvider serviceProvider, ILogger<Consumer> logger)
			: this (new ConsumerFactory (options.Value), options.Value, serviceProvider, logger)
		{
		}

		public Consumer (IConsumerFactory consumerFactory, EventConsumerConfiguration options, IServiceProvider serviceProvider, ILogger<Consumer> logger)
		{
			this.options = options;
			this.serviceProvider = serviceProvider;
			this.consumerFactory = consumerFactory;
			this.logger = logger;
		}

		protected override async Task ExecuteAsync (CancellationToken cancellationToken)
		{
			await Task.Yield ();
			var topics = options.Topics.Distinct ();
			consumerFactory.GetConsumer ().Subscribe (topics);
			await Consume (cancellationTokenSource.Token);
		}

		public override Task StartAsync (CancellationToken cancellationToken)
		{
			cancellationTokenSource = CancellationTokenSource.CreateLinkedTokenSource (cancellationToken);
			return base.StartAsync (cancellationToken);
		}

		public override Task StopAsync (CancellationToken cancellationToken)
		{
			cancellationTokenSource.Cancel ();
			return base.StopAsync (cancellationToken);
		}

		protected virtual KafkaMessage ParseAsKafkaMessage (Message<Ignore, string> message)
		{
			return JsonConvert.DeserializeObject<KafkaMessage> (message.Value);
		}

		protected virtual async Task ProcessConsumeError (Exception ex, KafkaMessage kMessage)
		{
			if (kMessage != null) {
				ex.Data.Add ("IntegrationMessage", kMessage);
			}

			logger.LogError (ex, "Error consuming message from the broker");
		}

		async Task Consume (CancellationToken cancellationToken)
		{
			while (!cancellationToken.IsCancellationRequested) {
				var kMessage = default (KafkaMessage);
				try {
					var message = consumerFactory.GetConsumer ().Consume (cancellationToken);

					if (message == null) {
						continue;
					}

					logger.LogInformation ($" received message on topic <{message.Topic}>", message.Value);

					// 1: The received message is deserialized to an IntegrationEvent which contains the meta-data for handling the event.
					kMessage = ParseAsKafkaMessage (message.Message);

					// 2: The meta-data is used to retrieve the registered message handler for this kind of message.
					var handlerType = GetHandlerType (kMessage);
					if (handlerType == null) {
						continue;
					}

					using (var scope = serviceProvider.CreateScope ()) {
						// 3: The registered message handler information is used to resolve an instance of this message handler.
						var handler = GetHandler (scope, handlerType);
						logger.LogInformation ($"{nameof (Consumer)}: received message on topic <{message.Topic}>", kMessage);

						// 4: The message handler is called to process the actual event
						await HandleMessage (handler, kMessage, cancellationToken);

						// 5: The message is committed, if no exception occurred during handling of the message
						consumerFactory.GetConsumer ().Commit (message);
					}
				} catch (Exception ex) {
					await ProcessConsumeError (ex, kMessage);
				}
			}
		}

		async Task HandleMessage (IServiceEventHandler handler, KafkaMessage integrationMessage, CancellationToken cancellationToken)
		{
			await handler.Handle (JObject.Parse (integrationMessage.EventData), cancellationToken);
		}

		Type GetHandlerType (KafkaMessage message)
		{
			if (message.Name == null) {
				throw new ArgumentNullException ($"{nameof (Consumer)} exception: event Name is missing");
			}

			return options.Handlers.TryGetValue (message.Name, out var handlerType) ? handlerType : null;
		}

		IServiceEventHandler GetHandler (IServiceScope scope, Type handlerType)
		{
			var handler = scope.ServiceProvider.GetService (handlerType);

			if (handler == null) {
				throw new KeyNotFoundException ($"{nameof (Consumer)} exception: no handler found for type <{handlerType}>");
			}

			if (handler is IServiceEventHandler eventHandler) {
				return eventHandler;
			}

			throw new InvalidCastException ($"{nameof (Consumer)} exception: handler <{handlerType}> not of type <{typeof (IServiceEventHandler)}>");
		}
	}
}
