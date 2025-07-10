using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Coroutine.Installers.VContainer;
using VContainer;

namespace AhBearStudios.Core.Coroutine.Bootstrap
{
    /// <summary>
    /// Bootstrap installer for coroutine management services.
    /// Delegates to the proper VContainer installer for service registration.
    /// </summary>
    public sealed class CoroutineServiceInstaller : IBootstrapInstaller
    {
        private readonly CoroutineInstaller _coroutineInstaller;

        /// <summary>
        /// Initializes a new instance of the CoroutineServiceInstaller.
        /// </summary>
        /// <param name="enableDebugLogging">Whether to enable debug logging.</param>
        public CoroutineServiceInstaller(bool enableDebugLogging = false)
        {
            _coroutineInstaller = new CoroutineInstaller(enableDebugLogging, true);
        }

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

            // Delegate to the proper VContainer installer
            _coroutineInstaller.Install(builder);
        }

        /// <inheritdoc />
        public void PostInstall()
        {
            // Initialization is handled by the installer's callback
            // No additional post-install steps needed
        }
    }
}