
namespace MessageBroker.Infrastructure
{
	/// <summary>
	/// The message broker producer configuration class.
	/// </summary>
	public class EventProducerConfiguration
	{
		/// <summary>
		/// The message broker server where the producer will be connected.
		/// </summary>
		public string BootstrapServers { get; set; }

		/// <summary>
		/// This property will cause the producer to automatically retry a failed send request.
		/// This property specifies the number of retries when such failures occur.
		/// </summary>
		public int? MessageSendMaxRetries { get; set; }
	}
}
