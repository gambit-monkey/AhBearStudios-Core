using Unity.Profiling;

namespace AhBearStudios.Core.Profiling.Tagging
{
    /// <summary>
    /// Predefined profiler tags for coroutine operations
    /// </summary>
    public static class CoroutineProfilerTags
    {
        // Coroutine category
        private static readonly ProfilerCategory CoroutineCategory = ProfilerCategory.Scripts;
        
        // Common operation tags
        public static readonly ProfilerTag CoroutineStart = new ProfilerTag(CoroutineCategory, "Coroutine.Start");
        public static readonly ProfilerTag CoroutineExecute = new ProfilerTag(CoroutineCategory, "Coroutine.Execute");
        public static readonly ProfilerTag CoroutineComplete = new ProfilerTag(CoroutineCategory, "Coroutine.Complete");
        public static readonly ProfilerTag CoroutineCancel = new ProfilerTag(CoroutineCategory, "Coroutine.Cancel");
        public static readonly ProfilerTag CoroutineCleanup = new ProfilerTag(CoroutineCategory, "Coroutine.Cleanup");
        public static readonly ProfilerTag CoroutineTimeout = new ProfilerTag(CoroutineCategory, "Coroutine.Timeout");
        
        // Runner operation tags
        public static readonly ProfilerTag RunnerCreate = new ProfilerTag(CoroutineCategory, "Runner.Create");
        public static readonly ProfilerTag RunnerDispose = new ProfilerTag(CoroutineCategory, "Runner.Dispose");
        public static readonly ProfilerTag RunnerPause = new ProfilerTag(CoroutineCategory, "Runner.Pause");
        public static readonly ProfilerTag RunnerResume = new ProfilerTag(CoroutineCategory, "Runner.Resume");
        
        // Batch operation tags
        public static readonly ProfilerTag BatchStart = new ProfilerTag(CoroutineCategory, "Batch.Start");
        public static readonly ProfilerTag BatchCancel = new ProfilerTag(CoroutineCategory, "Batch.Cancel");
        public static readonly ProfilerTag BatchComplete = new ProfilerTag(CoroutineCategory, "Batch.Complete");
        
        /// <summary>
        /// Creates a runner-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="runnerId">Runner identifier (shortened GUID)</param>
        /// <returns>A profiler tag for the specific runner operation</returns>
        public static ProfilerTag ForRunner(string operationType, System.Guid runnerId)
        {
            string guidPrefix = runnerId.ToString().Substring(0, 8);
            return new ProfilerTag(CoroutineCategory, $"Runner.{guidPrefix}.{operationType}");
        }
        
        /// <summary>
        /// Creates a runner-specific profiler tag using only the runner name
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="runnerName">Name of the runner</param>
        /// <returns>A profiler tag for the specific runner operation</returns>
        public static ProfilerTag ForRunnerName(string operationType, string runnerName)
        {
            return new ProfilerTag(CoroutineCategory, $"Runner.{runnerName}.{operationType}");
        }
        
        /// <summary>
        /// Creates a coroutine-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="coroutineId">Coroutine identifier</param>
        /// <returns>A profiler tag for the specific coroutine operation</returns>
        public static ProfilerTag ForCoroutine(string operationType, int coroutineId)
        {
            return new ProfilerTag(CoroutineCategory, $"Coroutine.{coroutineId}.{operationType}");
        }
        
        /// <summary>
        /// Creates a tag-specific profiler tag
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <param name="tagName">Name of the coroutine tag</param>
        /// <returns>A profiler tag for coroutines with the specified tag</returns>
        public static ProfilerTag ForTag(string operationType, string tagName)
        {
            return new ProfilerTag(CoroutineCategory, $"Tag.{tagName}.{operationType}");
        }
        
        /// <summary>
        /// Gets the appropriate tag for the given operation type
        /// </summary>
        /// <param name="operationType">Type of operation</param>
        /// <returns>A profiler tag for the operation</returns>
        public static ProfilerTag ForOperation(string operationType)
        {
            switch (operationType.ToLowerInvariant())
            {
                case "start":
                    return CoroutineStart;
                case "execute":
                    return CoroutineExecute;
                case "complete":
                    return CoroutineComplete;
                case "cancel":
                    return CoroutineCancel;
                case "cleanup":
                    return CoroutineCleanup;
                case "timeout":
                    return CoroutineTimeout;
                case "runnercreate":
                    return RunnerCreate;
                case "runnerdispose":
                    return RunnerDispose;
                case "runnerpause":
                    return RunnerPause;
                case "runnerresume":
                    return RunnerResume;
                case "batchstart":
                    return BatchStart;
                case "batchcancel":
                    return BatchCancel;
                case "batchcomplete":
                    return BatchComplete;
                default:
                    return new ProfilerTag(CoroutineCategory, $"Coroutine.{operationType}");
            }
        }
    }
}