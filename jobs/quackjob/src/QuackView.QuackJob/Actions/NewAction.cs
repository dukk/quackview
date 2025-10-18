using System.Reflection;
using System.Text.RegularExpressions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Actions;

// TODO: Come back to this after I have a job management service implemented.

// internal class NewAction(ILogger<NewAction> logger, IConsoleService console, IServiceProvider serviceProvider) : Action(console)
// {
//     private readonly ILogger<NewAction> logger = logger ?? throw new ArgumentNullException(nameof(logger));
//     private readonly IServiceProvider serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));

//     public override async Task ExecuteAsync(string[] args)
//     {
        
//     }
// }