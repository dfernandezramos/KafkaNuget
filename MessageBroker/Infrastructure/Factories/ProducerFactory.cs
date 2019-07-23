using MessageBroker.Infrastructure.Interfaces;
using Confluent.Kafka;

namespace MessageBroker.Infrastructure.Factories
{
	/// <summary>
	/// This class creates and provides an instance of IProducer<Null, string>.
	/// </summary>
	public class ProducerFactory : IProducerFactory
	{
		readonly IProducer<Null, string> producer;

		public ProducerFactory (EventProducerConfiguration config)
		{
			ProducerConfig producerConf = new ProducerConfig {
				BootstrapServers = config.BootstrapServers,
				MessageSendMaxRetries = config.MessageSendMaxRetries
			};
			producer = new ProducerBuilder<Null, string> (producerConf).Build ();
		}

		public virtual IProducer<Null, string> GetProducer ()
		{
			return producer;
		}
	}
}
