using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TypoDukk.QuackView.QuackJob;
using TypoDukk.QuackView.QuackJob.Actions;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests;

internal static class TestHost
{
    public static readonly Func<IFileService> DefaultFileServiceConstruction = () => Substitute.For<IFileService>();
    public static readonly Func<IDirectoryService> DefaultDirectoryServiceConstruction = () => Substitute.For<IDirectoryService>();
    public static readonly Func<IConsoleService> DefaultConsoleServiceConstruction = () => Substitute.For<IConsoleService>();

    public static IHost CreateHost(Type[]? excludeServiceTypes = null,
        bool expandActions = false, bool expandJobs = false,
        Func<IFileService>? fileConstructor = null,
        Func<IDirectoryService>? directoryConstructor = null,
        Func<IConsoleService>? consoleConstructor = null)
    {
        var hostBuilder = Host.CreateDefaultBuilder();

        hostBuilder.ConfigureAppConfiguration((context, config) =>
        {
            config.AddJsonFile(Environment.ExpandEnvironmentVariables("%QUACKVIEW_DIR%config/quackjob.json"),
                optional: true, reloadOnChange: true);
        });

        hostBuilder.ConfigureServices(services =>
        {
            Program.ComposeServices(services);

            services.RemoveAll<IFileService>();
            services.RemoveAll<IDirectoryService>();
            services.RemoveAll<IConsoleService>();

            var fileService = (fileConstructor ?? TestHost.DefaultFileServiceConstruction)();
            var directoryService = (directoryConstructor ?? TestHost.DefaultDirectoryServiceConstruction)();
            var consoleService = (consoleConstructor ?? TestHost.DefaultConsoleServiceConstruction)();

            services.AddSingleton(fileService);
            services.AddSingleton(directoryService);
            services.AddSingleton(consoleService);

            Program.ComposeActions(services);

            if (expandActions)
            {
                var serviceProvider = services.BuildServiceProvider();
                var actions = serviceProvider.GetServices<IAction>();
                foreach (var action in actions)
                    _ = services.AddSingleton(action.GetType(), action);
            }

            Program.ComposeJobs(services);

            if (expandJobs)
            {
                var serviceProvider = services.BuildServiceProvider();
                var jobs = serviceProvider.GetServices<IJob>();
                foreach (var job in jobs)
                    _ = services.AddSingleton(job.GetType(), job);
            }

            if (excludeServiceTypes != null)
            {
                hostBuilder.ConfigureServices((context, services) =>
                {
                    foreach (var service in excludeServiceTypes)
                        services.RemoveAll(service);
                });
            }
        });

        return hostBuilder.Build();
    }
}