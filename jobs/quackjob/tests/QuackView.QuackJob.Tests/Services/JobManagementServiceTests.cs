// using TypoDukk.QuackView.QuackJob.Jobs;
// using TypoDukk.QuackView.QuackJob.Services;
// using NSubstitute;
// using Microsoft.Extensions.Logging;
// using System.Text.Json;
// using System.Text.Json.Nodes;

// namespace TypoDukk.QuackView.QuackJob.Tests.Services;

// [TestClass]
// internal class JobManagementServiceTests
// {
//     [TestMethod]
//     public async Task GetAvailableJobFilesAsync_ReturnsListOfFiles()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<JobManagementService>>();
//         var directory = Substitute.For<IDirectoryService>();
//         var file = Substitute.For<IFileService>();
//         var SpecialPaths = Substitute.For<ISpecialPaths>();
//         var jobManagementService = new JobManagementService(logger, directory, file, SpecialPaths);
//         var jobFiles = new string[] { "job1.json", "job2.json", "job3.json" };

//         directory.EnumerateFilesAsync(Path.Combine("quackview-tests", "jobs")).Returns(jobFiles);

//         // Act
//         var result = await jobManagementService.GetAvailableJobFilesAsync();

//         // Assert
//         Assert.IsNotNull(result);
//     }

//     [TestMethod]
//     public async Task LoadJobFileAsync_ReturnsJobFileWithNoConfig()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<JobManagementService>>();
//         var directory = Substitute.For<IDirectoryService>();
//         var file = Substitute.For<IFileService>();
//         var SpecialPaths = Substitute.For<ISpecialPaths>();
//         var jobManagementService = new JobManagementService(logger, directory, file, SpecialPaths);

//         file.ExistsAsync(Arg.Any<string>()).Returns(true);
//         file.ReadAllTextAsync(Arg.Any<string>()).Returns(Task.FromResult(getJobFileContentWithNoConfig()));

//         // Act
//         var result = await jobManagementService.LoadJobFileAsync<object>("test-job.json");

//         // Assert
//         Assert.IsNotNull(result);
//     }

//     [TestMethod]
//     public async Task LoadJobFileAsync_ReturnsJobFileWithConfig()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<JobManagementService>>();
//         var directory = Substitute.For<IDirectoryService>();
//         var file = Substitute.For<IFileService>();
//         var SpecialPaths = Substitute.For<ISpecialPaths>();
//         var jobManagementService = new JobManagementService(logger, directory, file, SpecialPaths);

//         file.ExistsAsync(Arg.Any<string>()).Returns(true);
//         file.ReadAllTextAsync(Arg.Any<string>()).Returns(Task.FromResult(getJobFileContentWithConfig()));

//         // Act
//         var result = await jobManagementService.LoadJobFileAsync<object>("test-job.json");

//         // Assert
//         Assert.IsNotNull(result);
//     }

//     [TestMethod]
//     public async Task SaveJobFileAsync_SavesFile_NoConfig()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<JobManagementService>>();
//         var directory = Substitute.For<IDirectoryService>();
//         var file = Substitute.For<IFileService>();
//         var SpecialPaths = Substitute.For<ISpecialPaths>();
//         var jobManagementService = new JobManagementService(logger, directory, file, SpecialPaths);

//         file.ExistsAsync(Arg.Any<string>()).Returns(false);
//         SpecialPaths.GetJobsDirectoryPathAsync().Returns(Path.Combine("quackview-tests", "jobs"));

//         // Act
//         var jobFile = this.getJobFileWithNoConfig();
//         await jobManagementService.SaveJobFileAsync("test-job.json", jobFile);

//         // Assert
//         var jobFilePath = Path.Combine("quackview-tests", "jobs", "test-job.json");

//         await file.Received().ExistsAsync(jobFilePath);
//         await file.Received().WriteAllTextAsync(jobFilePath, Arg.Any<string>());
//     }

//     [TestMethod]
//     public async Task SaveJobFileAsync_SavesFile_Config()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<JobManagementService>>();
//         var directory = Substitute.For<IDirectoryService>();
//         var file = Substitute.For<IFileService>();
//         var SpecialPaths = Substitute.For<ISpecialPaths>();
//         var jobManagementService = new JobManagementService(logger, directory, file, SpecialPaths);

//         file.ExistsAsync(Arg.Any<string>()).Returns(false);
//         SpecialPaths.GetJobsDirectoryPathAsync().Returns(Path.Combine("quackview-tests", "jobs"));

//         // Act
//         var jobFile = this.getJobFileWithConfig();
//         await jobManagementService.SaveJobFileAsync("test-job.json", jobFile);

//         // Assert
//         var jobFilePath = Path.Combine("quackview-tests", "jobs", "test-job.json");

//         await file.Received().ExistsAsync(jobFilePath);
//         await file.Received().WriteAllTextAsync(jobFilePath, Arg.Any<string>());
//     }

//     [TestMethod]
//     public async Task DeleteJobFileAsync_ValidPath_DeletesFile()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<JobManagementService>>();
//         var directory = Substitute.For<IDirectoryService>();
//         var file = Substitute.For<IFileService>();var SpecialPaths = Substitute.For<ISpecialPaths>();
//         var jobManagementService = new JobManagementService(logger, directory, file, SpecialPaths);

//         SpecialPaths.GetJobsDirectoryPathAsync().Returns(Path.Combine("quackview-tests", "jobs"));

//         // Act
//         await jobManagementService.DeleteJobFileAsync("test-job.json");

//         // Assert
//         await file.Received().DeleteFileAsync(Path.Combine("quackview-tests", "jobs", "test-job.json"));
//     }

//     private JobFile<object> getJobFileWithNoConfig()
//     {
//         var jobFile = new JobFile<object>()
//         {
//             Metadata = new()
//             {
//                 Name = "test job",
//                 Description = "test job description",
//                 Runner = "test-runner",
//                 Schedule = "*/10 * * * *"
//             }
//         };

//         return jobFile;
//     }

//     private string getJobFileContentWithNoConfig()
//     {
//         return this.getJobFileWithNoConfig().ToJson();
//     }

//     private JobFile<object> getJobFileWithConfig()
//     {
//         var jobFile = new JobFile<object>()
//         {
//             Metadata = new()
//             {
//                 Name = "test job",
//                 Description = "test job description",
//                 Runner = "test-runner",
//                 Schedule = "*/10 * * * *"
//             },
//             Config = JsonSerializer.Deserialize<JsonObject>("{\"blah\": \"test\"}")
//         };

//         return jobFile;
//     }

//     private string getJobFileContentWithConfig()
//     {
//         return this.getJobFileWithConfig().ToJson();
//     }
// }