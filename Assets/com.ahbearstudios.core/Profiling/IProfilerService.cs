namespace AhBearStudios.Core.Profiling;

/// <summary>
/// Placeholder interface for profiler service integration.
/// </summary>
public interface IProfilerService
{
    /// <summary>
    /// Begins a profiling scope.
    /// </summary>
    /// <param name="tag">The profiling tag</param>
    /// <returns>Disposable profiling scope</returns>
    IDisposable BeginScope(string tag);
}