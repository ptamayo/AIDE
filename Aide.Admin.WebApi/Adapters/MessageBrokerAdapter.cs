using Aide.Core.Cloud.Azure.ServiceBus;
using System;
using System.Threading.Tasks;
using Aide.Hangfire.Common.Messages;

namespace Aide.Admin.WebApi.Adapters
{
	public interface IMessageBrokerAdapter
	{
		Task SendNewUserMessage(string firstName, string emailAddress, string temporaryPsw);
		Task SendNewUserMessage(NewUserMessage message);
		Task SendResetUserPswMessage(string firstName, string emailAddress, string temporaryPsw);
		Task SendResetUserPswMessage(ResetUserPswMessage message);
	}

	public class MessageBrokerAdapter : IMessageBrokerAdapter
	{
		private readonly IBusService _bus;
		private readonly AppSettings _appSettings;
		private const string QueueName1 = "plk_new_user_welcome_email_queue";
		private const string QueueName2 = "plk_reset_user_psw_email_queue";

		public MessageBrokerAdapter(IBusService bus, AppSettings appSettings)
		{
			_bus = bus ?? throw new ArgumentNullException(nameof(bus));
			_appSettings = appSettings ?? throw new ArgumentNullException(nameof(appSettings));
		}

		public async Task SendNewUserMessage(string firstName, string emailAddress, string temporaryPsw)
		{
			if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentNullException(nameof(firstName));
			if (string.IsNullOrWhiteSpace(emailAddress)) throw new ArgumentNullException(nameof(emailAddress));
			if (string.IsNullOrWhiteSpace(temporaryPsw)) throw new ArgumentNullException(nameof(temporaryPsw));

			var message = new NewUserMessage
			{
				FirstName = firstName,
				EmailAddress = emailAddress,
				TemporaryPsw = temporaryPsw
			};

			await SendNewUserMessage(message).ConfigureAwait(false);
		}

		public async Task SendNewUserMessage(NewUserMessage message)
		{
			// Send message to hangfire
			var queueUrl = _appSettings.ServiceBusConfig.Queue[QueueName1];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<NewUserMessage>(message).ConfigureAwait(false);
		}

		public async Task SendResetUserPswMessage(string firstName, string emailAddress, string temporaryPsw)
		{
			if (string.IsNullOrWhiteSpace(firstName)) throw new ArgumentNullException(nameof(firstName));
			if (string.IsNullOrWhiteSpace(emailAddress)) throw new ArgumentNullException(nameof(emailAddress));
			if (string.IsNullOrWhiteSpace(temporaryPsw)) throw new ArgumentNullException(nameof(temporaryPsw));

			var message = new ResetUserPswMessage
			{
				FirstName = firstName,
				EmailAddress = emailAddress,
				TemporaryPsw = temporaryPsw
			};

			await SendResetUserPswMessage(message).ConfigureAwait(false);
		}

		public async Task SendResetUserPswMessage(ResetUserPswMessage message)
		{
			// Send message to hangfire
			var queueUrl = _appSettings.ServiceBusConfig.Queue[QueueName2];
			var endpoint = await _bus.GetSendEndpoint(queueUrl).ConfigureAwait(false);
			await endpoint.Send<ResetUserPswMessage>(message).ConfigureAwait(false);
		}
	}
}
