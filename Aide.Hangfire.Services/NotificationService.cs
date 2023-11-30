using Aide.Core.CustomExceptions;
using Aide.Hangfire.Domain.Objects;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using Newtonsoft.Json;
using System.Threading.Tasks;

namespace Aide.Hangfire.Services
{
    public interface INotificationService
	{
		Task<Notification> SendNotification(Notification dto);
	}

	public class NotificationService : INotificationService
	{
		#region Properties

		private readonly HttpClient _httpClient;
		private readonly NotificationServiceConfig _serviceConfig;
		private const string _defaultMediaType = "application/json";
        

        #endregion

        #region Constructor

        public NotificationService(HttpClient httpClient, NotificationServiceConfig serviceConfig)
		{
			_httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
			_serviceConfig = serviceConfig ?? throw new ArgumentNullException(nameof(serviceConfig));
		}

		#endregion

		#region Methods

		public async Task<Notification> SendNotification(Notification notification)
		{
			if (notification == null) throw new ArgumentNullException(nameof(notification));
			var payload = new StringContent(JsonConvert.SerializeObject(notification), Encoding.UTF8, _defaultMediaType);
			var endpoint = _serviceConfig.Endpoints["Hub"];
			var response = await _httpClient.PostAsync(endpoint, payload).ConfigureAwait(false);
			response.EnsureSuccessStatusCode();
			if (!response.IsSuccessStatusCode)
			{
				throw new EndpointRequestCustomizedException($"Got {response.StatusCode} from endpoint {response.RequestMessage.RequestUri}");
			}
			if (response.StatusCode == HttpStatusCode.NotFound)
			{
				throw new NonExistingRecordCustomizedException();
			}
			var responseContent = await response.Content.ReadAsStringAsync().ConfigureAwait(false);
			return JsonConvert.DeserializeObject<Notification>(responseContent);
		}

		#endregion

		#region Local classes

		public class NotificationServiceConfig
		{
			public Dictionary<string, string> Endpoints { get; set; }
		}

		#endregion
	}
}
