using System.Text.Json;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;
namespace TypoDukk.QuackView.QuackJob.Tests.Jobs;

// [TestClass]
// public sealed class JobRunnerTests
// {

// }

internal class TestJobRunner(IDiskIOService file) : JobRunner(file)
{
    public override Task ExecuteJobFileAsync(JobFile jobFile)
    {
        throw new NotImplementedException();
    }
}

internal class TestTestTestJobRunner(IDiskIOService file) : JobRunner(file)
{
    public override Task ExecuteJobFileAsync(JobFile jobFile)
    {
        throw new NotImplementedException();
    }
}
