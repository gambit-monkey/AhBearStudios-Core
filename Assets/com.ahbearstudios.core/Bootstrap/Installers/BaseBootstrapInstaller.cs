using AhBearStudios.Core.Bootstrap.Interfaces;
using AhBearStudios.Core.Logging.Interfaces;
using AhBearStudios.Core.Bootstrap.Models;
using Unity.Collections;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    /// <summary>
    /// Base implementation of IBootstrapInstaller providing common functionality for all installers.
    /// Integrates with all core systems: logging, message bus, health checks, alerts, and profiling.
    /// Provides robust error handling, performance monitoring, and comprehensive system integration.
    /// </summary>
    public abstract partial class BaseBootstrapInstaller : IBootstrapInstaller
    {
        #region Core Properties

        /// <inheritdoc />
        public abstract string InstallerName { get; }

        /// <inheritdoc />
        public abstract int Priority { get; }

        /// <inheritdoc />
        public virtual bool IsEnabled => true;

        /// <inheritdoc />
        public virtual Type[] Dependencies => Array.Empty<Type>();

        /// <inheritdoc />
        public virtual long EstimatedMemoryOverheadBytes => 1024; // 1KB default

        /// <inheritdoc />
        public virtual bool SupportsHotReload => true;

        /// <inheritdoc />
        public abstract SystemCategory Category { get; }

        #endregion

        #region Private Fields

        private readonly object _metricsLock = new object();
        private InstallationMetrics _metrics;
        private SystemHealthStatus _healthStatus;
        private bool _isInstalled;
        private bool _isDisposed;
        private ILoggingService _logger;
        private FixedString64Bytes _correlationId;

        #endregion

        #region Protected Properties

        /// <summary>
        /// Gets whether this installer has been successfully installed.
        /// </summary>
        protected bool IsInstalled => _isInstalled;

        /// <summary>
        /// Gets the logger instance for this installer.
        /// </summary>
        protected ILoggingService Logger => _logger;

        /// <summary>
        /// Gets the correlation ID for tracking this installation session.
        /// </summary>
        protected FixedString64Bytes CorrelationId => _correlationId;

        #endregion

        #region Constructor

        /// <summary>
        /// Initializes a new instance of the BaseBootstrapInstaller.
        /// </summary>
        protected BaseBootstrapInstaller()
        {
            InitializeMetrics();
            InitializeHealthStatus();
        }

        #endregion
    }
}