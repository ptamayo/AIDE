using Aide.Core.Cloud.Azure.SendGrid;
using Aide.Hangfire.Common.Messages;
using Aide.Hangfire.Jobs.Settings;
using Microsoft.Extensions.Logging;
using SendGrid.Helpers.Mail;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Aide.Hangfire.Jobs
{
	public class UserManagementJob
	{
		private readonly ILogger<UserManagementJob> _logger;
		private readonly ISendGridClientAdapter _sendGridClient;
		private readonly UserManagementJobConfig _configSettings;

		public UserManagementJob(ILogger<UserManagementJob> logger, ISendGridClientAdapter sendGridClientAdapter, UserManagementJobConfig configSettings)
		{
			_logger = logger ?? throw new ArgumentNullException(nameof(logger));
			_sendGridClient = sendGridClientAdapter ?? throw new ArgumentNullException(nameof(sendGridClientAdapter));
			_configSettings = configSettings ?? throw new ArgumentNullException(nameof(configSettings));
		}

		public async Task SendWelcomeEmailAsync(NewUserMessage arg)
		{
            if (!_configSettings.SendGridConfig.NewUserEmailEnabled)
            {
                _logger.LogWarning($"The setting for emailing the password to new users is OFF. The welcome message and password for the user {arg.EmailAddress} won't be emailed.");
                return;
            }

            try
			{
				var msg = _sendGridClient.NewSendGridMessage();
				msg.SetFrom(_configSettings.EmailFrom);
				var recipients = new List<EmailAddress>
				{
					new EmailAddress(arg.EmailAddress)
				};
				msg.AddTos(recipients);

				// Use a dynamic template that has been previously setup in SendGrid portal:
				msg.TemplateId = _configSettings.SendGridConfig.NewUserEmailTemplateId;
				msg.Personalizations[0].TemplateData = new
				{
					firstName = arg.FirstName,
					emailAddress = arg.EmailAddress,
					temporaryPsw = arg.TemporaryPsw,
					urlWeb = _configSettings.UrlWeb,
					emailForSupport = _configSettings.EmailForSupport
				};

				// If don't want to use a template the just prepare a raw message on the fly:
				//msg.SetSubject("Bienvenido al Administrador de Imágenes Express (AIDE)");
				//msg.AddContent(MimeType.Text, 
				//@$"
				//	Estimado {arg.firstName},\r\n\r\n
				//	En hora buena! Ya puedes ingresar al Administrador de Imágenes Express!\r\n
				//  Solo tienes que ir a <a href=\"{_configSettings.urlWeb}\">{_configSettings.urlWeb}</a> y usar tus credenciales a continuación:\r\n\r\n
				//	Usuario: {arg.emailAddress}\r\n
				//	Contraseña: {arg.temporaryPsw}\r\n\r\n
				//  Es muy recomendable que cambies tu contraseña al accesar el sistema por primera vez.\r\n
				//  Para cambiar tu contraseña solo tienes que dar click en el círculo con las iniciales de tu nombre que está localizado en la parte superior derecha de la pantalla (es el círculo que está coloreado).\r\n
				//  Después elije la opción \"Mi perfil\" y escribe 2 veces tu nueva contraseña en el formulario titulado \"Contraseña\". Ah! Y no olvides dar click en el botón \"Guardar\" para completar el cambio.\r\n
				//  Para cualquier duda o pregunta escríbenos al siguiente correo electrónico y con gusto te responderemos: {_configSettings.emailForSupport}\r\n\r\n
				//  Por el momento eso es todo! Que tengas un excelente día!
				//");
				//msg.AddContent(MimeType.Html, 
				//@$"
				//	Estimado {arg.firstName}<br><br>
				//	En hora buena! Ya puedes ingresar al Administrador de Imágenes Express!<br>
				//	Solo tienes que ir a <a href=\"{_configSettings.urlWeb}\">{_configSettings.urlWeb}</a> y usar tus credenciales a continuación:<br><br>
				//	Usuario: <b>{arg.emailAddress}</b><br>
				//	Contraseña: <b>{arg.temporaryPsw}</b><br><br>
				//  Es muy recomendable que cambies tu contraseña al accesar el sistema por primera vez.<br>
				//  Para cambiar tu contraseña solo tienes que dar click en el círculo con las iniciales de tu nombre que está localizado en la parte superior derecha de la pantalla (es el círculo que está coloreado).<br>
				//  Después elije la opción \"Mi perfil\" y escribe 2 veces tu nueva contraseña en el formulario titulado \"Contraseña\". Ah! Y no olvides dar click en el botón \"Guardar\" para completar el cambio.<br>
				//  Para cualquier duda o pregunta escríbenos al siguiente correo electrónico y con gusto te responderemos: {_configSettings.emailForSupport}<br><br>
				//  Por el momento eso es todo! Que tengas un excelente día!
				//");

				if (_configSettings.IsEmailServiceEnabled)
				{
					var response = await _sendGridClient.SendEmailAsync(msg).ConfigureAwait(false);

					if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
					{
						_logger.LogInformation($"Email successfully sent!");
					}
					else
					{
						_logger.LogWarning($"Somehow the email didn't go through. Status Code: {response.StatusCode}", response);
					}
				}
                else
                {
					_logger.LogInformation($"The user it's been created but the email notification is currently disabled.");
				}

				// End
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't send a new user welcome email due to an unhandled error.");
				throw;
			}
		}

		public async Task SendPswResetEmailAsync(ResetUserPswMessage arg)
		{
            if (!_configSettings.SendGridConfig.ResetUserPswEmailEnabled)
            {
                _logger.LogWarning($"The setting for emailing the new password to existing users is OFF. The new password for the user {arg.EmailAddress} won't be emailed.");
                return;
            }

            try
			{
				var msg = _sendGridClient.NewSendGridMessage();
				msg.SetFrom(_configSettings.EmailFrom);
				var recipients = new List<EmailAddress>
				{
					new EmailAddress(arg.EmailAddress)
				};
				msg.AddTos(recipients);

				// Use a dynamic template that has been previously setup in SendGrid portal:
				msg.TemplateId = _configSettings.SendGridConfig.ResetUserPswEmailTemplateId;
				msg.Personalizations[0].TemplateData = new
				{
					firstName = arg.FirstName,
					emailAddress = arg.EmailAddress,
					temporaryPsw = arg.TemporaryPsw,
					urlWeb = _configSettings.UrlWeb,
					emailForSupport = _configSettings.EmailForSupport
				};

				// If don't want to use a template the just prepare a raw message on the fly:
				//msg.SetSubject("AIDE - Confirmación de cambio de contraseña");
				//msg.AddContent(MimeType.Text, 
				//@$"
				//	Estimado {arg.firstName},\r\n\r\n
				//	Este correo es para confirmar que hemos cambiado tu contraseña para ingresar al sistema AIDE. Si tu no solicitaste este cambio favor de notificar de inmediato al área de sistemas.\r\n
				//  Para accesar al sistema sólo tienes que ir a <a href=\"{_configSettings.urlWeb}\">{_configSettings.urlWeb}</a> y usar las credenciales a continuación:\r\n\r\n
				//	Usuario: {arg.emailAddress}\r\n
				//	Contraseña: {arg.temporaryPsw}\r\n\r\n
				//  Es muy recomendable que cambies tu contraseña al accesar el sistema por primera vez.\r\n
				//  Para cambiar tu contraseña solo tienes que dar click en el círculo con las iniciales de tu nombre que está localizado en la parte superior derecha de la pantalla (es el círculo que está coloreado).\r\n
				//  Después elije la opción \"Mi perfil\" y escribe 2 veces tu nueva contraseña en el formulario titulado \"Contraseña\". Ah! Y no olvides dar click en el botón \"Guardar\" para completar el cambio.\r\n
				//  Para cualquier duda o pregunta escríbenos al siguiente correo electrónico y con gusto te responderemos: {_configSettings.emailForSupport}\r\n\r\n
				//  Por el momento eso es todo! Que tengas un excelente día!
				//");
				//msg.AddContent(MimeType.Html, 
				//@$"
				//	Estimado {arg.firstName}<br><br>
				//	Este correo es para confirmar que hemos cambiado tu contraseña para ingresar al sistema AIDE. Si tu no solicitaste este cambio favor de notificar de inmediato al área de sistemas.<br>
				//	Para accesar al sistema sólo tienes que ir a <a href=\"{_configSettings.urlWeb}\">{_configSettings.urlWeb}</a> y usar las credenciales a continuación:<br><br>
				//	Usuario: <b>{arg.emailAddress}</b><br>
				//	Contraseña: <b>{arg.temporaryPsw}</b><br><br>
				//  Es muy recomendable que cambies tu contraseña al accesar el sistema por primera vez.<br>
				//  Para cambiar tu contraseña solo tienes que dar click en el círculo con las iniciales de tu nombre que está localizado en la parte superior derecha de la pantalla (es el círculo que está coloreado).<br>
				//  Después elije la opción \"Mi perfil\" y escribe 2 veces tu nueva contraseña en el formulario titulado \"Contraseña\". Ah! Y no olvides dar click en el botón \"Guardar\" para completar el cambio.<br>
				//  Para cualquier duda o pregunta escríbenos al siguiente correo electrónico y con gusto te responderemos: {_configSettings.emailForSupport}<br><br>
				//  Por el momento eso es todo! Que tengas un excelente día!
				//");

				if (_configSettings.IsEmailServiceEnabled)
				{
					var response = await _sendGridClient.SendEmailAsync(msg).ConfigureAwait(false);

					if (response.StatusCode == System.Net.HttpStatusCode.Accepted)
					{
						_logger.LogInformation($"Email successfully sent!");
					}
					else
					{
						_logger.LogWarning($"Somehow the email didn't go through. Status Code: {response.StatusCode}", response);
					}
				}
                else
                {
					_logger.LogInformation($"The password it's been reset but the email notification is currently disabled.");
				}

				// End
			}
			catch (Exception ex)
			{
				_logger.LogError(ex, $"Couldn't send a new user welcome email due to an unhandled error.");
				throw;
			}
		}

		#region Local Classes

		public class UserManagementJobConfig
		{
			public string UrlWeb { get; set; }
            public bool IsEmailServiceEnabled { get; set; }
            public string EmailFrom { get; set; }
			public string EmailForSupport { get; set; }
			public SendGridConfig SendGridConfig { get; set; }
		}

		#endregion
	}
}
