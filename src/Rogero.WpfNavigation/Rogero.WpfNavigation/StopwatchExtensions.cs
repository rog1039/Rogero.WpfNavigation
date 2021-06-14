using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace Rogero.WpfNavigation
{
    internal static class StopwatchExtensions
    {
        public static StopwatchCheckpoint Checkpoint(this Stopwatch stopwatch, string name = "")
        {
            var elapsedTicks = stopwatch.ElapsedTicks;
            var checkpoint = new StopwatchCheckpoint(name,
                                                     null,
                                                     elapsedTicks,
                                                     elapsedTicks);
            return checkpoint;
        }

        public static StopwatchCheckpoint Checkpoint(this Stopwatch stopwatch, string name, StopwatchCheckpoint parentCheckpoint)
        {
            var elapsedTicks = stopwatch.ElapsedTicks;
            var checkpoint = new StopwatchCheckpoint(name,
                                                     parentCheckpoint,
                                                     elapsedTicks,
                                                     elapsedTicks - parentCheckpoint.ElapsedSinceStopwatchStart);
            return checkpoint;
        }

        public static void PrintCheckpoints(this Stopwatch stopwatch, params StopwatchCheckpoint[] checkpoints)
        {
            var header = $"|-------------------------------|" + Environment.NewLine +
                         $"| >>> Stopwatch checkpoints <<< |" + Environment.NewLine +
                         $"|-------------------------------|" + Environment.NewLine +
                         $"|";
            var table = CheckpointsToString(checkpoints).TrimStart();

            Console.WriteLine(header + Environment.NewLine + table);
        }

        public static string CheckpointsToString(params StopwatchCheckpoint[] checkpoints)
        {
            var checkpointsCoerced = checkpoints
                .Select(z => new
                {
                    z.Name,
                    ParentName        = z.ParentCheckpoint?.Name,
                    CheckpointElapsed = z.ElapsedSinceParent,
                    TotalElapsed      = z.ElapsedSinceStopwatchStart
                });
            return checkpointsCoerced.ToStringTable();
        }
    }

    public class StopwatchCheckpoint
    {
        public string              Name                       { get; set; }
        public StopwatchCheckpoint ParentCheckpoint           { get; set; }
        public long                ElapsedSinceStopwatchStart { get; set; }
        public long                ElapsedSinceParent         { get; set; }

        public StopwatchCheckpoint(string name, StopwatchCheckpoint parentCheckpoint, long elapsedSinceStopwatchStart, long elapsedSinceParent)
        {
            Name                       = name;
            ParentCheckpoint           = parentCheckpoint;
            ElapsedSinceStopwatchStart = elapsedSinceStopwatchStart;
            ElapsedSinceParent         = elapsedSinceParent;
        }

        public void PrintFancy()
        {
            Console.WriteLine($"| {Name,-25} | {ElapsedSinceStopwatchStart,15:#,##} | {ElapsedSinceParent,15:#,##} |");
        }
    }

    public class StopwatchTimestampCheckpoint
    {
        private long                         Timestamp { get; set; }
        private string                       Name      { get; set; }
        private StopwatchTimestampCheckpoint Parent    { get; set; }
        private StopwatchTimestampCheckpoint Root      { get; set; }

        private long ElapsedSinceParentTicks => Timestamp - Parent.Timestamp;
        private long ElapsedSinceRootTicks   => Timestamp - Root.Timestamp;

        private double ElapsedSinceParentMs => ElapsedSinceParentTicks / (double) Stopwatch.Frequency * 1000;
        private double ElapsedSinceRootMs   => ElapsedSinceRootTicks / (double) Stopwatch.Frequency * 1000;

        public TimeSpan ElapsedSinceParentTimeSpan => TimeSpan.FromMilliseconds(ElapsedSinceParentMs);
        public TimeSpan ElapsedSinceRootTimeSpan => TimeSpan.FromMilliseconds(ElapsedSinceRootMs);


        public StopwatchTimestampCheckpoint(long timestamp, string name, StopwatchTimestampCheckpoint parent, StopwatchTimestampCheckpoint root)
        {
            Timestamp = timestamp;
            Name      = name;
            Parent    = parent;
            Root      = root;
        }

        public StopwatchTimestampCheckpoint(long timestamp, string name)
        {
            Timestamp = timestamp;
            Name      = name;
            Parent    = this;
            Root      = this;
        }

        public static StopwatchTimestampCheckpoint Origin()
        {
            return new StopwatchTimestampCheckpoint(Stopwatch.GetTimestamp(), "Origin");
        }

        public StopwatchTimestampCheckpoint Checkpoint(string checkpointName)
        {
            return new StopwatchTimestampCheckpoint(Stopwatch.GetTimestamp(), checkpointName, this, this.Root);
        }


        public static void PrintCheckpoints(params StopwatchTimestampCheckpoint[] checkpoints)
        {
            var header = $"|-------------------------------|" + Environment.NewLine +
                         $"| >>> Stopwatch checkpoints <<< |" + Environment.NewLine +
                         $"|-------------------------------|" + Environment.NewLine +
                         $"|";
            var table  = CheckpointsToString(checkpoints).TrimStart();
            var footer = "======================================================================================";

            var output = string.Join(Environment.NewLine, header, table) + footer;
            Console.WriteLine(output);
        }

        public static string CheckpointsToString(params StopwatchTimestampCheckpoint[] checkpoints)
        {
            return CheckpointsToString((IEnumerable<StopwatchTimestampCheckpoint>) checkpoints);
        }

        public static string CheckpointsToString(IEnumerable<StopwatchTimestampCheckpoint> checkpoints)
        {
            var format = "#,##.#";
            var checkpointsCoerced = checkpoints
                .Select(z => new
                {
                    CheckpointName  = z.Name,
                    // CheckpointTicks = z.ElapsedSinceParentTicks,
                    // TotalTicks      = z.ElapsedSinceRootTicks,
                    CheckpointMs    = z.ElapsedSinceParentMs.ToString(format),
                    TotalMs         = z.ElapsedSinceRootMs.ToString(format),
                    ParentName      = z.Parent?.Name,
                });
            return checkpointsCoerced.ToStringTable();
        }
    }
}