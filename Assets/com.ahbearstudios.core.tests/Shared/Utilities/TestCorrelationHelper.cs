using System;
using System.Collections.Generic;
using System.Linq;
using AhBearStudios.Core.Common.Utilities;
using AhBearStudios.Core.Tests.Shared.TestDoubles.Stubs;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Helper class for managing correlation IDs in tests, ensuring proper tracking and verification.
    /// Uses DeterministicIdGenerator for consistent, reproducible test IDs.
    /// Strictly follows CLAUDETESTS.md guidelines with TDD test double integration.
    /// Unity Test Runner compatible for both Edit Mode and Play Mode tests.
    /// </summary>
    public sealed class TestCorrelationHelper : IDisposable
    {
        private readonly Dictionary<string, Guid> _correlationIds = new Dictionary<string, Guid>();
        private readonly Dictionary<Guid, string> _correlationContexts = new Dictionary<Guid, string>();
        private readonly List<Guid> _createdIds = new List<Guid>();
        private readonly Dictionary<Guid, DateTime> _correlationTimestamps = new Dictionary<Guid, DateTime>();
        private readonly Dictionary<Guid, List<string>> _correlationChains = new Dictionary<Guid, List<string>>();
        private StubLoggingService _loggingService;

        /// <summary>
        /// Initializes a new instance of TestCorrelationHelper with optional logging integration.
        /// </summary>
        /// <param name="loggingService">Optional logging service for test double integration</param>
        public TestCorrelationHelper(StubLoggingService loggingService = null)
        {
            _loggingService = loggingService;
        }

        /// <summary>
        /// Gets all correlation IDs created during the test session.
        /// </summary>
        public IReadOnlyList<Guid> CreatedIds => _createdIds;

        /// <summary>
        /// Gets the number of correlation IDs created.
        /// </summary>
        public int Count => _createdIds.Count;

        /// <summary>
        /// Creates a deterministic correlation ID for the specified test context.
        /// Multiple calls with the same context will return the same ID.
        /// Enhanced with TDD test double integration and correlation tracking.
        /// </summary>
        /// <param name="testContext">The test context or operation name</param>
        /// <returns>A deterministic correlation ID</returns>
        public Guid CreateCorrelationId(string testContext)
        {
            if (string.IsNullOrEmpty(testContext))
                throw new ArgumentException("Test context cannot be null or empty", nameof(testContext));

            if (_correlationIds.TryGetValue(testContext, out var existingId))
            {
                _loggingService?.LogInfo($"Reusing existing correlation ID for context '{testContext}': {existingId}");
                return existingId;
            }

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("Test", testContext);
            var timestamp = DateTime.UtcNow;

            _correlationIds[testContext] = correlationId;
            _correlationContexts[correlationId] = testContext;
            _correlationTimestamps[correlationId] = timestamp;
            _createdIds.Add(correlationId);

            // Initialize correlation chain
            _correlationChains[correlationId] = new List<string> { testContext };

            _loggingService?.LogInfo($"Created new correlation ID for context '{testContext}': {correlationId}");

            return correlationId;
        }

        /// <summary>
        /// Creates a unique correlation ID for each call, even with the same context.
        /// Enhanced with timestamp tracking and TDD test double integration.
        /// </summary>
        /// <param name="testContext">The test context or operation name</param>
        /// <returns>A unique correlation ID</returns>
        public Guid CreateUniqueCorrelationId(string testContext)
        {
            if (string.IsNullOrEmpty(testContext))
                throw new ArgumentException("Test context cannot be null or empty", nameof(testContext));

            var uniqueContext = $"{testContext}_{DateTime.UtcNow.Ticks}_{_createdIds.Count}";
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("Test", uniqueContext);
            var timestamp = DateTime.UtcNow;

            _correlationContexts[correlationId] = testContext;
            _correlationTimestamps[correlationId] = timestamp;
            _createdIds.Add(correlationId);

            // Initialize correlation chain
            _correlationChains[correlationId] = new List<string> { testContext };

            _loggingService?.LogInfo($"Created unique correlation ID for context '{testContext}': {correlationId}");

            return correlationId;
        }

        /// <summary>
        /// Gets the test context associated with a correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to look up</param>
        /// <returns>The test context, or null if not found</returns>
        public string GetContext(Guid correlationId)
        {
            return _correlationContexts.TryGetValue(correlationId, out var context) ? context : null;
        }

        /// <summary>
        /// Gets the correlation ID for a specific test context.
        /// </summary>
        /// <param name="testContext">The test context to look up</param>
        /// <returns>The correlation ID, or Guid.Empty if not found</returns>
        public Guid GetCorrelationId(string testContext)
        {
            return _correlationIds.TryGetValue(testContext, out var id) ? id : Guid.Empty;
        }

        /// <summary>
        /// Verifies that a correlation ID was created during the test session.
        /// </summary>
        /// <param name="correlationId">The correlation ID to verify</param>
        /// <returns>True if the ID was created by this helper</returns>
        public bool WasCreatedByHelper(Guid correlationId)
        {
            return _correlationContexts.ContainsKey(correlationId);
        }

        /// <summary>
        /// Gets all correlation IDs created for a specific test context pattern.
        /// </summary>
        /// <param name="contextPattern">The context pattern to match (supports wildcards)</param>
        /// <returns>All matching correlation IDs</returns>
        public IEnumerable<Guid> GetCorrelationIdsForContext(string contextPattern)
        {
            if (string.IsNullOrEmpty(contextPattern))
                return new List<Guid>();

            // Simple wildcard matching (supports * at end)
            var isWildcard = contextPattern.EndsWith("*");
            var pattern = isWildcard ? contextPattern.Substring(0, contextPattern.Length - 1) : contextPattern;

            return _correlationContexts
                .Where(kvp => isWildcard ? kvp.Value.StartsWith(pattern) : kvp.Value == pattern)
                .Select(kvp => kvp.Key)
                .ToList();
        }
        
        /// <summary>
        /// Static method to create related correlation IDs for use in tests.
        /// </summary>
        /// <param name="baseContext">The base context name</param>
        /// <param name="count">The number of related IDs to create</param>
        /// <returns>A list of related correlation IDs</returns>
        public static IList<Guid> CreateRelatedCorrelationIds(string baseContext, int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be positive", nameof(count));

            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                var context = $"{baseContext}_Related_{i}";
                var correlationId = DeterministicIdGenerator.GenerateCorrelationId("Test", context);
                ids.Add(correlationId);
            }

            return ids;
        }

        /// <summary>
        /// Verifies that all expected correlation IDs were created.
        /// </summary>
        /// <param name="expectedContexts">The expected test contexts</param>
        /// <returns>True if all expected IDs were created</returns>
        public bool VerifyAllCreated(params string[] expectedContexts)
        {
            return expectedContexts.All(context => _correlationIds.ContainsKey(context));
        }

        /// <summary>
        /// Gets a summary of all correlation IDs created during the test session.
        /// </summary>
        /// <returns>A formatted summary string</returns>
        public string GetSummary()
        {
            var summary = $"Test Correlation Summary: {_createdIds.Count} IDs created\n";

            foreach (var kvp in _correlationIds)
            {
                summary += $"  {kvp.Key}: {kvp.Value}\n";
            }

            return summary;
        }

        /// <summary>
        /// Gets the timestamp when a correlation ID was created.
        /// </summary>
        /// <param name="correlationId">The correlation ID to look up</param>
        /// <returns>The creation timestamp, or null if not found</returns>
        public DateTime? GetTimestamp(Guid correlationId)
        {
            return _correlationTimestamps.TryGetValue(correlationId, out var timestamp) ? timestamp : null;
        }

        /// <summary>
        /// Gets the correlation chain for a specific correlation ID.
        /// </summary>
        /// <param name="correlationId">The correlation ID to look up</param>
        /// <returns>The correlation chain, or empty list if not found</returns>
        public IReadOnlyList<string> GetCorrelationChain(Guid correlationId)
        {
            return _correlationChains.TryGetValue(correlationId, out var chain) ? chain : new List<string>();
        }

        /// <summary>
        /// Adds a context step to an existing correlation chain.
        /// Enables tracking of multi-step operations in complex test scenarios.
        /// </summary>
        /// <param name="correlationId">The correlation ID to extend</param>
        /// <param name="context">The new context step to add</param>
        /// <returns>True if the chain was updated, false if correlation ID not found</returns>
        public bool ExtendCorrelationChain(Guid correlationId, string context)
        {
            if (string.IsNullOrEmpty(context))
                throw new ArgumentException("Context cannot be null or empty", nameof(context));

            if (!_correlationChains.TryGetValue(correlationId, out var chain))
                return false;

            chain.Add(context);
            _loggingService?.LogInfo($"Extended correlation chain for {correlationId}: {context}");
            return true;
        }

        /// <summary>
        /// Validates frame budget compliance for correlation operations.
        /// Ensures correlation tracking meets Unity's 60 FPS requirements (16.67ms budget).
        /// </summary>
        /// <param name="operation">The correlation operation to validate</param>
        /// <param name="operationName">Name for reporting</param>
        /// <param name="customBudget">Custom budget (optional, defaults to TestConstants.FrameBudget)</param>
        /// <returns>True if operation completes within frame budget</returns>
        public bool ValidateFrameBudgetCompliance(System.Action operation, string operationName, TimeSpan? customBudget = null)
        {
            var budget = customBudget ?? TestConstants.FrameBudget;
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                operation();
            }
            finally
            {
                stopwatch.Stop();
            }

            var withinBudget = stopwatch.Elapsed <= budget;
            _loggingService?.LogInfo($"Frame budget validation for '{operationName}': {stopwatch.Elapsed.TotalMilliseconds:F2}ms (Budget: {budget.TotalMilliseconds:F2}ms) - {(withinBudget ? "PASS" : "FAIL")}");

            return withinBudget;
        }

        /// <summary>
        /// Gets correlation statistics for performance analysis.
        /// Essential for CLAUDETESTS.md performance requirements and monitoring.
        /// </summary>
        /// <returns>Dictionary of correlation statistics</returns>
        public Dictionary<string, object> GetCorrelationStatistics()
        {
            var stats = new Dictionary<string, object>
            {
                ["TotalCorrelationIds"] = _createdIds.Count,
                ["UniqueContexts"] = _correlationIds.Count,
                ["ActiveChains"] = _correlationChains.Count,
                ["AverageChainLength"] = _correlationChains.Count > 0
                    ? _correlationChains.Values.Select(chain => chain.Count).Average()
                    : 0.0,
                ["OldestCorrelation"] = _correlationTimestamps.Count > 0
                    ? _correlationTimestamps.Values.Min()
                    : (DateTime?)null,
                ["NewestCorrelation"] = _correlationTimestamps.Count > 0
                    ? _correlationTimestamps.Values.Max()
                    : (DateTime?)null
            };

            return stats;
        }

        /// <summary>
        /// Validates correlation data integrity for robust testing.
        /// Ensures all internal dictionaries are consistent and correlation chains are valid.
        /// </summary>
        /// <returns>True if all correlation data is consistent</returns>
        public bool ValidateDataIntegrity()
        {
            try
            {
                // Check that all created IDs have contexts
                foreach (var id in _createdIds)
                {
                    if (!_correlationContexts.ContainsKey(id))
                    {
                        _loggingService?.LogError($"Integrity violation: Created ID {id} missing context");
                        return false;
                    }
                }

                // Check that all correlation IDs have timestamps
                foreach (var kvp in _correlationContexts)
                {
                    if (!_correlationTimestamps.ContainsKey(kvp.Key))
                    {
                        _loggingService?.LogError($"Integrity violation: Correlation ID {kvp.Key} missing timestamp");
                        return false;
                    }
                }

                // Check that all correlation chains have valid entries
                foreach (var kvp in _correlationChains)
                {
                    if (kvp.Value.Count == 0)
                    {
                        _loggingService?.LogError($"Integrity violation: Correlation ID {kvp.Key} has empty chain");
                        return false;
                    }

                    if (kvp.Value.Any(string.IsNullOrEmpty))
                    {
                        _loggingService?.LogError($"Integrity violation: Correlation ID {kvp.Key} has null/empty chain entries");
                        return false;
                    }
                }

                _loggingService?.LogInfo("Correlation data integrity validation passed");
                return true;
            }
            catch (System.Exception ex)
            {
                _loggingService?.LogError($"Integrity validation failed with exception: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// Creates a batch of correlated IDs for complex distributed testing scenarios.
        /// Follows CLAUDETESTS.md guidelines for correlation tracking in distributed systems.
        /// </summary>
        /// <param name="baseContext">Base context for the correlation batch</param>
        /// <param name="operations">List of operation names to correlate</param>
        /// <param name="parentCorrelationId">Optional parent correlation ID for hierarchical tracking</param>
        /// <returns>Dictionary mapping operation names to correlation IDs</returns>
        public Dictionary<string, Guid> CreateCorrelatedBatch(string baseContext, IEnumerable<string> operations, Guid parentCorrelationId = default)
        {
            if (string.IsNullOrEmpty(baseContext))
                throw new ArgumentException("Base context cannot be null or empty", nameof(baseContext));

            if (operations == null)
                throw new ArgumentNullException(nameof(operations));

            var batch = new Dictionary<string, Guid>();
            var operationList = operations.ToList();

            if (operationList.Count == 0)
                return batch;

            _loggingService?.LogInfo($"Creating correlated batch for base context '{baseContext}' with {operationList.Count} operations");

            foreach (var operation in operationList)
            {
                if (string.IsNullOrEmpty(operation))
                    continue;

                var correlatedContext = $"{baseContext}_{operation}_{DateTime.UtcNow.Ticks}";
                var correlationId = CreateCorrelationId(correlatedContext);

                // Link to parent correlation if provided
                if (parentCorrelationId != default && _correlationChains.TryGetValue(correlationId, out var chain))
                {
                    chain.Insert(0, $"Parent:{parentCorrelationId}");
                }

                batch[operation] = correlationId;
            }

            return batch;
        }

        /// <summary>
        /// Clears all stored correlation IDs and contexts.
        /// </summary>
        public void Clear()
        {
            _correlationIds.Clear();
            _correlationContexts.Clear();
            _createdIds.Clear();
            _correlationTimestamps.Clear();
            _correlationChains.Clear();

            _loggingService?.LogInfo("Cleared all correlation data");
        }

        /// <summary>
        /// Disposes of the helper and clears all data.
        /// </summary>
        public void Dispose()
        {
            Clear();
        }
    }
}