using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TypoDukk.QuackView.QuackJob;
using TypoDukk.QuackView.QuackJob.Actions;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Tests;

namespace TypoDukk.QuackView.QuackJob.Tests;

[TestClass]
public sealed class ProgramTests
{
    internal Program CreateProgram()
    {
#pragma warning disable CS8604 // Possible null reference argument.

        var host = TestHost.CreateHost();
        return new Program(
            host.Services.GetService<ILogger<Program>>(),
            host.Services.GetService<IServiceProvider>(),
            host.Services.GetService<IConsoleService>());

#pragma warning restore CS8604 // Possible null reference argument.
    }

    [TestMethod]
    public void VerifyNoDuplicationActions()
    {
        var host = TestHost.CreateHost();
        var actions = host.Services.GetServices<IAction>();
        var registeredActions = host.Services.GetServices<IAction>().ToList();
        var duplicateActionGroups = registeredActions
            .GroupBy(a => a.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateActionGroups.Count > 0)
        {
            var details = string.Join("; ", duplicateActionGroups.Select(g =>
                $"{g.Key}: {string.Join(", ", g.Select(a => a.GetType().FullName))}"));
            throw new InvalidOperationException($"Duplicate action names detected: {details}");
        }
    }
    
    [TestMethod]
    public void VerifyNoDuplicationJobs() 
    {
        var host = TestHost.CreateHost();
        var jobs = host.Services.GetService<IJobRunner>();
        var registeredJobs = host.Services.GetServices<IJobRunner>().ToList();
        var duplicateJobGroups = registeredJobs
            .GroupBy(j => j.Name, StringComparer.OrdinalIgnoreCase)
            .Where(g => g.Count() > 1)
            .ToList();

        if (duplicateJobGroups.Count > 0)
        {
            var details = string.Join("; ", duplicateJobGroups.Select(g =>
                $"{g.Key}: {string.Join(", ", g.Select(a => a.GetType().FullName))}"));
            throw new InvalidOperationException($"Duplicate job names detected: {details}");
        }
    }

    [TestMethod]
    public async Task Run_With_InvalidAction()
    {
        // Arrange
        var console = Substitute.For<IConsoleService>();
        var host = TestHost.CreateHost(consoleConstructor: () => console);
        var args = new[] { "blah-blah-fish-cow-blah" };
        var program = this.CreateProgram();

        // Act
        await program.Run(args);
    }
}