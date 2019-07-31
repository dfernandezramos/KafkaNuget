namespace MessageBroker.Contracts
{
	/// <summary>
	/// Kafka message contract. All messages consumed from a broker will have an event name and its event data.
	/// </summary>
	public class KafkaMessage
	{
		/// <summary>
		/// The event name.
		/// </summary>
		public string Name { get; set; }

		/// <summary>
		/// The event data.
		/// </summary>
		public string EventData { get; set; }
	}
}
