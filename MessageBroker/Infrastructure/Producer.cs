using MessageBroker.Contracts;
using MessageBroker.Contracts.Events;
using MessageBroker.Infrastructure.Factories;
using MessageBroker.Infrastructure.Interfaces;
using Confluent.Kafka;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace MessageBroker.Infrastructure
{
	/// <summary>
	/// The message broker producer class. It provides a method to send events to the message broker.
	/// </summary>
	public class Producer : IProducer
	{
		readonly IProducerFactory producerFactory;
		readonly JsonSerializerSettings jsonSettings;
		readonly ILogger<Producer> logger;

		public Producer (IOptions<EventProducerConfiguration> options, ILogger<Producer> logger) : this (new ProducerFactory (options.Value), logger)
		{
		}

		public Producer (IProducerFactory producerFactory, ILogger<Producer> logger)
		{
			this.producerFactory = producerFactory;
			this.logger = logger;
			jsonSettings = new JsonSerializerSettings {
				ContractResolver = new CamelCasePropertyNamesContractResolver ()
			};
		}

		/// <summary>
		/// Sends the specified event to the message broker on the specified topic.
		/// </summary>
		/// <param name="sentEvent">The event to be sent</param>
		/// <param name="topic">The destination topic</param>
		public async Task Send (IEvent sentEvent, string topic)
		{
			if (sentEvent == null) {
				throw new ArgumentNullException (nameof (sentEvent));
			}

			if (string.IsNullOrEmpty (topic)) {
				throw new ArgumentNullException (nameof (topic));
			}

			var message = GetMessage (sentEvent);

			try {
				await producerFactory.GetProducer ().ProduceAsync (topic, message);
				logger.LogInformation ($"{nameof (Producer)}: message sent on topic <{topic}>: ", sentEvent);
			} catch (ProduceException<Null, string> e) {
				logger.LogError (e.ToString ());
				throw new ArgumentException (e.Error.Reason);
			}
		}

		Message<Null, string> GetMessage (IEvent sentEvent)
		{
			var attribute = sentEvent.GetType ().GetCustomAttribute<EventAttribute> ();

			if (attribute == null) {
				throw new ArgumentException ($"{nameof (EventAttribute)} missing on {nameof (sentEvent)}");
			}

			if (string.IsNullOrEmpty (attribute.Name)) {
				throw new ArgumentNullException (
					$"{nameof (EventAttribute)}.Name missing on {nameof (sentEvent)}");
			}

			var kMessage = new KafkaMessage {
				Name = attribute.Name,
				EventData = JsonConvert.SerializeObject (sentEvent, jsonSettings)
			};

			var message = JsonConvert.SerializeObject (kMessage, jsonSettings);

			return new Message<Null, string> { Value = message };
		}
	}
}
