namespace AhBearStudios.Core.Pooling;

/// <summary>
/// Placeholder interface for pooling service integration.
/// </summary>
public interface IPoolingService
{
    /// <summary>
    /// Gets a service instance from the pool.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <returns>The service instance</returns>
    T GetService<T>() where T : class;

    /// <summary>
    /// Registers a service with the pool.
    /// </summary>
    /// <typeparam name="T">The service type</typeparam>
    /// <param name="factory">The factory function</param>
    void RegisterService<T>(Func<T> factory) where T : class;
}