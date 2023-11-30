using System;
using System.Linq;
using Aide.Core.Adapters;
using Aide.Core.Data;
using Aide.Core.Interfaces;
using Aide.Claims.Services;
using Aide.Claims.Models;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Aide.Core.Cloud.Azure.ServiceBus;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Serilog;
using Aide.Claims.WebApi.Adapters;
using Aide.Core.WebApi.Interfaces;
using Aide.Core.WebApi.Services;
using Microsoft.AspNetCore.StaticFiles;
using Azure.Messaging.ServiceBus;

namespace Aide.Claims.WebApi
{
    public class Startup
	{
		readonly string _myAllowSpecificOrigins = "_myAllowSpecificOrigins";

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

			// The following line enables Application Insights telemetry collection.
			services.AddApplicationInsightsTelemetry();

			// Enable CORS Step 1 - begin
			if (appSettings.CORSConfig != null && appSettings.CORSConfig.Origins.Any())
			{
				services.AddCors(options =>
				{
					options.AddPolicy(_myAllowSpecificOrigins,
					builder =>
					{
						builder.WithOrigins(appSettings.CORSConfig.Origins.ToArray())
						.AllowAnyHeader()
						.AllowAnyMethod()
						.AllowCredentials(); // 4/17: Recently added. Need verify if this enables local app to access the API
				});
				});
			}
            // Enable CORS Step 1 - end

            // Here you'll find all different options:
            // https://www.strathweb.com/2020/02/asp-net-core-mvc-3-x-addmvc-addmvccore-addcontrollers-and-other-bootstrapping-approaches/
            // IMPORTANT: .NET 6 moved from Newtonsoft.Json to System.Text.Json
            // See links below for further details:
            // https://github.com/dotnet/aspnetcore/issues/17811
            // https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-3.1&tabs=visual-studio#jsonnet-support
            services.AddControllers().AddNewtonsoftJson();

			// Configure JWT Authentication Step 1 - begin
			var key = Encoding.ASCII.GetBytes(appSettings.AuthenticationConfig.SymmetricSecurityKey);
			services.AddAuthentication(x =>
			{
				x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
				x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
			})
			.AddJwtBearer(x =>
			{
				x.RequireHttpsMetadata = false;
				x.SaveToken = true;
				x.TokenValidationParameters = new TokenValidationParameters
				{
					ValidateIssuerSigningKey = true,
					IssuerSigningKey = new SymmetricSecurityKey(key),
					ValidateIssuer = false,
					ValidateAudience = false
				};
			});
			// Configure JWT Authentication Step 1 - end

			// Configure Header propagation middleware Step 1 - begin
			// Further details here: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1#header-propagation-middleware
			services.AddHttpClient().AddHeaderPropagation(); // "MyForwardingClient" ???
			services.AddHeaderPropagation(options =>
			{
				options.Headers.Add("Authorization");
				options.Headers.Add("Content-Type");
			});
			// Configure Header propagation middleware Step 1 - end

			// Configure Dependency Injection
			ConfigureDependencies(services);
		}

		public void ConfigureDependencies(IServiceCollection services)
		{
			var appSettings = new AppSettings();
			Configuration.GetSection("AppSettings").Bind(appSettings);

            if (!string.IsNullOrWhiteSpace(AIDE_DEV_NAME))
            {
                foreach (var key in appSettings.ServiceBusConfig.Queue.Keys)
                {
                    var queueName = appSettings.ServiceBusConfig.Queue[key].Replace(key, $"{AIDE_DEV_NAME.ToLower()}_{key}");
                    Console.WriteLine($"Adding customized queue name of {queueName.Split('/')?.LastOrDefault()}");
                    appSettings.ServiceBusConfig.Queue[key] = queueName;
                }
            }

            services.AddTransient<AppSettings>(ti => appSettings);

			// Database context
			services.AddTransient<AideDbContext>(ti => new AideDbContext(Configuration.GetConnectionString("DbAideClaims")));

			// Singletons
			services.AddSingleton<ICacheService>(si => new DotNetMemoryCacheService(EnumTimeExpression.CoordinatedUniversalTime, appSettings.CacheConfig.Enabled));

            services.AddSingleton<IBusService>(si => new BusService(si.GetRequiredService<ILogger<BusService>>(),
                                                                    appSettings.ServiceBusConfig.ConnectionString,
                                                                    ServiceBusTransportType.AmqpWebSockets // To bypass blocked TCP port 5671 and instead use port 443.
                                                                    ));

            services.AddSingleton<IAppLicencingAdapter>(si => new AppLicencingAdapter(appSettings.License));

			// Adapters
			services.AddTransient<IMessageBrokerAdapter, MessageBrokerAdapter>();
			services.AddTransient<IFileSystemAdapter, FileSystemAdapter>();
			services.AddTransient<IContentTypeProvider, FileExtensionContentTypeProvider>();

			// Local services
			services.AddTransient<IAppLicenseService, AppLicenseService<Startup>>();
			services.AddTransient<IClaimTypeService, ClaimTypeService>();
			services.AddTransient<ClaimService.ClaimServiceConfig>(ti => new ClaimService.ClaimServiceConfig
			{
				DepositSlipConfig = new ClaimService.DepositSlipConfig
				{
					StoreProbatoryDocument = new ClaimService.ProbatoryDocumentConfig
					{
						ProbatoryDocumentId = appSettings.DepositSlipConfig.StoreProbatoryDocument.ProbatoryDocumentId,
						GroupId = appSettings.DepositSlipConfig.StoreProbatoryDocument.GroupId,
						SortPriority = appSettings.DepositSlipConfig.StoreProbatoryDocument.SortPriority
					},
					TPAProbatoryDocument = new ClaimService.ProbatoryDocumentConfig
					{
						ProbatoryDocumentId = appSettings.DepositSlipConfig.TPAProbatoryDocument.ProbatoryDocumentId,
						GroupId = appSettings.DepositSlipConfig.TPAProbatoryDocument.GroupId,
						SortPriority = appSettings.DepositSlipConfig.TPAProbatoryDocument.SortPriority
					}
				},
				StaleOrdersRemovalConfig = new ClaimService.StaleOrdersRemovalConfig
				{
                    SourceExclusions = appSettings.StaleOrdersRemovalConfig.SourceExclusions
                }
			});
			services.AddTransient<IClaimService, ClaimService>();
			services.AddTransient<IClaimProbatoryDocumentService, ClaimProbatoryDocumentService>();
			services.AddTransient<IClaimProbatoryDocumentMediaService, ClaimProbatoryDocumentMediaService>();
			services.AddTransient<IClaimSignatureService, ClaimSignatureService>();
			services.AddTransient<IMediaService, MediaService>();
			services.AddTransient<IDocumentTypeService, DocumentTypeService>();
			services.AddTransient<IClaimDocumentTypeService, ClaimDocumentTypeService>();
			services.AddTransient<IDocumentService, DocumentService>();
			services.AddTransient<IClaimDocumentService, ClaimDocumentService>();

			// External services
			// Enable IHttpClientFactory and configure external endpoints
			// Further details here: https://docs.microsoft.com/en-us/aspnet/core/fundamentals/http-requests?view=aspnetcore-3.1
			var userServiceConfig = appSettings.ExternalServicesConfig.FirstOrDefault(x => x.Service == "UserService");
			services.AddHttpClient<IUserService, UserService>(c =>
			{
				c.BaseAddress = new Uri(userServiceConfig.BaseAddress);
			})
			.AddHeaderPropagation();
			services.AddTransient<UserService.UserServiceConfig>(ti => new UserService.UserServiceConfig
			{
				Endpoints = userServiceConfig.Endpoints
			});

			var storeServiceConfig = appSettings.ExternalServicesConfig.FirstOrDefault(x => x.Service == "StoreService");
			services.AddHttpClient<IStoreService, StoreService>(c =>
			{
				c.BaseAddress = new Uri(storeServiceConfig.BaseAddress);
			})
			.AddHeaderPropagation();
			services.AddTransient<StoreService.StoreServiceConfig>(ti => new StoreService.StoreServiceConfig
			{
				Endpoints = storeServiceConfig.Endpoints
			});

			var insuranceCompanyServiceConfig = appSettings.ExternalServicesConfig.FirstOrDefault(x => x.Service == "InsuranceCompanyService");
			services.AddHttpClient<IInsuranceCompanyService, InsuranceCompanyService>(c =>
			{
				c.BaseAddress = new Uri(insuranceCompanyServiceConfig.BaseAddress);
			})
			.AddHeaderPropagation();
			services.AddTransient<InsuranceCompanyService.InsuranceCompanyServiceConfig>(ti => new InsuranceCompanyService.InsuranceCompanyServiceConfig
			{
				Endpoints = insuranceCompanyServiceConfig.Endpoints
			});

			var probatoryDocumentServiceConfig = appSettings.ExternalServicesConfig.FirstOrDefault(x => x.Service == "ProbatoryDocumentService");
			services.AddHttpClient<IProbatoryDocumentService, ProbatoryDocumentService>(c =>
			{
				c.BaseAddress = new Uri(probatoryDocumentServiceConfig.BaseAddress);
			})
			.AddHeaderPropagation();
			services.AddTransient<ProbatoryDocumentService.ProbatoryDocumentServiceConfig>(ti => new ProbatoryDocumentService.ProbatoryDocumentServiceConfig
			{
				Endpoints = probatoryDocumentServiceConfig.Endpoints
			});

			var insuranceProbatoryDocumentServiceConfig = appSettings.ExternalServicesConfig.FirstOrDefault(x => x.Service == "InsuranceProbatoryDocumentService");
			services.AddHttpClient<IInsuranceProbatoryDocumentService, InsuranceProbatoryDocumentService>(c =>
			{
				c.BaseAddress = new Uri(insuranceProbatoryDocumentServiceConfig.BaseAddress);
			})
			.AddHeaderPropagation();
			services.AddTransient<InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentServiceConfig>(ti => new InsuranceProbatoryDocumentService.InsuranceProbatoryDocumentServiceConfig
			{
				Endpoints = insuranceProbatoryDocumentServiceConfig.Endpoints
			});
		}

		// This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
		public void Configure(IApplicationBuilder app, IWebHostEnvironment env, ILoggerFactory loggerFactory, IHostApplicationLifetime appLifetime, IAppLicenseService appLicense)
		{
			var appSettings = new AppSettings();
			Configuration.GetSection("AppSettings").Bind(appSettings);

			if (env.IsDevelopment())
			{
				app.UseDeveloperExceptionPage();
			}

			loggerFactory.AddSerilog();

			app.UseHttpsRedirection();

			// Configure Header propagation middleware Step 2 - begin
			app.UseHeaderPropagation();
			// Configure Header propagation middleware Step 2 - end

			app.UseRouting();

			//app.UseAuthorization();

			// Enable CORS Step 2 - begin
			if (appSettings.CORSConfig != null && appSettings.CORSConfig.Origins.Any())
			{
				app.UseCors(_myAllowSpecificOrigins);
			}
			// Enable CORS Step 2 - end

			// Configure JWT Authentication Step 2 - begin
			app.UseAuthentication();
			app.UseAuthorization();
			// Configure JWT Authentication Step 2 - end

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapControllers();
			});

            //// License validation
            //// TODO: Apply ENV VAR to validate OS.
            //appLifetime.ApplicationStarted.Register(() => appLicense.OnAppStartUp());
		}
	}
}
