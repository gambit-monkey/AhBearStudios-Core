using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using VContainer;

namespace AhBearStudios.Core.DependencyInjection.Extensions.VContainer
{
    /// <summary>
    /// Extension methods for VContainer inspection operations.
    /// Provides functionality to examine container registrations and state.
    /// </summary>
    public static class VContainerInspectionExtensions
    {
        private static readonly FieldInfo _registrationsField;

        static VContainerInspectionExtensions()
        {
            // Use reflection to access the internal registrations collection from VContainer
            _registrationsField = typeof(ContainerBuilder).GetField("registrations",
                BindingFlags.Instance | BindingFlags.NonPublic);

            if (_registrationsField == null)
            {
                throw new InvalidOperationException(
                    "Could not find 'registrations' field in VContainer.ContainerBuilder. " +
                    "This extension might not be compatible with the current VContainer version.");
            }
        }

        /// <summary>
        /// Checks if a type is already registered in the container builder.
        /// </summary>
        /// <typeparam name="T">The type to check for registration.</typeparam>
        /// <param name="builder">The container builder.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static bool IsRegistered<T>(this IContainerBuilder builder)
        {
            return IsRegistered(builder, typeof(T));
        }

        /// <summary>
        /// Checks if a type is already registered in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="type">The type to check for registration.</param>
        /// <returns>True if the type is registered, false otherwise.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder or type is null.</exception>
        /// <exception cref="ArgumentException">Thrown when builder is not a ContainerBuilder instance.</exception>
        public static bool IsRegistered(this IContainerBuilder builder, Type type)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));
            if (type == null) throw new ArgumentNullException(nameof(type));

            // Only works with ContainerBuilder
            if (!(builder is ContainerBuilder containerBuilder))
            {
                throw new ArgumentException(
                    "IsRegistered only works with VContainer.ContainerBuilder instances",
                    nameof(builder));
            }

            try
            {
                // Get the internal registrations collection
                var registrations = _registrationsField.GetValue(containerBuilder) as IList<Registration>;
                if (registrations == null)
                {
                    return false;
                }

                // Check if the type is registered directly or as an interface/base class
                return registrations.Any(reg =>
                    reg.InterfaceTypes.Contains(type) || // Registered as interface
                    reg.ImplementationType == type); // Registered as implementation
            }
            catch (Exception)
            {
                // If we can't access registrations for any reason, assume not registered
                return false;
            }
        }

        /// <summary>
        /// Gets the count of registrations in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>The number of registrations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static int GetRegistrationCount(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!(builder is ContainerBuilder containerBuilder))
                return 0;

            try
            {
                var registrations = _registrationsField.GetValue(containerBuilder) as IList<Registration>;
                return registrations?.Count ?? 0;
            }
            catch (Exception)
            {
                return 0;
            }
        }

        /// <summary>
        /// Gets information about all registrations in the container builder.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A list of registration information.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyList<RegistrationInfo> GetRegistrationInfo(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            if (!(builder is ContainerBuilder containerBuilder))
                return new List<RegistrationInfo>();

            try
            {
                var registrations = _registrationsField.GetValue(containerBuilder) as IList<Registration>;
                if (registrations == null)
                    return new List<RegistrationInfo>();

                return registrations.Select(reg => new RegistrationInfo
                {
                    ImplementationType = reg.ImplementationType,
                    InterfaceTypes = reg.InterfaceTypes.ToArray(),
                    Lifetime = reg.Lifetime
                }).ToList();
            }
            catch (Exception)
            {
                return new List<RegistrationInfo>();
            }
        }

        /// <summary>
        /// Logs all current registrations to the Unity console for debugging purposes.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <param name="logLevel">The Unity log level to use.</param>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static void LogRegistrations(this IContainerBuilder builder, UnityEngine.LogType logLevel = UnityEngine.LogType.Log)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            var message = $"Container has {registrations.Count} registrations:\n" +
                         string.Join("\n", registrations.Select((r, i) => $"  {i + 1}. {r}"));

            switch (logLevel)
            {
                case UnityEngine.LogType.Error:
                    UnityEngine.Debug.LogError(message);
                    break;
                case UnityEngine.LogType.Warning:
                    UnityEngine.Debug.LogWarning(message);
                    break;
                case UnityEngine.LogType.Log:
                default:
                    UnityEngine.Debug.Log(message);
                    break;
            }
        }

        /// <summary>
        /// Gets a summary of container registrations grouped by lifetime.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A dictionary mapping lifetimes to registration counts.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyDictionary<Lifetime, int> GetRegistrationSummary(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            return registrations
                .GroupBy(r => r.Lifetime)
                .ToDictionary(g => g.Key, g => g.Count());
        }

        /// <summary>
        /// Gets all registered interface types.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A collection of all registered interface types.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyCollection<Type> GetRegisteredInterfaceTypes(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            return registrations
                .SelectMany(r => r.InterfaceTypes)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Gets all registered implementation types.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A collection of all registered implementation types.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyCollection<Type> GetRegisteredImplementationTypes(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            return registrations
                .Where(r => r.ImplementationType != null)
                .Select(r => r.ImplementationType)
                .Distinct()
                .ToList();
        }

        /// <summary>
        /// Checks if there are any duplicate registrations for the same interface.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        /// <returns>A dictionary of interface types that have multiple registrations.</returns>
        /// <exception cref="ArgumentNullException">Thrown when builder is null.</exception>
        public static IReadOnlyDictionary<Type, int> FindDuplicateRegistrations(this IContainerBuilder builder)
        {
            if (builder == null) throw new ArgumentNullException(nameof(builder));

            var registrations = builder.GetRegistrationInfo();
            var interfaceCounts = new Dictionary<Type, int>();

            foreach (var registration in registrations)
            {
                foreach (var interfaceType in registration.InterfaceTypes)
                {
                    interfaceCounts[interfaceType] = interfaceCounts.GetValueOrDefault(interfaceType, 0) + 1;
                }
            }

            return interfaceCounts.Where(kvp => kvp.Value > 1).ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
    }

    /// <summary>
    /// Information about a service registration.
    /// </summary>
    public sealed class RegistrationInfo
    {
        /// <summary>
        /// Gets or sets the implementation type.
        /// </summary>
        public Type ImplementationType { get; set; }

        /// <summary>
        /// Gets or sets the interface types this registration serves.
        /// </summary>
        public Type[] InterfaceTypes { get; set; }

        /// <summary>
        /// Gets or sets the lifetime of the registration.
        /// </summary>
        public Lifetime Lifetime { get; set; }

        /// <summary>
        /// Returns a string representation of this registration info.
        /// </summary>
        /// <returns>A formatted string describing the registration.</returns>
        public override string ToString()
        {
            var interfaces = InterfaceTypes?.Length > 0 
                ? string.Join(", ", InterfaceTypes.Select(t => t.Name))
                : "none";
            
            return $"{ImplementationType?.Name ?? "Factory"} -> [{interfaces}] ({Lifetime})";
        }

        /// <summary>
        /// Gets whether this registration uses a factory.
        /// </summary>
        public bool IsFactoryRegistration => ImplementationType == null;

        /// <summary>
        /// Gets the primary interface type (first in the list).
        /// </summary>
        public Type PrimaryInterfaceType => InterfaceTypes?.FirstOrDefault();

        /// <summary>
        /// Gets whether this registration serves multiple interfaces.
        /// </summary>
        public bool ServesMultipleInterfaces => InterfaceTypes?.Length > 1;
    }
}