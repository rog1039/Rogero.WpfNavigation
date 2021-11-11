using Serilog;

namespace Rogero.WpfNavigation;

internal static class PerformanceTimer
{
    public static PerformanceTimerManager Start(this ILogger logger, string operationName)
    {
        var performanceTimerManager =  new PerformanceTimerManager(logger, operationName);
        performanceTimerManager.Start();
        return performanceTimerManager;
    }

    private static PerformanceTimerManager StartTimer(this ILogger logger, string operationName)
    {
        return new PerformanceTimerManager(logger, operationName);
    }

    public sealed class PerformanceTimerManager : IDisposable
    {
        private readonly ILogger _logger;
        private readonly string  _operationName;
        private readonly Guid    _perfTimerId = Guid.NewGuid();

        private          StopwatchTimestampCheckpoint        _originCheckpoint;
        private          StopwatchTimestampCheckpoint        _lastCheckpoint;
        private readonly IList<StopwatchTimestampCheckpoint> _checkpoints = new List<StopwatchTimestampCheckpoint>();

        public PerformanceTimerManager(ILogger logger, string operationName)
        {
            _operationName = operationName;
            _logger = logger
                .ForContext("Operation",   operationName)
                .ForContext("PerfTimerId", _perfTimerId);
        }

        public void Start()
        {
            _originCheckpoint = _lastCheckpoint = StopwatchTimestampCheckpoint.Origin();
            _checkpoints.Add(_originCheckpoint);
            _logger.Information($"Starting operation: {_operationName}");
        }

        public void Checkpoint(string checkpointName)
        {
            if (_originCheckpoint == null) throw new InvalidOperationException("Origin checkpoint is null. Did you make a call to Start() first?");
                
            CheckpointInternal(checkpointName);
                
            _logger
                .Information(
                    "Checkpoint: {CheckpointName}: Total elapsed: {TotalElapsed} [Since last checkpoint: {TimeSinceLastCheckpoint}]",
                    checkpointName,
                    _lastCheckpoint.ElapsedSinceRootTimeSpan,
                    _lastCheckpoint.ElapsedSinceParentTimeSpan);
        }

        private void CheckpointInternal(string checkpointName)
        {
            _lastCheckpoint = _lastCheckpoint.Checkpoint(checkpointName);
            _checkpoints.Add(_lastCheckpoint);
        }

        public IDisposable TimeSubOperation(string subOperationName)
        {
            var timer = new PerformanceTimerManager(_logger, subOperationName);
            timer.Start();
            timer.Checkpoint($"Starting {subOperationName}");
            return timer;
        }

        private void Dispose(bool disposing)
        {
            if (disposing)
            {
                Checkpoint($"Finished");
                WriteStopwatchTableResultToLog();
            }
        }

        private void WriteStopwatchTableResultToLog()
        {
            var table = StopwatchTimestampCheckpoint.CheckpointsToString(_checkpoints);
            _logger
                .Information(
                    "Total Time: {Elapsed}  stopwatch table for operation, {OperationName}:\r\n{TimingsTable}",
                    _lastCheckpoint.ElapsedSinceRootTimeSpan,
                    _operationName,
                    table
                );
        }

        public void Dispose()
        {
            Dispose(true);
        }
            
        // public void Checkpoint<T1>(string checkpointName, T1 property)
        // {
        //     if (_originCheckpoint == null) throw new InvalidOperationException("Origin checkpoint is null. Did you make a call to Start() first?");
        //     
        //     CheckpointInternal(checkpointName);
        //     
        //     _logger
        //         .Information(
        //             "Checkpoint: {CheckpointName}: Total elapsed: {TotalElapsed} [Since last checkpoint: {TimeSinceLastCheckpoint}]",
        //             checkpointName,
        //             _lastCheckpoint.ElapsedSinceRootTimeSpan,
        //             _lastCheckpoint.ElapsedSinceParentTimeSpan);
        // }

        public void Checkpoint(string checkpointName, string message, params object[] items)
        {
            if (_originCheckpoint == null) throw new InvalidOperationException("Origin checkpoint is null. Did you make a call to Start() first?");
                
            CheckpointInternal(checkpointName);

            var paramCount = 3 + items.Length;
            var logParams  = new object[paramCount];
            logParams[0] = checkpointName;
            logParams[1] = _lastCheckpoint.ElapsedSinceRootTimeSpan;
            logParams[2] = _lastCheckpoint.ElapsedSinceParentTimeSpan;
            for (int i = 0; i < items.Length; i++)
            {
                logParams[i + 3] = items[i];
            }
                
                
            _logger
                .Information(
                    "Checkpoint: {CheckpointName}: Total elapsed: {TotalElapsed} [Since last checkpoint: {TimeSinceLastCheckpoint}] " + message,
                    logParams
                );
        }

    }
}