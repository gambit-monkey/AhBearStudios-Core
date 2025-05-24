using AhBearStudios.Core.DependencyInjection.Adapters;
using AhBearStudios.Core.DependencyInjection.Interfaces;

namespace AhBearStudios.Core.DependencyInjection.Configuration
{
    public static class DependencyInjectionConfiguration
    {
        private static IDependencyInjectionAttribute _currentImplementation = new VContainerInjectAttributeAdapter();

        public static void SetImplementation(IDependencyInjectionAttribute implementation)
        {
            _currentImplementation = implementation;
        }

        public static IDependencyInjectionAttribute GetCurrentImplementation()
        {
            return _currentImplementation;
        }
    }
}