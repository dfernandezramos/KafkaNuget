using Confluent.Kafka;

namespace MessageBroker.Infrastructure.Interfaces
{
	/// <summary>
	/// The message broker consumer factory interface.
	/// </summary>
	public interface IConsumerFactory
	{
		/// <summary>
		/// Gets the message broker consumer instance.
		/// </summary>
		/// <returns>The message broker consumer instance.</returns>
		IConsumer<Ignore, string> GetConsumer ();
	}
}
