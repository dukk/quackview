using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TypoDukk.QuackView.QuackJob;
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
            host.Services.GetService<IServiceProvider>(),
            host.Services.GetService<ILogger<Program>>(),
            host.Services.GetService<IConsoleService>());

        #pragma warning restore CS8604 // Possible null reference argument.
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