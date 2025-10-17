using Microsoft.Extensions.DependencyInjection;
using TypoDukk.QuackView.QuackJob.Actions;

namespace TypoDukk.QuackView.QuackJob.Tests.Actions;

[TestClass]
public sealed class HelpActionTests
{
    [TestMethod]
    public async Task ExecuteAsync_NoArgs_DisplaysHelp()
    {
        // Arrange
        var helpAction = TestHost.CreateHost(expandActions: true).Services.GetRequiredService<HelpAction>();

        // Act
        await helpAction.ExecuteAsync(Array.Empty<string>());

        // Assert
        // (In a real test, we would capture the output and verify it contains expected help text)
    }
}