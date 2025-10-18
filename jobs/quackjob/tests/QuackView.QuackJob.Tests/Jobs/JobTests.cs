using TypoDukk.QuackView.QuackJob.Jobs;
namespace TypoDukk.QuackView.QuackJob.Tests.Jobs;

[TestClass]
public sealed class JobTests
{
    [TestMethod]
    public void GetNameFromType()
    {
        Assert.AreEqual("test", Job<TestJobConfig>.GetNameFromType(typeof(TestJob)));
        Assert.AreEqual("test-test-test", Job<TestJobConfig>.GetNameFromType(typeof(TestTestTestJob)));
    }
}

internal class TestJob() : Job<TestJobConfig>
{
    override public Task ExecuteAsync(TestJobConfig config)
    {
        throw new NotImplementedException();
    }
}

internal class TestTestTestJob() : Job<TestJobConfig>
{
    override public Task ExecuteAsync(TestJobConfig config)
    {
        throw new NotImplementedException();
    }
}

internal class TestJobConfig
{

}