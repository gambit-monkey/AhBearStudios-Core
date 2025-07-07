using AhBearStudios.Core.DependencyInjection.Configuration;

namespace AhBearStudios.Core.DependencyInjection.Models
{
    /// <summary>
    /// Represents a service registration for validation purposes.
    /// Framework-agnostic representation of a dependency registration.
    /// </summary>
    public sealed class ServiceRegistration
    {
        /// <summary>
        /// Gets the service interface type.
        /// </summary>
        public Type ServiceType { get; }
        
        /// <summary>
        /// Gets the implementation type (null for factory registrations).
        /// </summary>
        public Type ImplementationType { get; }
        
        /// <summary>
        /// Gets the service lifetime.
        /// </summary>
        public ServiceLifetime Lifetime { get; }
        
        /// <summary>
        /// Gets whether this is a factory registration.
        /// </summary>
        public bool IsFactory { get; }
        
        /// <summary>
        /// Gets whether this is an instance registration.
        /// </summary>
        public bool IsInstance { get; }
        
        /// <summary>
        /// Gets the registration name (for named services).
        /// </summary>
        public string Name { get; }
        
        /// <summary>
        /// Gets additional registration metadata.
        /// </summary>
        public object Metadata { get; }
        
        /// <summary>
        /// Initializes a new service registration.
        /// </summary>
        public ServiceRegistration(
            Type serviceType,
            Type implementationType = null,
            ServiceLifetime lifetime = ServiceLifetime.Transient,
            bool isFactory = false,
            bool isInstance = false,
            string name = null,
            object metadata = null)
        {
            ServiceType = serviceType ?? throw new ArgumentNullException(nameof(serviceType));
            ImplementationType = implementationType;
            Lifetime = lifetime;
            IsFactory = isFactory;
            IsInstance = isInstance;
            Name = name;
            Metadata = metadata;
        }
        
        /// <summary>
        /// Creates a registration for a concrete implementation.
        /// </summary>
        public static ServiceRegistration ForImplementation<TService, TImplementation>(
            ServiceLifetime lifetime = ServiceLifetime.Transient,
            string name = null)
            where TImplementation : class, TService
        {
            return new ServiceRegistration(
                typeof(TService),
                typeof(TImplementation),
                lifetime,
                false,
                false,
                name);
        }
        
        /// <summary>
        /// Creates a registration for a factory.
        /// </summary>
        public static ServiceRegistration ForFactory<TService>(
            ServiceLifetime lifetime = ServiceLifetime.Transient,
            string name = null)
        {
            return new ServiceRegistration(
                typeof(TService),
                null,
                lifetime,
                true,
                false,
                name);
        }
        
        /// <summary>
        /// Creates a registration for an instance.
        /// </summary>
        public static ServiceRegistration ForInstance<TService>(string name = null)
        {
            return new ServiceRegistration(
                typeof(TService),
                null,
                ServiceLifetime.Instance,
                false,
                true,
                name);
        }
        
        /// <summary>
        /// Returns a string representation of this registration.
        /// </summary>
        public override string ToString()
        {
            var impl = ImplementationType?.Name ?? (IsFactory ? "Factory" : IsInstance ? "Instance" : "Unknown");
            var nameText = !string.IsNullOrEmpty(Name) ? $" (Name: {Name})" : "";
            return $"{ServiceType.Name} -> {impl} ({Lifetime}){nameText}";
        }
        
        /// <summary>
        /// Determines equality based on service type, implementation type, and name.
        /// </summary>
        public override bool Equals(object obj)
        {
            if (obj is ServiceRegistration other)
            {
                return ServiceType == other.ServiceType &&
                       ImplementationType == other.ImplementationType &&
                       string.Equals(Name, other.Name, StringComparison.Ordinal);
            }
            return false;
        }
        
        /// <summary>
        /// Gets a hash code for this registration.
        /// </summary>
        public override int GetHashCode()
        {
            var hash = ServiceType.GetHashCode();
            if (ImplementationType != null)
                hash = hash * 23 + ImplementationType.GetHashCode();
            if (Name != null)
                hash = hash * 23 + Name.GetHashCode();
            return hash;
        }
    }
}