using Castle.Core.Logging;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using TypoDukk.QuackView.QuackJob.Actions;
using TypoDukk.QuackView.QuackJob.Jobs;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests.Actions;

[TestClass]
public class RebuildScheduleActionTests
{
    [TestMethod]
    public void ExecuteAsync_ShouldRunWithoutExceptions()
    {
        // Arrange
        var logger = Substitute.For<ILogger<RebuildScheduleAction>>();
        var cronScheduler = Substitute.For<ICronScheduler>();
        var console = Substitute.For<IConsoleService>();
        var disk = Substitute.For<IDiskIOService>();
        var specialPaths = Substitute.For<ISpecialPaths>();
        var action = new RebuildScheduleAction(logger, cronScheduler, console, disk, specialPaths);

        disk.DirectoryExistsAsync("test-jobs").Returns(true);
        disk.EnumerateFilesAsync("test-jobs", "*.json").Returns(["job1.json", "job2.json"]);
        disk.ReadAllTextAsync("job1.json").Returns(new JobFile() { Metadata = new() { Name = "Job 1", Schedule = "* * * * *" } }.ToJson());
        disk.ReadAllTextAsync("job2.json").Returns(new JobFile() { Metadata = new() { Name = "Job 2", Schedule = "* * * * *" } }.ToJson());
        specialPaths.GetJobsDirectoryPathAsync().Returns("test-jobs");
        specialPaths.GetQuackJobExecutablePathAsync().Returns("test-quackjob");
        specialPaths.GetCrontabFilePathAsync().Returns("test-crontab");

        // Act & Assert
        Task.Run(async () => await action.ExecuteAsync(Array.Empty<string>())).GetAwaiter().GetResult();

        cronScheduler.Received().BackupSchedule();
        cronScheduler.Received().ClearAllJobsAsync();
        cronScheduler.Received(2).ScheduleAsync(Arg.Any<CronJob>());
    }
}