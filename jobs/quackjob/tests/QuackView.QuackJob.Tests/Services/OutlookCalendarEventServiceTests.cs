using Microsoft.Extensions.Logging;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NSubstitute;
using TypoDukk.QuackView.QuackJob.Services;
using TypoDukk.QuackView.QuackJob.Data;

namespace TypoDukk.QuackView.QuackJob.Tests.Services
{
    [TestClass]
    public sealed class OutlookCalendarEventServiceTests
    {
        [TestMethod]
        public async Task GetCalendarsAsync_NoCredentials_ThrowsInvalidOperationException()
        {
            // Arrange
            var graph = Substitute.For<IGraphService>();
            var logger = Substitute.For<ILogger<OutlookCalendarEventService>>();
            var service = new OutlookCalendarEventService(logger, graph);

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentNullException>(async () => await service.GetCalendarsAsync(string.Empty));
        }

        [TestMethod]
        public async Task GetEventsAsync_InvalidDateRange_ThrowsArgumentException()
        {
            // Arrange
            var graph = Substitute.For<IGraphService>();
            var logger = Substitute.For<ILogger<OutlookCalendarEventService>>();
            var service = new OutlookCalendarEventService(logger, graph);
            var start = DateTime.UtcNow;
            var end = start.AddHours(-1); // End is before start

            // Act & Assert
            await Assert.ThrowsExceptionAsync<ArgumentException>(async () => await service.GetEventsAsync("testAccount", new[] { "Calendar1" }, start, end));
        }

        // Coming back to this....
        // [TestMethod]
        // public async Task GetEventsAsync_ValidParameters_CallsGraphService()
        // {
        //     // Arrange
        //     var graph = Substitute.For<IGraphService>().When;
        //     var logger = Substitute.For<ILogger<OutlookCalendarEventService>>();
        //     var service = new OutlookCalendarEventService(logger, graph);
        //     var start = DateTime.UtcNow;
        //     var end = start.AddHours(1);

        //     // Act
        //     await service.GetEventsAsync("testAccount", new[] { "Calendar1" }, start, end);

        //     // Assert
        //     await graph.Received(1).ExecuteInContextAsync(Arg.Any<Func<Microsoft.Graph.GraphServiceClient,
        //         Task<IEnumerable<CalendarEvent>>>>(), "testAccount");
        // }

        [TestMethod]
        public async Task GetCalendarsAsync_ValidParameters_CallsGraphService()
        {
            // Arrange
            var graph = Substitute.For<IGraphService>();
            var logger = Substitute.For<ILogger<OutlookCalendarEventService>>();
            var service = new OutlookCalendarEventService(logger, graph);

            // Act
            await service.GetCalendarsAsync("testAccount");

            // Assert
            await graph.Received(1).ExecuteInContextAsync(Arg.Any<Func<Microsoft.Graph.GraphServiceClient,
                Task<IEnumerable<CalendarEventCalendar>>>>(), "testAccount");
        }
    }
}