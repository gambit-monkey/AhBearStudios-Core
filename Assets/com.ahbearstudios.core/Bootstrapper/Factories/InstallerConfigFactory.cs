using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Bootstrap.Builders;

namespace AhBearStudios.Core.Bootstrap.Factories
{
    /// <summary>
    /// Factory for creating common installer configurations using the builder pattern.
    /// </summary>
    public static class InstallerConfigFactory
    {
        /// <summary>
        /// Creates a standard configuration with all core systems and common optional systems.
        /// </summary>
        /// <returns>A standard installer configuration</returns>
        public static IInstallerConfig CreateStandardConfig()
        {
            return new InstallerConfigBuilder()
                .WithCoreSystems(true, true, true, true)
                .WithOptionalSystems(false, true, true, true, true, true)
                .WithDevelopmentSystems(false, false, false, false, false)
                .OptimizeForCurrentPlatform()
                .Build();
        }

        /// <summary>
        /// Creates a development configuration with all debugging tools enabled.
        /// </summary>
        /// <returns>A development-focused installer configuration</returns>
        public static IInstallerConfig CreateDevelopmentConfig()
        {
            return new InstallerConfigBuilder()
                .WithDevelopmentOptimizations()
                .Build();
        }

        /// <summary>
        /// Creates a minimal configuration suitable for performance-critical environments.
        /// </summary>
        /// <returns>A minimal installer configuration</returns>
        public static IInstallerConfig CreateMinimalConfig()
        {
            return new InstallerConfigBuilder()
                .WithMinimalSystems()
                .OptimizeForCurrentPlatform()
                .Build();
        }

        /// <summary>
        /// Creates a configuration optimized for mobile platforms.
        /// </summary>
        /// <returns>A mobile-optimized installer configuration</returns>
        public static IInstallerConfig CreateMobileConfig()
        {
            return new InstallerConfigBuilder()
                .WithMobileOptimizations()
                .Build();
        }

        /// <summary>
        /// Creates a configuration optimized for console platforms.
        /// </summary>
        /// <returns>A console-optimized installer configuration</returns>
        public static IInstallerConfig CreateConsoleConfig()
        {
            return new InstallerConfigBuilder()
                .WithConsoleOptimizations()
                .Build();
        }

        /// <summary>
        /// Creates a configuration optimized for PC platforms.
        /// </summary>
        /// <returns>A PC-optimized installer configuration</returns>
        public static IInstallerConfig CreatePCConfig()
        {
            return new InstallerConfigBuilder()
                .WithPCOptimizations()
                .Build();
        }

        /// <summary>
        /// Creates a full configuration with all systems enabled.
        /// </summary>
        /// <returns>A complete installer configuration with all systems</returns>
        public static IInstallerConfig CreateFullConfig()
        {
            return new InstallerConfigBuilder()
                .WithCoreSystems(true, true, true, true)
                .WithOptionalSystems(true, true, true, true, true, true)
                .WithDevelopmentSystems(true, true, true, true, true)
                .WithPlatformSystems(true, true, true, true)
                .WithThirdPartyIntegrations(true, true, true, true)
                .WithValidation(true, true, true)
                .Build();
        }
    }
}