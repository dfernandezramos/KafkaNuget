using MessageBroker.Infrastructure.Interfaces;
using Confluent.Kafka;

namespace MessageBroker.Infrastructure.Factories
{
	/// <summary>
	/// This class creates and provides an instance of IConsumer<Ignore, string>.
	/// </summary>
	public class ConsumerFactory : IConsumerFactory
	{
		readonly IConsumer<Ignore, string> consumer;

		public ConsumerFactory (EventConsumerConfiguration config)
		{
			var consumerConfig = new ConsumerConfig {
				GroupId = config.GroupId,
				BootstrapServers = config.Server
			};

			consumer = new ConsumerBuilder<Ignore, string> (consumerConfig).Build ();
		}

		public virtual IConsumer<Ignore, string> GetConsumer ()
		{
			return consumer;
		}
	}
}
