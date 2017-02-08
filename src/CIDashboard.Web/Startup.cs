using System;
using System.Configuration;
using Autofac;
using Autofac.Integration.SignalR;
using AutoMapper;
using CIDashboard.Data.CompositionRoot;
using CIDashboard.Data.Interfaces;
using CIDashboard.Domain.CompositionRoot;
using CIDashboard.Domain.MappingProfiles;
using CIDashboard.Web;
using CIDashboard.Web.Application.Interfaces;
using CIDashboard.Web.CompositionRoot;
using CIDashboard.Web.Hubs;
using CIDashboard.Web.MappingProfiles;
using Hangfire;
using Hangfire.SqlServer;
using Microsoft.AspNet.SignalR;
using Microsoft.Owin;
using Owin;
using Serilog;

[assembly: OwinStartup(typeof(Startup))]

namespace CIDashboard.Web
{
    public class Startup
    {
        public void Configuration(IAppBuilder app)
        {
            ConfigureLog();

            ContainerBuilder builder = new ContainerBuilder();

            // STANDARD SIGNALR SETUP:
            // Get your HubConfiguration. In OWIN, you'll create one
            // rather than using GlobalHost.
            HubConfiguration config = new HubConfiguration();

            // Register your SignalR hubs.
            // builder.RegisterHubs(Assembly.GetExecutingAssembly());
            builder.RegisterType<CiDashboardHub>().ExternallyOwned();

            // add application modules
            builder.RegisterModule<DataModule>();
            builder.RegisterModule<WebModule>();
            builder.RegisterModule<CiServicesModule>();
            
            // Set the dependency resolver to be Autofac.
            IContainer container = builder.Build();
            config.Resolver = new AutofacDependencyResolver(container);

            // OWIN SIGNALR SETUP:
            // Register the Autofac middleware FIRST, then the standard SignalR middleware.
            app.UseAutofacMiddleware(container);

            ConfigureMappers();

            GlobalHost.DependencyResolver = config.Resolver;

            // force DB creation if it does not exists
            container.Resolve<ICiDashboardContextBootstrap>().InitiateDatabase();

            ConfigureHangfireJobs(container);

            app.MapSignalR("/signalr", config);
        }

        private void ConfigureMappers()
        {
            Mapper.Initialize(cfg =>
            {
                cfg.AddProfile<ViewModelProfilers>();
                cfg.AddProfile<TeamCityProfiler>();
            });
        }

        private void ConfigureHangfireJobs(IContainer container)
        {
            GlobalConfiguration.Configuration
                .UseAutofacActivator(container)
                .UseSqlServerStorage(
                    "CiDashboardContext",
                    new SqlServerStorageOptions {QueuePollInterval = TimeSpan.FromSeconds(5)});


            string refreshInfoCron = ConfigurationManager.AppSettings["RefreshInfoCron"];
            if(string.IsNullOrEmpty(refreshInfoCron))
                refreshInfoCron = "*/5 * * * *";
            
            RecurringJob.RemoveIfExists("SendRefreshBuildResults");
            RecurringJob.AddOrUpdate("SendRefreshBuildResults", () => container.Resolve<IRefreshInformation>().SendRefreshBuildResultsSync(), refreshInfoCron);
        }

        private void ConfigureLog()
        {
            Log.Logger = new LoggerConfiguration()
                .ReadFrom.AppSettings()
                .CreateLogger();
        }
    }
}
