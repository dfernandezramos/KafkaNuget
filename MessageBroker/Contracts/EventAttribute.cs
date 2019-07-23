using System;

namespace MessageBroker.Contracts
{
	/// <summary>
	/// All events sent to the broker will have an event name. Provide it through this attribute.
	/// </summary>
	[AttributeUsage (AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
	public class EventAttribute : Attribute
	{
		/// <summary>
		/// The event name.
		/// </summary>
		public string Name { get; }

		public EventAttribute (string name)
		{
			Name = name;
		}
	}
}
