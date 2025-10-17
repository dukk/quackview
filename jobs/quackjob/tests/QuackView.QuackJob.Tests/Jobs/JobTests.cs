using TypoDukk.QuackView.QuackJob.Jobs;
namespace TypoDukk.QuackView.QuackJob.Tests.Jobs;

[TestClass]
public sealed class JobTests
{
    [TestMethod]
    public void MatchesArgName_ValidName_ReturnsTrue()
    {
        IJob job = new TestJob();

        Assert.IsTrue(job.MatchesArgName("test"));
        Assert.IsTrue(job.MatchesArgName("Test"));
        Assert.IsTrue(job.MatchesArgName("TEST"));
        Assert.IsTrue(job.MatchesArgName("--TEST"));

        IJob job2 = new TestTestTestJob();

        Assert.IsTrue(job2.MatchesArgName("test-test-test"));
        Assert.IsTrue(job2.MatchesArgName("TestTestTest"));
        Assert.IsTrue(job2.MatchesArgName("TEST-TEST-TEST"));
        Assert.IsTrue(job2.MatchesArgName("--TEST-TEST-TEST"));
    }

    [TestMethod]
    public void MatchesArgName_InvalidName_ReturnsFalse()
    {
        IJob job = new TestJob();

        Assert.IsFalse(job.MatchesArgName("tests"));
        Assert.IsFalse(job.MatchesArgName("test-job"));
        Assert.IsFalse(job.MatchesArgName("TestJob"));
        Assert.IsFalse(job.MatchesArgName("Other"));

        IJob job2 = new TestTestTestJob();

        Assert.IsFalse(job2.MatchesArgName("tests"));
        Assert.IsFalse(job2.MatchesArgName("test-job"));
        Assert.IsFalse(job2.MatchesArgName("TestJob"));
        Assert.IsFalse(job2.MatchesArgName("Other"));
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