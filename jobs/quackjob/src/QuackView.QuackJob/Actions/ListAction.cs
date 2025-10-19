using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class ListAction(
    ILogger<ListAction> logger,
    IConsoleService console,
    IServiceProvider serviceProvider) 
    : Action(logger, console)
{
    protected readonly new ILogger<ListAction> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
    protected readonly IServiceProvider ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public override async Task ExecuteAsync(string[] args)
    {
        await Task.Run(() =>
        {
            var jobRunners = this.ServiceProvider.GetServices<IJobRunner>();

            this.Console.WriteLine("Available Jobs Runners:");

            foreach (var jobRunner in jobRunners.OrderBy(j => j.Name))
                this.Console.WriteLine($" {jobRunner.Name}\t- {jobRunner.Description}");
        });
    }
}