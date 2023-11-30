using Hangfire;
using Hangfire.Dashboard;
using Hangfire.MySql;
using Aide.Core.Cloud.Azure.SendGrid;
using Aide.Core.Cloud.Azure.ServiceBus;
using Aide.Core.Adapters;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Core.Media.MagickDotNet;
using Aide.Core.WebApi.Interfaces;
using Aide.Core.WebApi.Services;
using Aide.Hangfire.Jobs;
using Aide.Hangfire.Services;
using Aide.Reports.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Serilog;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Transactions;
using Azure.Messaging.ServiceBus;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;

namespace Aide.Hangfire.Worker
{
    public class Startup
	{
		public Startup(IConfiguration configuration, IHostEnvironment env)
		{
            var builder = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

            if (!string.IsNullOrWhiteSpace(env.EnvironmentName))
            {
                Console.WriteLine($"Loading appsettings.{env.EnvironmentName}.json");
                builder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
            }

            Configuration = builder.Build();

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(configuration)
				.CreateLogger();
		}

        public string AIDE_DEV_NAME
		{
			get
			{
				return Environment.GetEnvironmentVariable("AIDE_DEV_NAME");
            }
		}

        public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		public void ConfigureServices(IServiceCollection services)
		{
			var appSettings = new AppSettings();
			Configuration.GetSection("AppSettings").Bind(appSettings);
			services.AddTransient<AppSettings>(ti => appSettings);

			// The following line enables Application Insights telemetry collection.
			//services.AddApplicationInsightsTelemetry();

			// Singletons
			services.AddSingleton<ICacheService>(si => new DotNetMemoryCacheService(EnumTimeExpression.CoordinatedUniversalTime, appSettings.CacheConfig.Enabled));
			services.AddSingleton<IAppLicencingAdapter>(si => new AppLicencingAdapter(appSettings.License));

			// DB Contexts (non-transactional dbs only)
			services.AddTransient<InsuranceReportsDbContext>(ti => new InsuranceReportsDbContext(Configuration.GetConnectionString("InsuranceReportsDb")));

			// Adapters
			services.AddTransient<IFileSystemAdapter, FileSystemAdapter>();
			services.AddTransient<ICsvAdapter, CsvAdapter>();
			services.AddTransient<ITimeZoneAdapter, TimeZoneAdapter>();
			services.AddTransient<CollageAdapter.CollageAdapterConfig>(ti => new CollageAdapter.CollageAdapterConfig
			{
				LimitMemoryPercentage = appSettings.MediaEngineConfig.LimitMemoryPercentage,
				CollagePdfDensity = appSettings.MediaEngineConfig.CollagePdfDensity,
				CollageImageQuality = appSettings.MediaEngineConfig.CollageImageQuality,
				CollageImageWidth = appSettings.MediaEngineConfig.CollageImageWidth,
				GhostscriptDirectory = appSettings.MediaEngineConfig.GhostscriptDirectory
			});
			services.AddTransient<ICollageAdapter, CollageAdapter>();
			services.AddTransient<IZipArchiveAdapter, ZipArchiveAdapter>();
			services.AddTransient<IJwtSecurityTokenHandlerAdapter, JwtSecurityTokenHandlerAdapter>();
			services.AddTransient<ISendGridClientAdapter, SendGridClientAdapter>(ti => new SendGridClientAdapter(appSettings.SendGridConfig.ApiKey));

			// Local services
			services.AddTransient<IAppLicenseService, AppLicenseService<Startup>>();

			// External services
			// Enable IHttpClientFactory and configure external endpoints
			// Further details here: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1
			var userServiceConfig = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "UserService");
			services.AddHttpClient<IUserService, UserService>(c =>
			{
				c.BaseAddress = new Uri(userServiceConfig.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<UserService.UserServiceConfig>(ti => new UserService.UserServiceConfig
			{
				Endpoints = userServiceConfig.Endpoints
				//Notice this service does not require credentials
			});
			var claimServiceConfig = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "ClaimService");
			services.AddHttpClient<IClaimService, ClaimService>(c =>
			{
				c.BaseAddress = new Uri(claimServiceConfig.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<ClaimService.ClaimServiceConfig>(ti => new ClaimService.ClaimServiceConfig
			{
				Endpoints = claimServiceConfig.Endpoints,
				Credentials = new ServiceCredentialsConfig
				{
					Username = appSettings.ExternalServicesConfig.Credentials.Username,
					HashedPsw = appSettings.ExternalServicesConfig.Credentials.HashedPsw
				}
			});

			var claimDocumentServiceConfig = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "ClaimDocumentService");
			services.AddHttpClient<IClaimDocumentService, ClaimDocumentService>(c =>
			{
				c.BaseAddress = new Uri(claimDocumentServiceConfig.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<ClaimDocumentService.ClaimDocumentServiceConfig>(ti => new ClaimDocumentService.ClaimDocumentServiceConfig
			{
				Endpoints = claimDocumentServiceConfig.Endpoints,
				Credentials = new ServiceCredentialsConfig
				{
					Username = appSettings.ExternalServicesConfig.Credentials.Username,
					HashedPsw = appSettings.ExternalServicesConfig.Credentials.HashedPsw
				}
			});

			var claimProbatoryDocumentServiceConfig = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "ClaimProbatoryDocumentService");
			services.AddHttpClient<IClaimProbatoryDocumentService, ClaimProbatoryDocumentService>(c =>
			{
				c.BaseAddress = new Uri(claimProbatoryDocumentServiceConfig.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<ClaimProbatoryDocumentService.ClaimProbatoryDocumentServiceConfig>(ti => new ClaimProbatoryDocumentService.ClaimProbatoryDocumentServiceConfig
			{
				Endpoints = claimProbatoryDocumentServiceConfig.Endpoints,
				Credentials = new ServiceCredentialsConfig
				{
					Username = appSettings.ExternalServicesConfig.Credentials.Username,
					HashedPsw = appSettings.ExternalServicesConfig.Credentials.HashedPsw
				}
			});

			var insuranceCollageServiceConfig = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "InsuranceCollageService");
			services.AddHttpClient<IInsuranceCollageService, InsuranceCollageService>(c =>
			{
				c.BaseAddress = new Uri(insuranceCollageServiceConfig.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<InsuranceCollageService.InsuranceCollageServiceConfig>(ti => new InsuranceCollageService.InsuranceCollageServiceConfig
			{
				Endpoints = insuranceCollageServiceConfig.Endpoints,
				Credentials = new ServiceCredentialsConfig
				{
					Username = appSettings.ExternalServicesConfig.Credentials.Username,
					HashedPsw = appSettings.ExternalServicesConfig.Credentials.HashedPsw
				}
			});

			var insuranceProbatoryDocumentService = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "InsuranceProbatoryDocumentService");
			services.AddHttpClient<IInsuranceProbatoryDocumentService, InsuranceProbatoryDocumentService>(c =>
			{
				c.BaseAddress = new Uri(insuranceProbatoryDocumentService.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentServiceConfig>(ti => new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentServiceConfig
			{
				Endpoints = insuranceProbatoryDocumentService.Endpoints,
				Credentials = new ServiceCredentialsConfig
				{
					Username = appSettings.ExternalServicesConfig.Credentials.Username,
					HashedPsw = appSettings.ExternalServicesConfig.Credentials.HashedPsw
				}
			});

			var notificationServiceConfig = appSettings.ExternalServicesConfig.Services.FirstOrDefault(x => x.Service == "NotificationService");
			services.AddHttpClient<INotificationService, NotificationService>(c =>
			{
				c.BaseAddress = new Uri(notificationServiceConfig.BaseAddress);
			});
			//.AddHeaderPropagation();
			services.AddTransient<NotificationService.NotificationServiceConfig>(ti => new NotificationService.NotificationServiceConfig
			{
				Endpoints = notificationServiceConfig.Endpoints
			});

			// Jobs
			services.AddTransient<UserManagementJob.UserManagementJobConfig>(ti => new UserManagementJob.UserManagementJobConfig
			{
				UrlWeb = appSettings.UrlWeb,
				IsEmailServiceEnabled = appSettings.EmailServiceConfig.IsEnabled,
				EmailFrom = appSettings.EmailServiceConfig.EmailAddresses.EmailFrom,
				EmailForSupport = appSettings.EmailServiceConfig.EmailAddresses.EmailForSupport,
				SendGridConfig = new Aide.Hangfire.Jobs.Settings.SendGridConfig
				{
					ApiKey = appSettings.SendGridConfig.ApiKey,
					NewUserEmailTemplateId = appSettings.SendGridConfig.NewUserEmailTemplateId,
					NewUserEmailEnabled = appSettings.SendGridConfig.NewUserEmailEnabled,
					ResetUserPswEmailTemplateId = appSettings.SendGridConfig.ResetUserPswEmailTemplateId,
					ResetUserPswEmailEnabled = appSettings.SendGridConfig.ResetUserPswEmailEnabled
				}
			});

			services.AddTransient<ClaimManagementJob.ClaimManagementJobConfig>(ti => new ClaimManagementJob.ClaimManagementJobConfig
			{
				UrlWeb = appSettings.UrlWeb,
				IsEmailServiceEnabled = appSettings.EmailServiceConfig.IsEnabled,
				EmailFrom = appSettings.EmailServiceConfig.EmailAddresses.EmailFrom,
				EmailForSupport = appSettings.EmailServiceConfig.EmailAddresses.EmailForSupport,
				SendGridConfig = new Jobs.Settings.SendGridConfig
				{
					ApiKey = appSettings.SendGridConfig.ApiKey,
					NewUserEmailTemplateId = appSettings.SendGridConfig.NewUserEmailTemplateId,
					NewUserEmailEnabled = appSettings.SendGridConfig.NewUserEmailEnabled,
					ResetUserPswEmailTemplateId = appSettings.SendGridConfig.ResetUserPswEmailTemplateId,
					ResetUserPswEmailEnabled = appSettings.SendGridConfig.ResetUserPswEmailEnabled
				}
			});

			services.AddTransient<ClaimReceiptJob.ClaimReceiptJobConfig>(ti => new ClaimReceiptJob.ClaimReceiptJobConfig
			{
				IsEmailServiceEnabled = appSettings.EmailServiceConfig.IsEnabled,
				EmailFrom = appSettings.EmailServiceConfig.EmailAddresses.EmailFrom,
				PilkingtonTpaEmail = appSettings.EmailServiceConfig.EmailAddresses.PilkingtonTpaEmail,
				SendGridConfig = new Aide.Hangfire.Jobs.Settings.SendGridConfig
				{
					ApiKey = appSettings.SendGridConfig.ApiKey,
					ClaimReceiptEmailTemplateId = appSettings.SendGridConfig.ClaimReceiptEmailTemplateId,
					ClaimReceiptEmailEnabled = appSettings.SendGridConfig.ClaimReceiptEmailEnabled
				},
				PdfFilesConfig = new ClaimReceiptJob.PdfFilesConfig
				{
					PathToSave = appSettings.PdfFilesConfig.PathToSave,
					BaseUrl = appSettings.PdfFilesConfig.BaseUrl
				},
				ThirdPartySystemNotifications =
					appSettings.ServiceBusConfig.ThirdPartySystemNotifications
					.ToDictionary(
						x => x.Key,
						x => new ClaimReceiptJob.ThirdPartySystemNotificationQueue
						{
							Enabled = x.Value.Enabled,
							Queue = x.Value.Queue
						})
			});

			services.AddTransient<ClaimProbatoryDocumentsJob.ConfigSettings>(ti => new ClaimProbatoryDocumentsJob.ConfigSettings
			{
				PdfFilesConfig = new ClaimProbatoryDocumentsJob.PdfFilesConfig
				{
					PathToSave = appSettings.PdfFilesConfig.PathToSave,
					BaseUrl = appSettings.PdfFilesConfig.BaseUrl
				},
				ZipFilesConfig = new ClaimProbatoryDocumentsJob.ZipFilesConfig
				{
					PathToSave = appSettings.ZipFilesConfig.PathToSave,
					BaseUrl = appSettings.ZipFilesConfig.BaseUrl
				},
				ReceiptDocumentConfig = new ClaimProbatoryDocumentsJob.ReceiptDocumentConfig
				{
					DocumentTypeId = appSettings.ReceiptDocumentConfig.DocumentTypeId,
					GroupId = appSettings.ReceiptDocumentConfig.GroupId,
					SortPriority = appSettings.ReceiptDocumentConfig.SortPriority
				},
				TemporaryFilesConfig = new ClaimProbatoryDocumentsJob.TemporaryFilesConfig
				{
					PathToSave = appSettings.TemporaryFilesConfig.PathToSave,
					BaseUrl = appSettings.TemporaryFilesConfig.BaseUrl
				},
				MediaEngineConfig = new ClaimProbatoryDocumentsJob.MediaEngineConfig
				{
					ResizeImageWidth = appSettings.MediaEngineConfig.ResizeImageWidth
				}
			});

			services.AddTransient<ReportJob.ConfigSettings>(ti => new ReportJob.ConfigSettings
			{
				PageSize = appSettings.ReportJobConfig.DefaultPageSize,
				TemporaryFilesConfig = new ReportJob.TemporaryFilesConfig
				{
					PathToSave = appSettings.TemporaryFilesConfig.PathToSave,
					BaseUrl = appSettings.TemporaryFilesConfig.BaseUrl
				}
			});

            services.AddTransient<ZipFilesManagementJob.ZipFilesManagementJobConfig>(ti => new ZipFilesManagementJob.ZipFilesManagementJobConfig
            {
                Credentials = new ServiceCredentialsConfig
				{
					Username = appSettings.ExternalServicesConfig.Credentials.Username,
					HashedPsw = appSettings.ExternalServicesConfig.Credentials.HashedPsw
				}
            });

			// Add Hangfire services.
			services.AddHangfire(configuration => configuration
				.SetDataCompatibilityLevel(CompatibilityLevel.Version_170)
				.UseSimpleAssemblyNameTypeSerializer()
				.UseRecommendedSerializerSettings()
				.UseStorage(new MySqlStorage(Configuration.GetConnectionString("HangfireConnection"), new MySqlStorageOptions
				{
					TransactionIsolationLevel = IsolationLevel.ReadCommitted,
					QueuePollInterval = TimeSpan.FromSeconds(15),
					JobExpirationCheckInterval = TimeSpan.FromHours(1),
					CountersAggregateInterval = TimeSpan.FromMinutes(5),
					PrepareSchemaIfNecessary = true,
					DashboardJobListLimit = 50000,
					TransactionTimeout = TimeSpan.FromMinutes(5),
					TablesPrefix = string.IsNullOrWhiteSpace(AIDE_DEV_NAME) ? "hangfire_" : $"{AIDE_DEV_NAME.ToLower()}_",
//#pragma warning disable CS0618 // Type or member is obsolete
//					InvisibilityTimeout = TimeSpan.FromMinutes(5) // It's currently depreated but still valid for MySQL
//#pragma warning restore CS0618 // Type or member is obsolete
				}))
				.UseSerilogLogProvider()
				);

			if (appSettings.JobServerConfig.WorkerCount > 0)
			{
				services.AddHangfireServer(options =>
				{
					/*
                    * Concurrency level control (Setting 2 of 2) *
                    Hangfire uses its own fixed worker thread pool to consume queued jobs.
                    Default worker count is set to Environment.ProcessorCount * 5.
                    This number is optimized both for CPU-intensive and I/O intensive tasks.
                    If you experience excessive waits or context switches, you can configure the amount of workers manually.
                    */
					options.WorkerCount = appSettings.JobServerConfig.WorkerCount;
				});
            }
			else
			{
				services.AddHangfireServer();
			}

			//// IMPORTANT: Do NOT use the code commented below because it resulted troublematic. It need further investigation of IHostedService
			//// Add the processing server as IHostedService
			//services.AddHangfireServer(options =>
			//{
			//	/*
			//	 * Concurrency level control (Setting 1 of 2) *
			//	Hangfire uses its own fixed worker thread pool to consume queued jobs.
			//	Default worker count is set to Environment.ProcessorCount * 5.
			//	This number is optimized both for CPU-intensive and I/O intensive tasks.
			//	If you experience excessive waits or context switches, you can configure the amount of workers manually.
			//	*/
			//	options.WorkerCount = 5;
			//});

			// Add Azure ServiceBus service
			//var receiveEndpoint = new Dictionary<string, Type>
			//{
			//	{
			//		"plk_queuex",
			//		typeof(TestMessageConsumer)
			//	},
			//	{
			//		"plk_email_claim_receipt_queue",
			//		typeof(EmailClaimReceiptMessageConsumer)
			//	}
			//};
			//var json = Newtonsoft.Json.JsonConvert.SerializeObject(receiveEndpoint);
			var receiveEndpoint = new Dictionary<string, Type>();
			foreach (var x in appSettings.ServiceBusConfig.QueueConsumer)
			{
                var queueName = x.Key;
                if (!string.IsNullOrWhiteSpace(AIDE_DEV_NAME))
                {
                    queueName = $"{AIDE_DEV_NAME.ToLower()}_{queueName}";
                    Console.WriteLine($"Adding customized queue name of {queueName}");
                }
                var type = Type.GetType(x.Value);
				receiveEndpoint.Add(queueName, type);
			}

			services.AddSingleton<IBusService>(si => new BusService(si.GetRequiredService<ILogger<BusService>>(),
																	appSettings.ServiceBusConfig.ConnectionString,
																	ServiceBusTransportType.AmqpWebSockets, // To bypass blocked TCP port 5671 and instead use port 443.
																	receiveEndpoint));

			// Add framework services.
			services.AddRazorPages();

			// Set the global settings for serialization.
			JsonConvert.DefaultSettings = () => new JsonSerializerSettings
			{
				ContractResolver = new CamelCasePropertyNamesContractResolver() // Camel case always.
            };
        }

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IBackgroundJobClient backgroundJobs, IHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IAppLicenseService appLicense)
		{
			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}
			else
			{
				app.UseExceptionHandler("/Error");
			}

			app.UseStaticFiles();

			// The tweak below is not working, need revisit.
			// Make sure the index page is hit in order for the service bus to start.
			// Initiate azure service bus (tweak)
			var bus = app.ApplicationServices.GetService<IBusService>();

			loggerFactory.AddSerilog();

			app.UseHangfireDashboard("/hangfire", new DashboardOptions
			{
				//AppPath = "http://localhost:5000",
				Authorization = new[] { new MyAuthorizationFilter() }
			});

            //app.UseHangfireDashboard();

            var appSettings = new AppSettings();
            Configuration.GetSection("AppSettings").Bind(appSettings);

            // Trigger Jobs here in the start-up
            //backgroundJobs.Enqueue(() => Console.WriteLine("Hello world from Hangfire!"));

            // Schedule Jobs
            //backgroundJobs.Schedule<OrderManagementJob>(x => x.RemoveStaledOrders(), TimeSpan.FromDays(1));

            // Recurring Jobs
            var recurringRemoveStaledOrdersJobConfig = appSettings.RecurringJobsConfig.FirstOrDefault(x => x.JobName == "RemoveStaledOrders");
			var timeZoneInfo = TimeZoneInfo.FindSystemTimeZoneById(recurringRemoveStaledOrdersJobConfig.TimeZone);
			var thresholdInHours = Convert.ToDouble(recurringRemoveStaledOrdersJobConfig.Args["ThresholdInHours"]);
#pragma warning disable CS0618 // Type or member is obsolete
            RecurringJob.AddOrUpdate<OrderManagementJob>(x => x.RemoveStaledOrders(thresholdInHours), recurringRemoveStaledOrdersJobConfig.CronExpression, timeZoneInfo);
#pragma warning restore CS0618 // Type or member is obsolete

            app.UseRouting();

			app.UseAuthorization();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapRazorPages();
			});

            //// License validation
            //// TODO: Apply ENV VAR to validate OS.
            //appLifetime.ApplicationStarted.Register(() => appLicense.OnAppStartUp());
        }
    }

	public class MyAuthorizationFilter : IDashboardAuthorizationFilter
	{
		public bool Authorize(DashboardContext context)
		{
			//var httpContext = context.GetHttpContext();

			// Allow all authenticated users to see the Dashboard (potentially dangerous).
			//return httpContext.User.Identity.IsAuthenticated;
			return true;
		}
	}
}
