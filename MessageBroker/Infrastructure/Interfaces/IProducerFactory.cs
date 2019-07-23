using Confluent.Kafka;

namespace MessageBroker.Infrastructure.Interfaces
{
	/// <summary>
	/// The message broker producer factory interface.
	/// </summary>
	public interface IProducerFactory
	{
		/// <summary>
		/// Gets the message broker producer instance.
		/// </summary>
		/// <returns>The message broker producer instance.</returns>
		IProducer<Null, string> GetProducer ();
	}
}
