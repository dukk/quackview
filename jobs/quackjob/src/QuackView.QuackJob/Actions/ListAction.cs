using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

internal class ListAction(ILogger<ListAction> logger, IConsoleService console, IServiceProvider serviceProvider) : Action(console)
{
    private readonly ILogger<ListAction> logger = logger ?? throw new ArgumentNullException(nameof(logger));
    private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

    public override async Task ExecuteAsync(string[] args)
    {
        await Task.Run(() =>
        {
            var jobs = this.serviceProvider.GetServices<IJob>();
            var jobTypes = from j in jobs
                           select j.GetType();

            console.WriteLine("Available Jobs:");

            foreach (var job in jobTypes.OrderBy(j => j.Name))
            {
                console.WriteLine($"\t{job.Name}\t- ...");
            }
        });
    }
}