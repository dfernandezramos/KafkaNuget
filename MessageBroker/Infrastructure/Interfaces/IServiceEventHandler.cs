using Newtonsoft.Json.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Infrastructure.Interfaces
{
	/// <summary>
	/// This is the service event handler interface. Each event handler related to the consumed events from the broker should implement this.
	/// </summary>
	public interface IServiceEventHandler
	{
		/// <summary>
		/// Executes a handle method in order to do some action with the consumed event from the message broker.
		/// </summary>
		/// <param name="jObject">The event serialized as a jObject</param>
		/// <param name="cancellationToken">The cancellation token</param>
		Task Handle (JObject jObject, CancellationToken cancellationToken);
	}
}
