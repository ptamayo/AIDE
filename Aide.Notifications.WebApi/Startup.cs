using System.Linq;
using Aide.Core.Interfaces;
using Aide.Notifications.Models;
using Aide.Notifications.WebApi.Hubs;
using Aide.Notifications.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Aide.Core.WebApi.Interfaces;
using Aide.Core.WebApi.Services;
using Aide.Core.Adapters;
using Serilog;
using Microsoft.Extensions.Logging;
using System;

namespace Aide.Notifications.WebApi
{
    public class Startup
	{
		readonly string _myAllowSpecificOrigins = "CorsPolicy";

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

			// Enable SignalR Step 1 - begin
			services.AddSignalR();
			services.AddSingleton<IUserIdProvider, CustomUserIdProvider>();
			// Enable SignalR Step 1 - end

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

			services.AddControllersWithViews().AddNewtonsoftJson();
			ConfigureDependencies(services);
		}

		public void ConfigureDependencies(IServiceCollection services)
		{
			var appSettings = new AppSettings();
			Configuration.GetSection("AppSettings").Bind(appSettings);
			services.AddTransient<AppSettings>(ti => appSettings);

			//services.AddSingleton<ICacheService>(si => new DotNetMemoryCacheService(EnumTimeExpression.CoordinatedUniversalTime, appSettings.CacheConfig.Enabled));
			services.AddSingleton<IAppLicencingAdapter>(si => new AppLicencingAdapter(appSettings.License));

			services.AddTransient<IAppLicenseService, AppLicenseService<Startup>>();
			services.AddTransient<AideDbContext>(ti => new AideDbContext(Configuration.GetConnectionString("DbAideNotifications")));
			services.AddTransient<IUserService, UserService>();
			services.AddTransient<INotificationService, NotificationService>();
			services.AddTransient<INotificationUserService, NotificationUserService>();
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
			else
			{
				app.UseExceptionHandler("/Home/Error");
			}

			loggerFactory.AddSerilog();

			// Enable CORS Step 2 - begin
			if (appSettings.CORSConfig != null && appSettings.CORSConfig.Origins.Any())
			{
				app.UseCors(_myAllowSpecificOrigins);
			}
			// Enable CORS Step 2 - end

			app.UseStaticFiles();

			app.UseRouting();

			app.UseAuthorization();

			// Enable SignalR Step 2 - begin
			app.UseEndpoints(endpoints =>
			{
				endpoints.MapHub<NotificationHub>("/notificationHub");
				endpoints.MapControllerRoute(
					name: "default",
					pattern: "{controller=Home}/{action=Index}/{id?}");
			});
            // Enable SignalR Step 2 - end

            //// License validation
            //// TODO: Apply ENV VAR to validate OS.
            //appLifetime.ApplicationStarted.Register(() => appLicense.OnAppStartUp());
		}
	}
}
