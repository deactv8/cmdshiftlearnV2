using CmdShiftLearn.Api.Models;

namespace CmdShiftLearn.Api.Services
{
    public interface IEventLogger
    {
        Task LogAsync(PlatformEvent platformEvent);
    }

    public class EventLoggerService : IEventLogger
    {
        public Task LogAsync(PlatformEvent platformEvent)
        {
            // For now, just print to the console
            // In the future, this could write to a database, send to a webhook, etc.
            Console.WriteLine($"[EVENT] {platformEvent.Timestamp:yyyy-MM-dd HH:mm:ss} | {platformEvent.EventType} | User: {platformEvent.UserId} | {platformEvent.Description}");
            
            return Task.CompletedTask;
        }
    }
}