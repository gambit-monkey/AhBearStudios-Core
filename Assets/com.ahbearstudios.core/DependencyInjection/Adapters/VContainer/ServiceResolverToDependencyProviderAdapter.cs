using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.DependencyInjection.Models;

namespace AhBearStudios.Core.DependencyInjection.Adapters.VContainer;

/// <summary>
/// Adapter that converts IServiceResolver to IDependencyProvider for factory method compatibility.
/// </summary>
internal sealed class ServiceResolverToDependencyProviderAdapter : IDependencyProvider
{
    private readonly IServiceResolver _serviceResolver;

    public ContainerFramework Framework => _serviceResolver.Framework;
    public bool IsDisposed => _serviceResolver.IsDisposed;

    public ServiceResolverToDependencyProviderAdapter(IServiceResolver serviceResolver)
    {
        _serviceResolver = serviceResolver ?? throw new ArgumentNullException(nameof(serviceResolver));
    }

    public T Resolve<T>()
    {
        return _serviceResolver.Resolve<T>();
    }

    public bool TryResolve<T>(out T service)
    {
        return _serviceResolver.TryResolve<T>(out service);
    }

    public T ResolveOrDefault<T>(T defaultValue = default)
    {
        return _serviceResolver.ResolveOrDefault<T>(defaultValue);
    }
}