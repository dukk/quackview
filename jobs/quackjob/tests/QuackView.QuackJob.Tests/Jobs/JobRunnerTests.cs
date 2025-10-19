using System.Text.Json;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;
namespace TypoDukk.QuackView.QuackJob.Tests.Jobs;

[TestClass]
public sealed class JobRunnerTests
{
    [TestMethod]
    public void GetNameFromType()
    {
        Assert.AreEqual("test", JobRunner.GetNameFromType(typeof(TestJob)));
        Assert.AreEqual("test-test-test", JobRunner.GetNameFromType(typeof(TestTestTestJob)));
    }
}

internal class TestJob : JobRunner
{
    public override Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        throw new NotImplementedException();
    }
}

internal class TestTestTestJob : JobRunner
{
    public override Task ExecuteAsync(JsonElement? jsonConfig = null)
    {
        throw new NotImplementedException();
    }
}

internal class TestJobConfig
{

}