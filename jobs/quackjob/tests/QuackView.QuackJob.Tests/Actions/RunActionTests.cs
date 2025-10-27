using Microsoft.Extensions.Logging;
using NSubstitute;
using TypoDukk.QuackView.QuackJob.Actions;
using TypoDukk.QuackView.QuackJob.Services;

namespace TypoDukk.QuackView.QuackJob.Tests.Actions;

[TestClass]
internal class RunActionTests
{
    [TestInitialize]
    public void Setup()
    {
        this.Logger = Substitute.For<ILogger<RunAction>>();
        this.CommandLineParser = new CommandLineParser();
        this.ServiceProvider = Substitute.For<IServiceProvider>();
        this.Disk = Substitute.For<IDiskIOService>();
        this.Console = Substitute.For<IConsoleService>();
        this.SpecialPaths = Substitute.For<ISpecialPaths>();

        // this.ServiceProvider.GetServices<IJobRunner>().Returns([new BuildImageFileListJob()]);
    }

    [TestCleanup]
    public void Cleanup()
    {

    }

#pragma warning disable CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.
    protected ILogger<RunAction> Logger { get; private set; }
    protected ICommandLineParser CommandLineParser { get; private set; }
    protected IServiceProvider ServiceProvider { get; private set; }
    protected IDiskIOService Disk { get; private set; }
    protected IConsoleService Console { get; private set; }
    protected ISpecialPaths SpecialPaths { get; private set; }
#pragma warning restore CS8618 // Non-nullable field must contain a non-null value when exiting constructor. Consider adding the 'required' modifier or declaring as nullable.

    [TestMethod]
    public async Task ExecuteAsync_NoArgs_ThrowsException()
    {
        // Arrange
        var runAction = new RunAction(this.Logger, this.CommandLineParser, this.ServiceProvider,
        this.Disk, this.Console, this.SpecialPaths);

        // Act
        await Assert.ThrowsExceptionAsync<Exception>(async () => await runAction.ExecuteAsync([]));
    }

    [TestMethod]
    public async Task ExecuteAsync_InvalidJobFile_ThrowsException()
    {
        // Arrange
        this.Disk.FileExistsAsync(Arg.Any<string>()).Returns(false);
        this.SpecialPaths.GetJobsDirectoryPathAsync().Returns("test-jobs");

        var runAction = new RunAction(this.Logger, this.CommandLineParser, this.ServiceProvider, this.Disk, this.Console, this.SpecialPaths);

        // Act & Assert
        await Assert.ThrowsExceptionAsync<FileNotFoundException>(async () => await runAction.ExecuteAsync(["--job", "non-existing-file.json"]));
    }

    // [TestMethod]
    // public async Task ExecuteAsync_ValidJobFile_Successful()
    // {
    //     // Arrange
    //     this.DiskService.ExistsAsync(Arg.Any<string>()).Returns(true);
    //     this.DiskService.ReadAllTextAsync(Arg.Any<string>()).Returns(new JobFile<BuildImageFileListJobConfig>()
    //     {
    //         Metadata = new()
    //         {
    //             Name = "Test build image file list",
    //             Description = "Test",
    //             Runner = "build-image-file-list",
    //             Schedule = "* * * * *"
    //         },
    //         Config = new()
    //         {
    //             DirectoryPath = "photos",
    //             OutputDataFile = "photo-list.json",
    //             IncludeSubdirectories = false,
    //             SearchPattern = "*.jpg"
    //         }
    //     }.ToJson());
    //     this.SpecialPaths.GetJobsDirectoryPathAsync().Returns("test-jobs");

    //     var runAction = new RunAction(this.Logger, this.CommandLineParser, this.ServiceProvider, this.DiskService, this.ConsoleService, this.SpecialPaths);

    //     await runAction.ExecuteAsync(["--job", "valid-job-file.json.json"]);
    // }
}