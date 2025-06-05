using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Coroutine.Factories;
using AhBearStudios.Core.Coroutine.Interfaces;
using AhBearStudios.Core.Coroutine.Unity;
using VContainer;

namespace AhBearStudios.Core.Coroutine.Bootstrap
{
    /// <summary>
    /// Bootstrap installer for coroutine management services.
    /// Registers coroutine-related services with the dependency injection container.
    /// </summary>
    public sealed class CoroutineServiceInstaller : IBootstrapInstaller
    {
        /// <inheritdoc />
        public string InstallerName => "Coroutine Services";

        /// <inheritdoc />
        public int Priority => 100; // Early installation

        /// <inheritdoc />
        public bool IsEnabled => true;

        /// <inheritdoc />
        public System.Type[] Dependencies => System.Array.Empty<System.Type>();

        /// <inheritdoc />
        public bool ValidateInstaller()
        {
            return true;
        }

        /// <inheritdoc />
        public void PreInstall()
        {
            // No pre-installation setup required
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
                throw new System.ArgumentNullException(nameof(builder));

            // Register the coroutine manager as singleton
            builder.Register<CoreCoroutineManager>(Lifetime.Singleton)
                .AsImplementedInterfaces()
                .AsSelf();

            // Register factory for creating coroutine runners
            builder.Register<ICoroutineRunnerFactory, CoroutineRunnerFactory>(Lifetime.Singleton);

            // The default coroutine runner will be registered by the manager during initialization
        }

        /// <inheritdoc />
        public void PostInstall()
        {
            // Initialize the coroutine manager
            var manager = CoreCoroutineManager.Instance;
            manager.Initialize();
        }
    }
}