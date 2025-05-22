namespace AhBearStudios.Core.DependencyInjection.Interfaces
{
    /// <summary>
    /// Interface for abstracting dependency injection containers.
    /// </summary>
    public interface IDependencyProvider
    {
        /// <summary>
        /// Resolves a service of the specified type from the container.
        /// </summary>
        /// <typeparam name="T">The type of service to resolve.</typeparam>
        /// <returns>The resolved service.</returns>
        T Resolve<T>();
    }
}