using System;
using System.Linq;
using UnityEngine;
using AhBearStudios.Core.HealthCheck.Interfaces;

namespace AhBearStudios.Core.HealthCheck.Discovery
{
    /// <summary>
    /// Discovers and registers all IAutoRegisteringHealthCheck implementations.
    /// </summary>
    public static class HealthCheckAutoDiscovery
    {
        public static void RegisterAutoChecks(
            IHealthCheckRegistry registry,
            IServiceProvider services = null)
        {
            var marker = typeof(IAutoRegisteringHealthCheck);
            var all = AppDomain.CurrentDomain.GetAssemblies()
                .SelectMany(a => a.GetTypes())
                .Where(t => !t.IsAbstract && !t.IsInterface && marker.IsAssignableFrom(t));

            foreach (var type in all)
            {
                try
                {
                    var instance = services != null
                        ? (IAutoRegisteringHealthCheck)services.GetService(type)
                        : (IAutoRegisteringHealthCheck)Activator.CreateInstance(type);

                    registry.Register(instance);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[HealthCheckAutoDiscovery] Failed to instantiate '{type.FullName}': {ex.Message}");
                }
            }
        }
    }
}