using Aide.Core.Adapters;
using Aide.Core.Interfaces;
using Aide.Core.WebApi.Interfaces;
using Aide.Core.WebApi.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using Serilog;
using System.Linq;

namespace Aide.ApiGateway
{
	public class Startup
	{
		readonly string _myAllowSpecificOrigins = "CorsPolicy";

		public Startup(IConfiguration configuration)
		{
			//Configuration = configuration;

			var builder = new ConfigurationBuilder()
				.AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

			Configuration = builder.Build();

			Log.Logger = new LoggerConfiguration()
				.ReadFrom.Configuration(configuration)
				.CreateLogger();
		}

		public IConfiguration Configuration { get; }

		// This method gets called by the runtime. Use this method to add services to the container.
		// For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
		public void ConfigureServices(IServiceCollection services)
		{
			var appSettings = new AppSettings();
			Configuration.GetSection("AppSettings").Bind(appSettings);

			//// The following line enables Application Insights telemetry collection.
			//services.AddApplicationInsightsTelemetry();

			// Need revisit CORS policy
			// Enable CORS Step 1 - begin
			if (appSettings.CORSConfig != null && appSettings.CORSConfig.Origins.Any())
			{
				services.AddCors(x => x.AddPolicy(_myAllowSpecificOrigins, builder =>
				{
					builder
					.AllowAnyMethod()
					.AllowAnyHeader()
					.AllowCredentials()
					.WithOrigins(appSettings.CORSConfig.Origins.ToArray());
				}));
			}
            // Enable CORS Step 1 - end

            // Here you'll find all different options:
            // https://www.strathweb.com/2020/02/asp-net-core-mvc-3-x-addmvc-addmvccore-addcontrollers-and-other-bootstrapping-approaches/
            // IMPORTANT: .NET 6 moved from Newtonsoft.Json to System.Text.Json
            // See links below for further details:
            // https://github.com/dotnet/aspnetcore/issues/17811
            // https://docs.microsoft.com/en-us/aspnet/core/migration/22-to-30?view=aspnetcore-3.1&tabs=visual-studio#jsonnet-support
            services.AddControllers().AddNewtonsoftJson();

			services.AddOcelot();

			// Inject dependencies
			services.AddTransient<AppSettings>(ti => appSettings);
			services.AddSingleton<IAppLicencingAdapter>(si => new AppLicencingAdapter(appSettings.License));
			services.AddTransient<IAppLicenseService, AppLicenseService<Startup>>();
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

			app.UseRouting();

			app.UseEndpoints(endpoints =>
			{
				endpoints.MapGet("/", async context =>
				{
					await context.Response.WriteAsync("Hello World!");
				});
			});

            //// License validation
            //// TODO: Apply ENV VAR to validate OS.
            //appLifetime.ApplicationStarted.Register(() => appLicense.OnAppStartUp());

            // Enable CORS Step 2 - begin
            if (appSettings.CORSConfig != null && appSettings.CORSConfig.Origins.Any())
			{
				app.UseCors(_myAllowSpecificOrigins);
			}
			// Enable CORS Step 2 - end

			app.UseWebSockets();
			app.UseOcelot().Wait();
		}
	}
}
