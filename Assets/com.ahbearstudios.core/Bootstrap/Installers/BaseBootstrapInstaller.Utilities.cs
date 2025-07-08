using System;
using System.Diagnostics;
using System.Reflection;
using System.Threading.Tasks;
using AhBearStudios.Core.Bootstrap.Interfaces;

namespace AhBearStudios.Core.Bootstrap.Installers
{
    public abstract partial class BaseBootstrapInstaller
    {
        #region Utility Methods

        /// <summary>
        /// Measures the execution time of an action and logs the result.
        /// </summary>
        protected TimeSpan MeasureExecutionTime(Action action, string operationName = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                action();
            }
            finally
            {
                stopwatch.Stop();
                if (!string.IsNullOrEmpty(operationName))
                {
                    _logger?.LogInfo($"{operationName} completed in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                }
            }
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Measures the execution time of an async action and logs the result.
        /// </summary>
        protected async Task<TimeSpan> MeasureExecutionTimeAsync(Func<Task> action, string operationName = null)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            var stopwatch = Stopwatch.StartNew();
            try
            {
                await action();
            }
            finally
            {
                stopwatch.Stop();
                if (!string.IsNullOrEmpty(operationName))
                {
                    _logger?.LogInfo($"{operationName} completed in {stopwatch.Elapsed.TotalMilliseconds:F2}ms");
                }
            }
            return stopwatch.Elapsed;
        }

        /// <summary>
        /// Safely executes an action with exception handling and logging.
        /// </summary>
        protected bool TryExecute(Action action, string operationName = null, bool logErrors = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                action();
                return true;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    var operation = operationName ?? "Operation";
                    _logger?.LogException(ex, $"{operation} failed in {InstallerName}");
                }
                return false;
            }
        }

        /// <summary>
        /// Safely executes an async action with exception handling and logging.
        /// </summary>
        protected async Task<bool> TryExecuteAsync(Func<Task> action, string operationName = null, bool logErrors = true)
        {
            if (action == null)
                throw new ArgumentNullException(nameof(action));

            try
            {
                await action();
                return true;
            }
            catch (Exception ex)
            {
                if (logErrors)
                {
                    var operation = operationName ?? "Operation";
                    _logger?.LogException(ex, $"{operation} failed in {InstallerName}");
                }
                return false;
            }
        }

        /// <summary>
        /// Gets the current memory usage in bytes.
        /// </summary>
        protected long GetCurrentMemoryUsage()
        {
            GC.Collect();
            GC.WaitForPendingFinalizers();
            GC.Collect();
            return GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Validates that a type implements the required interface.
        /// </summary>
        protected bool ValidateTypeImplementsInterface<TInterface>(Type type)
        {
            if (type == null)
                return false;

            return typeof(TInterface).IsAssignableFrom(type);
        }

        /// <summary>
        /// Validates that a type has a parameterless constructor.
        /// </summary>
        protected bool ValidateTypeHasParameterlessConstructor(Type type)
        {
            if (type == null)
                return false;

            return type.GetConstructor(BindingFlags.Public | BindingFlags.Instance, null, Type.EmptyTypes, null) != null;
        }

        /// <summary>
        /// Gets a user-friendly description of the installer for logging and diagnostics.
        /// </summary>
        protected string GetInstallerDescription()
        {
            return $"{InstallerName} (Priority: {Priority}, Category: {Category}, Enabled: {IsEnabled})";
        }

        /// <summary>
        /// Logs the start of an installation phase.
        /// </summary>
        protected void LogPhaseStart(string phaseName)
        {
            _logger?.LogInfo($"[{InstallerName}] Starting {phaseName} phase (CorrelationId: {_correlationId})");
        }

        /// <summary>
        /// Logs the completion of an installation phase.
        /// </summary>
        protected void LogPhaseEnd(string phaseName, TimeSpan duration)
        {
            _logger?.LogInfo($"[{InstallerName}] Completed {phaseName} phase in {duration.TotalMilliseconds:F2}ms");
        }

        /// <summary>
        /// Logs installer configuration information.
        /// </summary>
        protected void LogInstallerInfo()
        {
            _logger?.LogInfo($"Installer Configuration: {GetInstallerDescription()}");
            
            if (Dependencies.Length > 0)
            {
                var deps = string.Join(", ", Array.ConvertAll(Dependencies, d => d.Name));
                _logger?.LogInfo($"Dependencies: {deps}");
            }
            
            _logger?.LogInfo($"Estimated Memory Overhead: {EstimatedMemoryOverheadBytes:N0} bytes");
        }

        /// <summary>
        /// Creates a scope name for profiling operations.
        /// </summary>
        protected string CreateProfilingScopeName(string operation)
        {
            return $"{InstallerName}.{operation}";
        }

        #endregion

        #region Constants

        /// <summary>
        /// Default timeout for installation operations in milliseconds.
        /// </summary>
        protected const int DefaultTimeoutMs = 30000; // 30 seconds

        /// <summary>
        /// Maximum number of retry attempts for failed operations.
        /// </summary>
        protected const int MaxRetryAttempts = 3;

        /// <summary>
        /// Default memory overhead estimation in bytes.
        /// </summary>
        protected const long DefaultMemoryOverheadBytes = 1024; // 1KB

        #endregion
    }
}