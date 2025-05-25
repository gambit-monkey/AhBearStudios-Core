namespace AhBearStudios.Pooling.Core
{
    public enum PoolThreadingMode
    {
        /// <summary>
        /// Single-threaded pool optimized for performance in single-threaded scenarios.
        /// Use when:
        /// - All pool operations occur on the main thread only
        /// - Maximum performance is needed and thread safety is not a concern
        /// - Used in simple Unity MonoBehaviour scripts that don't use threading
        /// - Minimizing overhead is critical
        /// Does not provide any thread safety guarantees and will fail if accessed from multiple threads.
        /// </summary>
        SingleThreaded,

        /// <summary>
        /// Thread-safe pool using synchronization locks suitable for multi-threaded environments.
        /// Use when:
        /// - Pool needs to be accessed from multiple threads (including Tasks, ThreadPool, or custom threads)
        /// - Safety is more important than absolute performance
        /// - Working with asynchronous operations or background processing
        /// - When implementing worker thread patterns or parallel processing
        /// Provides full thread safety at the cost of some performance overhead due to locking mechanisms.
        /// </summary>
        ThreadSafe,

        /// <summary>
        /// Thread-local pool that maintains separate pools for each thread.
        /// Use when:
        /// - Executing parallel operations where each thread needs its own pool instances
        /// - Minimizing lock contention in highly parallel operations
        /// - Working with fixed thread count scenarios (like a dedicated worker thread pool)
        /// - Need near single-threaded performance with thread isolation
        /// Provides excellent performance by eliminating contention, but uses more memory since each thread
        /// has its own separate pool. Best for scenarios with a small, fixed number of threads.
        /// </summary>
        ThreadLocal,

        /// <summary>
        /// Job-compatible pool designed specifically for Unity's job system.
        /// Use when:
        /// - Working with Unity's Job System (IJob, IJobParallelFor, etc.)
        /// - Processing data in Burst-compiled jobs 
        /// - Pooling blittable types or unmanaged value types
        /// - Need to maintain safety when passing pool data between main thread and jobs
        /// Optimized for high-performance, low-garbage data processing in DOTS workflows.
        /// Compatible with Unity's safety system and designed to work with Burst compilation.
        /// </summary>
        JobCompatible
    }
}