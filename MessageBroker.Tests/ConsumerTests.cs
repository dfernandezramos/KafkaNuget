using Confluent.Kafka;
using MessageBroker.Contracts;
using MessageBroker.Infrastructure;
using MessageBroker.Infrastructure.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MessageBroker.Tests
{
	public class ConsumerTests
	{
		Mock<IConsumerFactory> consumerFactory;
		Mock<IConsumer<Ignore, string>> kafkaConsumer;
		Mock<IServiceProvider> serviceProvider;
		Mock<ILogger<Consumer>> logger;
		CancellationTokenSource cancellationTokenSource;
		DummyConsumer consumer;

		public ConsumerTests ()
		{
			serviceProvider = new Mock<IServiceProvider> ();
			var serviceScope = new Mock<IServiceScope> ();
			serviceScope.Setup (x => x.ServiceProvider).Returns (serviceProvider.Object);
			var serviceScopeFactory = new Mock<IServiceScopeFactory> ();
			serviceScopeFactory.Setup (x => x.CreateScope ()).Returns (serviceScope.Object);
			serviceProvider.Setup (x => x.GetService (typeof (IServiceScopeFactory))).Returns (serviceScopeFactory.Object);

			kafkaConsumer = new Mock<IConsumer<Ignore, string>> ();
			consumerFactory = new Mock<IConsumerFactory> ();
			consumerFactory.Setup (pf => pf.GetConsumer ()).Returns (kafkaConsumer.Object);
			var config = GetConsumerConfiguration ();
			logger = new Mock<ILogger<Consumer>> ();
			consumer = new DummyConsumer (consumerFactory.Object, config, serviceProvider.Object, logger.Object);
			cancellationTokenSource = new CancellationTokenSource ();
		}

		[Fact]
		async Task Consume_EventMessageHandled_EventConsumed ()
		{
			// Arrange
			The_service_provider_will_return_an_event_handler<ValidEventHandler> ();
			The_consumer_will_retrieve_a_valid_message (Utils.VALID_EVENT_NAME);

			// Act
			await The_consumer_starts ();

			// Assert
			kafkaConsumer.Verify (kc => kc.Subscribe (It.IsAny<string> ()), Times.Once);
			kafkaConsumer.Verify (kc => kc.Consume (It.IsAny<CancellationToken> ()), Times.Once);
			kafkaConsumer.Verify (kc => kc.Commit (It.IsAny<ConsumeResult<Ignore, string>> ()), Times.Once);
		}

		[Fact]
		async Task Consume_InvalidMessage_ExceptionLogged ()
		{
			// Arrange
			The_consumer_will_retrieve_an_invalid_message ();

			// Act
			await The_consumer_starts ();

			// Assert
			kafkaConsumer.Verify (kc => kc.Subscribe (It.IsAny<string> ()), Times.Once);
			kafkaConsumer.Verify (kc => kc.Consume (It.IsAny<CancellationToken> ()), Times.Once);
			Then_an_error_has_been_logged<JsonSerializationException> ();
		}

		[Fact]
		async Task Consume_InvalidEventSent_ArgumentNullExceptionLogged ()
		{
			// Arrange
			The_consumer_will_retrieve_a_valid_message (null);

			// Act
			await The_consumer_starts ();

			// Assert
			kafkaConsumer.Verify (kc => kc.Subscribe (It.IsAny<string> ()), Times.Once);
			kafkaConsumer.Verify (kc => kc.Consume (It.IsAny<CancellationToken> ()), Times.Once);
			Then_an_error_has_been_logged<ArgumentNullException> ();
		}

		[Fact]
		async Task Consume_TheEventHandlerWontBeReturned_NullReferenceExceptionLogged ()
		{
			// Arrange
			The_consumer_will_retrieve_a_valid_message (Utils.VALID_EVENT_NAME);

			// Act
			await The_consumer_starts ();

			// Assert
			kafkaConsumer.Verify (kc => kc.Subscribe (It.IsAny<string> ()), Times.Once);
			kafkaConsumer.Verify (kc => kc.Consume (It.IsAny<CancellationToken> ()), Times.Once);
			Then_an_error_has_been_logged<KeyNotFoundException> ();
		}

		[Fact]
		async Task Consume_InvalidEventHandlerReturned_InvalidCastExceptionLogged ()
		{
			// Arrange
			The_service_provider_will_return_an_event_handler<InvalidEventHandler> ();
			The_consumer_will_retrieve_a_valid_message (Utils.INVALID_EVENT_NAME);

			// Act
			await The_consumer_starts ();

			// Assert
			kafkaConsumer.Verify (kc => kc.Subscribe (It.IsAny<string> ()), Times.Once);
			kafkaConsumer.Verify (kc => kc.Consume (It.IsAny<CancellationToken> ()), Times.Once);
			Then_an_error_has_been_logged<InvalidCastException> ();
		}

		async Task The_consumer_starts ()
		{
			await consumer.StartAsync (cancellationTokenSource.Token);
		}

		void The_consumer_will_retrieve_a_valid_message (string eventName)
		{
			var retrievedEvent = new ValidEvent { Username = "Jeremías" };

			var kMessage = new KafkaMessage {
				Name = eventName,
				EventData = JsonConvert.SerializeObject (retrievedEvent)
			};

			The_consumer_will_return_a_message (kMessage);
		}

		void The_consumer_will_retrieve_an_invalid_message ()
		{
			The_consumer_will_return_a_message ("invalid data");
		}

		void The_consumer_will_return_a_message (object message)
		{
			var result = new Message<Ignore, string> {
				Value = JsonConvert.SerializeObject (message)
			};

			SetupConsumeResult (new ConsumeResult<Ignore, string> {
				Message = result
			});
			kafkaConsumer.Setup (kc => kc.Commit (It.IsAny<ConsumeResult<Ignore, string>> ()))
						 .Callback (() => {
							 SetupConsumeResult (null);
							 consumer.StopAsync (cancellationTokenSource.Token).Wait ();
						 });
		}

		void The_service_provider_will_return_an_event_handler<T> () where T : new()
		{
			serviceProvider
				.Setup (x => x.GetService (typeof (T)))
				.Returns (new T ());
		}

		void Then_an_error_has_been_logged<T> () where T : Exception
		{
			logger.Verify (l => l.Log (LogLevel.Error, It.IsAny<EventId> (), It.IsAny<object> (), It.IsAny<T> (), It.IsAny<Func<object, Exception, string>> ()), Times.Once);
		}

		void SetupConsumeResult (ConsumeResult<Ignore, string> result)
		{
			kafkaConsumer.Setup (kc => kc.Consume (It.IsAny<CancellationToken> ())).Returns (result);
		}

		EventConsumerConfiguration GetConsumerConfiguration ()
		{
			var config = new EventConsumerConfiguration {
				Server = "kafkaserver",
				GroupId = "groupId",
				Topics = new List<string> { "NotificationsMicroservices" }
			};
			config.RegisterConsumer<ValidEvent, ValidEventHandler> ();
			config.Handlers[Utils.INVALID_EVENT_NAME] = typeof (InvalidEventHandler);

			return config;
		}
	}
}
