using MessageBroker.Contracts.Events;
using System.Threading.Tasks;

namespace MessageBroker.Infrastructure.Interfaces
{
	/// <summary>
	/// The message broker producer interface.
	/// </summary>
	public interface IProducer
	{
		/// <summary>
		/// Sends the provided event to the message broker in the provided topic.
		/// </summary>
		/// <param name="sentEvent">The event to be sent.</param>
		/// <param name="topic">The topic destination.</param>
		Task Send (IEvent sentEvent, string topic);
	}
}
