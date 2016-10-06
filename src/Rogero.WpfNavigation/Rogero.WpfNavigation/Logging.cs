using System;
using Serilog;

namespace Rogero.WpfNavigation
{
    public class Logging
    {
        public static IDisposable Timing(ILogger logger, string description)
        {
            return FinishTiming.StartTiming(description, logger);
        }

        private class FinishTiming : IDisposable
        {
            public DateTime Start { get; }
            public String Description { get; }
            public ILogger Logger { get; }

            public static FinishTiming StartTiming(string description, ILogger logger)
            {
                var timing = new FinishTiming(DateTime.UtcNow, description, logger);
                return timing;
            }

            public FinishTiming(DateTime start, string description, ILogger logger)
            {
                Start = start;
                Description = description;
                Logger = logger;
                Logger.ForContext("Description", Description).Information("Started " + Description + " at {StartTime}", Start);
            }

            public void Dispose()
            {
                var end = DateTime.UtcNow;
                var elapsed = end - Start;
                var logMessage = $"Finished {Description} in {elapsed:c}.";
                Logger.ForContext("Description", Description)
                    .ForContext("Elapsed", elapsed)
                    .Information(logMessage + " at {EndTime}", end);
            }
        }
    }
}