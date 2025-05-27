using System;
using AhBearStudios.Core.Profiling.Interfaces;
using AhBearStudios.Core.Profiling.Profilers;
using AhBearStudios.Core.Profiling.Factories;
using AhBearStudios.Core.Profiling.Metrics;
using AhBearStudios.Core.Profiling.Metrics.Serialization;
using AhBearStudios.Core.Profiling.Data;
using AhBearStudios.Core.Profiling.Unity;
using AhBearStudios.Core.MessageBus.Interfaces;
using AhBearStudios.Core.DependencyInjection.Interfaces;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Unity.Configuration;
using VContainer;
using VContainer.Unity;

namespace AhBearStudios.Core.DependencyInjection.Installers.VContainer
{
    /// <summary>
    /// VContainer installer for the profiling system.
    /// Registers all required interfaces and their implementations for proper dependency injection.
    /// </summary>
    public sealed class ProfilingInstaller : IInstaller
    {
        private readonly ProfilerConfiguration _configuration;
        private readonly bool _enableRuntimeProfiling;
        private readonly bool _enablePoolMetrics;
        private readonly bool _enableSerializationMetrics;

        /// <summary>
        /// Initializes a new instance of the ProfilingInstaller class with default configuration.
        /// </summary>
        public ProfilingInstaller() : this(null, true, true, true)
        {
        }

        /// <summary>
        /// Initializes a new instance of the ProfilingInstaller class with the specified configuration.
        /// </summary>
        /// <param name="configuration">The configuration to use for the profiling system.</param>
        /// <param name="enableRuntimeProfiling">Whether to enable runtime profiling components.</param>
        /// <param name="enablePoolMetrics">Whether to enable pool metrics tracking.</param>
        /// <param name="enableSerializationMetrics">Whether to enable serialization metrics tracking.</param>
        public ProfilingInstaller(
            ProfilerConfiguration configuration,
            bool enableRuntimeProfiling = true,
            bool enablePoolMetrics = true,
            bool enableSerializationMetrics = true)
        {
            _configuration = configuration;
            _enableRuntimeProfiling = enableRuntimeProfiling;
            _enablePoolMetrics = enablePoolMetrics;
            _enableSerializationMetrics = enableSerializationMetrics;
        }

        /// <inheritdoc />
        public void Install(IContainerBuilder builder)
        {
            if (builder == null)
            {
                throw new ArgumentNullException(nameof(builder));
            }

            // Register configuration
            RegisterConfiguration(builder);

            // Register core profiling interfaces
            RegisterCoreProfilers(builder);

            // Register metrics systems
            RegisterMetricsSystems(builder);

            // Register factories
            RegisterFactories(builder);

            // Register specialized profilers
            RegisterSpecializedProfilers(builder);

            // Register runtime components if enabled
            if (_enableRuntimeProfiling)
            {
                RegisterRuntimeComponents(builder);
            }

            // Configure build callbacks for initialization
            RegisterBuildCallbacks(builder);
        }

        /// <summary>
        /// Registers the profiler configuration.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterConfiguration(IContainerBuilder builder)
        {
            if (_configuration != null)
            {
                builder.RegisterInstance(_configuration);
            }
            else
            {
                // Create default configuration if none provided
                builder.Register<ProfilerConfiguration>(container =>
                {
                    var config = UnityEngine.ScriptableObject.CreateInstance<ProfilerConfiguration>();
                    config.InitializeDefaults();
                    return config;
                }, Lifetime.Singleton);
            }
        }

        /// <summary>
        /// Registers core profiling interfaces and implementations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterCoreProfilers(IContainerBuilder builder)
        {
            // Register profiler factory
            builder.RegisterIfNotPresent<ProfilerFactory>(Lifetime.Singleton);

            // Register the main profiler interface
            builder.Register<IProfiler>(container =>
            {
                var messageBus = container.Resolve<IMessageBus>();
                var dependencyProvider = container.Resolve<IDependencyProvider>();
                var factory = new ProfilerFactory(dependencyProvider);
                
                return factory.CreateProfiler();
            }, Lifetime.Singleton);

            // Register null profiler as fallback
            builder.RegisterIfNotPresent<NullProfiler>(Lifetime.Singleton);

            // Register stats collection
            builder.RegisterIfNotPresent<ProfilerStatsCollection>(Lifetime.Singleton);

            // Register system metrics tracker
            builder.Register<SystemMetricsTracker>(container =>
            {
                var config = container.Resolve<ProfilerConfiguration>();
                return new SystemMetricsTracker(config.UpdateInterval);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers metrics systems based on configuration.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterMetricsSystems(IContainerBuilder builder)
        {
            // Register pool metrics if enabled
            if (_enablePoolMetrics)
            {
                RegisterPoolMetrics(builder);
            }

            // Register serialization metrics if enabled
            if (_enableSerializationMetrics)
            {
                RegisterSerializationMetrics(builder);
            }

            // Register threshold alert system
            builder.Register<ThresholdAlertSystem>(container =>
            {
                var profileManager = container.Resolve<IProfilerManager>();
                var messageBus = container.Resolve<IMessageBus>();
                var config = container.Resolve<ProfilerConfiguration>();
                
                return new ThresholdAlertSystem(profileManager, messageBus, config.LogAlertsToConsole);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers pool metrics components.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterPoolMetrics(IContainerBuilder builder)
        {
            // Register pool metrics interfaces
            builder.Register<IPoolMetrics>(container =>
            {
                var messageBus = container.Resolve<IMessageBus>();
                return PoolMetricsFactory.CreateStandard(messageBus);
            }, Lifetime.Singleton);

            // Register native pool metrics
            builder.Register<INativePoolMetrics>(container =>
            {
                return PoolMetricsFactory.CreateNative();
            }, Lifetime.Singleton);

            // Register pool metrics alert adapter
            builder.Register<NativePoolMetricsAlertAdapter>(container =>
            {
                var nativeMetrics = container.Resolve<INativePoolMetrics>();
                var alertSystem = container.Resolve<ThresholdAlertSystem>();
                var messageBus = container.Resolve<IMessageBus>();
                
                return new NativePoolMetricsAlertAdapter(
                    nativeMetrics as NativePoolMetrics, 
                    alertSystem, 
                    messageBus);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers serialization metrics components.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterSerializationMetrics(IContainerBuilder builder)
        {
            // Register individual serializer metrics
            builder.Register<SerializerMetricsData>(Lifetime.Singleton);

            // Register composite serializer metrics
            builder.Register<ISerializerMetrics, CompositeSerializerMetrics>(container =>
            {
                var individualMetrics = container.Resolve<SerializerMetricsData>();
                var metricsList = new System.Collections.Generic.List<ISerializerMetrics> { individualMetrics };
                return new CompositeSerializerMetrics(metricsList);
            }, Lifetime.Singleton);

            // Register null serializer metrics as fallback
            builder.RegisterIfNotPresent<NullSerializerMetrics>(Lifetime.Singleton);
        }

        /// <summary>
        /// Registers factory components.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterFactories(IContainerBuilder builder)
        {
            // Register profiler factory with dependencies
            builder.Register<ProfilerFactory>(container =>
            {
                var dependencyProvider = container.Resolve<IDependencyProvider>();
                return new ProfilerFactory(dependencyProvider);
            }, Lifetime.Singleton);

            // Register pool metrics factory (static methods, so we register a delegate)
            builder.Register<Func<IMessageBus, int, IPoolMetrics>>(container =>
            {
                return (messageBus, capacity) => PoolMetricsFactory.CreateStandard(messageBus, capacity);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers specialized profiler implementations.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterSpecializedProfilers(IContainerBuilder builder)
        {
            // Register pool profiler
            if (_enablePoolMetrics)
            {
                builder.Register<PoolProfiler>(container =>
                {
                    var baseProfiler = container.Resolve<IProfiler>();
                    var poolMetrics = container.Resolve<IPoolMetrics>();
                    var messageBus = container.Resolve<IMessageBus>();
                    
                    return new PoolProfiler(baseProfiler, poolMetrics, messageBus);
                }, Lifetime.Singleton);
            }

            // Register serialization profiler
            if (_enableSerializationMetrics)
            {
                builder.Register<SerializationProfiler>(container =>
                {
                    var baseProfiler = container.Resolve<IProfiler>();
                    var serializerMetrics = container.Resolve<ISerializerMetrics>();
                    var messageBus = container.Resolve<IMessageBus>();
                    
                    return new SerializationProfiler(baseProfiler, serializerMetrics, messageBus);
                }, Lifetime.Singleton);
            }
        }

        /// <summary>
        /// Registers runtime Unity components if enabled.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterRuntimeComponents(IContainerBuilder builder)
        {
            // Register profiler manager interface
            builder.Register<IProfilerManager>(container =>
            {
                // Find existing ProfileManager in scene or create one
                var existing = UnityEngine.Object.FindObjectOfType<ProfileManager>();
                if (existing != null)
                {
                    return existing;
                }

                // Create new ProfileManager GameObject
                var go = new UnityEngine.GameObject("[ProfileManager]");
                UnityEngine.Object.DontDestroyOnLoad(go);
                var profileManager = go.AddComponent<ProfileManager>();
                
                return profileManager;
            }, Lifetime.Singleton);

            // Register dependency provider bridge
            builder.Register<IDependencyProvider>(container =>
            {
                return new VContainerDependencyProvider(container);
            }, Lifetime.Singleton);
        }

        /// <summary>
        /// Registers build callbacks for post-construction initialization.
        /// </summary>
        /// <param name="builder">The container builder.</param>
        private void RegisterBuildCallbacks(IContainerBuilder builder)
        {
            builder.RegisterBuildCallback(container =>
            {
                try
                {
                    InitializeProfilerSystem(container);
                }
                catch (Exception ex)
                {
                    UnityEngine.Debug.LogError($"[ProfilingInstaller] Failed to initialize profiler system: {ex.Message}");
                }
            });
        }

        /// <summary>
        /// Initializes the profiler system after container build.
        /// </summary>
        /// <param name="container">The resolved container.</param>
        private void InitializeProfilerSystem(IObjectResolver container)
        {
            // Initialize system metrics tracker
            var systemMetrics = container.Resolve<SystemMetricsTracker>();
            if (_configuration?.EnableProfiling == true)
            {
                systemMetrics.RegisterDefaultMetrics();
                systemMetrics.Start();
            }

            // Initialize threshold alert system
            var alertSystem = container.Resolve<ThresholdAlertSystem>();
            if (_configuration?.EnableProfiling == true)
            {
                alertSystem.Start();
            }

            // Initialize profiler manager if available
            if (container.TryResolve<IProfilerManager>(out var profilerManager))
            {
                if (_configuration?.EnableProfiling == true)
                {
                    profilerManager.StartProfiling();
                }
            }

            // Initialize pool metrics alert adapter if enabled
            if (_enablePoolMetrics && container.TryResolve<NativePoolMetricsAlertAdapter>(out var alertAdapter))
            {
                // Start processing alerts in a coroutine or update loop
                if (UnityEngine.Application.isPlaying)
                {
                    StartAlertProcessing(alertAdapter);
                }
            }
        }

        /// <summary>
        /// Starts alert processing for pool metrics.
        /// </summary>
        /// <param name="alertAdapter">The alert adapter to process.</param>
        private void StartAlertProcessing(NativePoolMetricsAlertAdapter alertAdapter)
        {
            // Find or create a MonoBehaviour to handle updates
            var updateHandler = UnityEngine.Object.FindObjectOfType<ProfileManager>();
            if (updateHandler != null)
            {
                // Subscribe to update events or use a coroutine
                var coroutineRunner = updateHandler.StartCoroutine(AlertProcessingCoroutine(alertAdapter));
            }
        }

        /// <summary>
        /// Coroutine for processing pool metrics alerts.
        /// </summary>
        /// <param name="alertAdapter">The alert adapter to process.</param>
        /// <returns>Coroutine enumerator.</returns>
        private System.Collections.IEnumerator AlertProcessingCoroutine(NativePoolMetricsAlertAdapter alertAdapter)
        {
            while (UnityEngine.Application.isPlaying)
            {
                alertAdapter.ProcessAlerts();
                yield return new UnityEngine.WaitForSeconds(0.1f); // Process every 100ms
            }
        }
    }

    /// <summary>
    /// Bridge implementation that adapts VContainer's IObjectResolver to IDependencyProvider.
    /// </summary>
    internal sealed class VContainerDependencyProvider : IDependencyProvider
    {
        private readonly IObjectResolver _resolver;

        /// <summary>
        /// Initializes a new instance of the VContainerDependencyProvider class.
        /// </summary>
        /// <param name="resolver">The VContainer object resolver.</param>
        public VContainerDependencyProvider(IObjectResolver resolver)
        {
            _resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
        }

        /// <summary>
        /// Resolves a dependency of the specified type.
        /// </summary>
        /// <typeparam name="T">Type to resolve.</typeparam>
        /// <returns>Instance of the requested type.</returns>
        public T Resolve<T>()
        {
            try
            {
                return _resolver.Resolve<T>();
            }
            catch (VContainerException ex)
            {
                UnityEngine.Debug.LogError($"[VContainerDependencyProvider] Failed to resolve {typeof(T).Name}: {ex.Message}");
                return default;
            }
        }
    }
}