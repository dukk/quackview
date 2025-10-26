// using System.Reflection;
// using System.Text.RegularExpressions;
// using Microsoft.Extensions.DependencyInjection;
// using Microsoft.Extensions.Logging;
// using TypoDukk.QuackView.QuackJob.Jobs;
// using TypoDukk.QuackView.QuackJob.Services;

// namespace TypoDukk.QuackView.QuackJob.Actions;

// internal class NewAction(
//     ILogger<NewAction> logger,
//     IConsoleService console,
//     IServiceProvider serviceProvider,
//     IJobManagementService jobManagement)
//     : Action(logger, console)
// {
//     protected readonly ILogger<NewAction> Logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     protected readonly IServiceProvider ServiceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
//     protected readonly IJobManagementService JobManagement = jobManagement ?? throw new ArgumentNullException(nameof(jobManagement));

//     public override async Task ExecuteAsync(string[] args)
//     {

//     }
// }