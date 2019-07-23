using MessageBroker.Contracts.Events;
using MessageBroker.Infrastructure;
using MessageBroker.Infrastructure.Interfaces;
using Confluent.Kafka;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Tests
{
	public class ProducerTests
	{
		const string TOPIC = "any_topic";

		Mock<IProducerFactory> producerFactory;
		Mock<IProducer<Null, string>> kafkaProducer;
		Producer producer;
		ILogger<Producer> logger;

		void InitializeProducer ()
		{
			kafkaProducer = new Mock<IProducer<Null, string>> ();
			producerFactory = new Mock<IProducerFactory> ();
			producerFactory.Setup (pf => pf.GetProducer ()).Returns (kafkaProducer.Object);
			logger = Mock.Of<ILogger<Producer>> ();
			producer = new Producer (producerFactory.Object, logger);
		}

		[Fact]
		public void SendEvent_NoEventProvided_ArgumentNullExceptionThrown ()
		{
			// Arrange
			InitializeProducer ();

			// Act & Assert
			When_the_producer_sends_a_message_produces_an_exception<ArgumentNullException> (null, TOPIC);
		}

		[Fact]
		public void SendEvent_NoTopicProvided_ArgumentNullExceptionThrown ()
		{
			// Arrange
			InitializeProducer ();

			// Act & Assert
			When_the_producer_sends_a_message_produces_an_exception<ArgumentNullException> (new ValidEvent (), null);
		}

		[Fact]
		public async Task SendEvent_EventAndTopicProvided_EventSent ()
		{
			// Arrange
			InitializeProducer ();
			var sentEvent = new ValidEvent { Username = "Jeremías" };

			// Act
			await producer.Send (sentEvent, TOPIC);

			// Assert
			kafkaProducer.Verify (kp => kp.ProduceAsync (TOPIC, It.IsAny<Message<Null, string>> ()), Times.Once);
		}

		[Fact]
		public void SendEvent_ProductionFails_ArgumentExceptionThrown ()
		{
			// Arrange
			InitializeProducer ();
			var sentEvent = new ValidEvent { Username = "Jeremías" };
			The_producer_will_fail ();

			// Act & Assert
			When_the_producer_sends_a_message_produces_an_exception<ArgumentException> (sentEvent, TOPIC);
		}

		[Fact]
		public void SendEvent_EventWithoutEventAttribute_ArgumentExceptionThrown ()
		{
			// Arrange
			InitializeProducer ();

			// Act & Assert
			When_the_producer_sends_a_message_produces_an_exception<ArgumentException> (new EventWithoutAttribute (), TOPIC);
		}

		[Fact]
		public void SendEvent_EventWithoutEventAttributeName_ArgumentNullExceptionThrown ()
		{
			// Arrange
			InitializeProducer ();

			// Act & Assert
			When_the_producer_sends_a_message_produces_an_exception<ArgumentNullException> (new EventWithoutName (), TOPIC);
		}

		void The_producer_will_fail ()
		{
			kafkaProducer.Setup (p => p.ProduceAsync (It.IsAny<string> (), It.IsAny<Message<Null, string>> ()))
						 .ThrowsAsync (new ProduceException<Null, string> (new Error (ErrorCode.BrokerNotAvailable),
								  new DeliveryResult<Null, string> ()));
		}

		void When_the_producer_sends_a_message_produces_an_exception<T> (IEvent sentEvent, string topic) where T : Exception
		{
			Assert.ThrowsAsync<T> (async () => await producer.Send (sentEvent, topic));
		}
	}
}
