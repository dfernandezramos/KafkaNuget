using MessageBroker.Contracts;
using MessageBroker.Contracts.Events;
using MessageBroker.Infrastructure;
using MessageBroker.Infrastructure.Interfaces;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace MessageBroker.Tests
{
	public class Utils
	{
		public const string VALID_EVENT_NAME = "Valid";
		public const string INVALID_EVENT_NAME = "Invalid";
	}

	public class ValidEventHandler : IServiceEventHandler
	{
		public async Task Handle (JObject jObject, CancellationToken cancellationToken)
		{
		}
	}

	public class InvalidEventHandler { }

	[Event (Utils.VALID_EVENT_NAME)]
	public class ValidEvent : IEvent
	{
		public string Username { get; set; }
	}

	public class EventWithoutAttribute : IEvent
	{

	}

	[Event (null)]
	public class EventWithoutName : IEvent
	{

	}

	public class DummyConsumer : Consumer
	{
		public DummyConsumer (IConsumerFactory consumerFactory, EventConsumerConfiguration options, IServiceProvider serviceProvider, ILogger<Consumer> logger)
			: base (consumerFactory, options, serviceProvider, logger) { }

		protected override async Task ProcessConsumeError (Exception ex, KafkaMessage kMessage)
		{
			await base.ProcessConsumeError (ex, kMessage);
			await StopAsync (new CancellationToken ());
		}
	}
}
