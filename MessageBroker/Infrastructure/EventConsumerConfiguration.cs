using System;
using System.Collections.Generic;
using System.Reflection;
using MessageBroker.Contracts;
using MessageBroker.Contracts.Events;
using MessageBroker.Infrastructure.Interfaces;

namespace MessageBroker.Infrastructure
{
	/// <summary>
	/// This class contains the message broker consumer configuration.
	/// </summary>
	public class EventConsumerConfiguration
	{
		/// <summary>
		/// A dictionary that contains the event handlers our microservice will handle when the consumer consumes an event.
		/// </summary>
		public IDictionary<string, Type> Handlers { get; set; } = new Dictionary<string, Type> ();

		/// <summary>
		/// The list of topics the consumer will be subscribed to.
		/// </summary>
		public List<string> Topics { get; set; }

		/// <summary>
		/// The group id which identifies a group of consumers.
		/// </summary>
		public string GroupId { get; set; }

		/// <summary>
		/// The message broker server where the consumer will be connected.
		/// </summary>
		public string Server { get; set; }

		/// <summary>
		/// Registers an event handler in order to consume the specified events and handle them.
		/// </summary>
		/// <typeparam name="TEvent">The event type to be consumed.</typeparam>
		/// <typeparam name="TEventHandler">The event handler type related to the event type to be consumed.</typeparam>
		public EventConsumerConfiguration RegisterConsumer<TEvent, TEventHandler> ()
			where TEvent : IEvent
			where TEventHandler : IServiceEventHandler
		{
			var eventName = typeof (TEvent).GetCustomAttribute<EventAttribute> ()?.Name;
			if (string.IsNullOrEmpty (eventName)) {
				throw new InvalidOperationException ($"{nameof (EventAttribute)} missing on {typeof (TEvent).Name}");
			}
			Handlers[eventName] = typeof (TEventHandler);
			return this;
		}
	}
}
