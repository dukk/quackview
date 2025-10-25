// using Microsoft.Extensions.Logging;
// using NSubstitute;
// using NSubstitute.Core.Arguments;
// using TypoDukk.QuackView.QuackJob.Data;
// using TypoDukk.QuackView.QuackJob.Jobs;
// using TypoDukk.QuackView.QuackJob.Services;

// namespace TypoDukk.QuackView.QuackJob.Tests.Jobs;

// [TestClass]
// internal sealed class UpcomingCalendarEventsJobTests
// {
//     [TestMethod]
//     public async Task ExecuteAsync_WithValidConfig_ShouldFetchEvents()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<UpcomingCalendarEventsJob>>();
//         var outlookService = Substitute.For<IOutlookCalendarEventService>();
//         var dataFileService = Substitute.For<IDataFileService>();
//         var job = new UpcomingCalendarEventsJob(logger, outlookService, dataFileService);

//         outlookService.GetEventsAsync(
//             Arg.Any<string>(),
//             Arg.Any<string[]>(),
//             Arg.Any<DateTime>(),
//             Arg.Any<DateTime>()).Returns(Task.FromResult<IEnumerable<CalendarEvent>>(
//             [
//                 new CalendarEvent
//                 {
//                     Subject = "Test Event 1",
//                     Start = DateTime.UtcNow.ToLongDateString(),
//                     End = DateTime.UtcNow.AddHours(1).ToLongDateString()
//                 },
//                 new CalendarEvent
//                 {
//                     Subject = "Test Event 2",
//                     Start = DateTime.UtcNow.AddDays(1).ToLongDateString(),
//                     End = DateTime.UtcNow.AddDays(1).AddHours(1).ToLongDateString()
//                 }
//             ]));


//         var config = new UpcomingCalendarEventsJobConfig
//         {
//             DaysInFuture = 14,
//             Accounts =
//             [
//                 new() {
//                     Account = "john.doe@example.com",
//                     Calendars = ["Family", "Holiday"]
//                 }
//             ],
//             OutputFileName = "calendar/upcoming-events.json"
//         };

//         // Act
//         await job.ExecuteAsync(config);

//         // Assert
//         _ = logger.ReceivedCalls().Any();
//         await outlookService.Received(1).GetEventsAsync("john.doe@example.com", Arg.Any<string[]>(), Arg.Any<DateTime>(), Arg.Any<DateTime>());
//         await dataFileService.Received(1).WriteJsonFileAsync("calendar/upcoming-events.json", Arg.Is<IEnumerable<CalendarEvent>>(events => events.Count() == 2));
//     }

//     [TestMethod]
//     public async Task ExecuteAsync_WithInvalidConfig_ShouldThrowArgumentException()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<UpcomingCalendarEventsJob>>();
//         var outlookService = Substitute.For<IOutlookCalendarEventService>();
//         var dataFileService = Substitute.For<IDataFileService>();
//         var job = new UpcomingCalendarEventsJob(logger, outlookService, dataFileService);

//         var config = new UpcomingCalendarEventsJobConfig
//         {
//             DaysInFuture = 14,
//             Accounts = [],
//             OutputFileName = "calendar/upcoming-events.json"
//         };

//         // Act & Assert
//         await Assert.ThrowsExceptionAsync<ArgumentException>(() => job.ExecuteAsync(config));
//     }

//     [TestMethod]
//     public async Task ExecuteAsync_WithNullConfig_ShouldThrowArgumentNullException()
//     {
//         // Arrange
//         var logger = Substitute.For<ILogger<UpcomingCalendarEventsJob>>();
//         var outlookService = Substitute.For<IOutlookCalendarEventService>();
//         var dataFileService = Substitute.For<IDataFileService>();
//         var job = new UpcomingCalendarEventsJob(logger, outlookService, dataFileService);

//         // Act & Assert
//         await Assert.ThrowsExceptionAsync<ArgumentNullException>(() => job.ExecuteAsync(null!));
//     }
// }