using System;
using System.Collections.Generic;
using ZLinq;
using AhBearStudios.Core.Common.Utilities;

namespace AhBearStudios.Core.Tests.Shared.Utilities
{
    /// <summary>
    /// Helper class for managing correlation IDs in tests, ensuring proper tracking and verification.
    /// Uses DeterministicIdGenerator for consistent, reproducible test IDs.
    /// </summary>
    public sealed class TestCorrelationHelper : IDisposable
    {
        private readonly Dictionary<string, Guid> _correlationIds = new Dictionary<string, Guid>();
        private readonly Dictionary<Guid, string> _correlationContexts = new Dictionary<Guid, string>();
        private readonly List<Guid> _createdIds = new List<Guid>();

        /// <summary>
        /// Gets all correlation IDs created during the test session.
        /// </summary>
        public IReadOnlyList<Guid> CreatedIds => _createdIds.AsValueEnumerable().ToList();

        /// <summary>
        /// Gets the number of correlation IDs created.
        /// </summary>
        public int Count => _createdIds.Count;

        /// <summary>
        /// Creates a deterministic correlation ID for the specified test context.
        /// Multiple calls with the same context will return the same ID.
        /// </summary>
        /// <param name="testContext">The test context or operation name</param>
        /// <returns>A deterministic correlation ID</returns>
        public Guid CreateCorrelationId(string testContext)
        {
            if (string.IsNullOrEmpty(testContext))
                throw new ArgumentException("Test context cannot be null or empty", nameof(testContext));

            if (_correlationIds.TryGetValue(testContext, out var existingId))
                return existingId;

            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("Test", testContext);

            _correlationIds[testContext] = correlationId;
            _correlationContexts[correlationId] = testContext;
            _createdIds.Add(correlationId);

            return correlationId;
        }

        /// <summary>
        /// Creates a unique correlation ID for each call, even with the same context.
        /// </summary>
        /// <param name="testContext">The test context or operation name</param>
        /// <returns>A unique correlation ID</returns>
        public Guid CreateUniqueCorrelationId(string testContext)
        {
            if (string.IsNullOrEmpty(testContext))
                throw new ArgumentException("Test context cannot be null or empty", nameof(testContext));

            var uniqueContext = $"{testContext}_{DateTime.UtcNow.Ticks}_{_createdIds.Count}";
            var correlationId = DeterministicIdGenerator.GenerateCorrelationId("Test", uniqueContext);

            _correlationContexts[correlationId] = testContext;
            _createdIds.Add(correlationId);

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
                return Enumerable.Empty<Guid>();

            // Simple wildcard matching (supports * at end)
            var isWildcard = contextPattern.EndsWith("*");
            var pattern = isWildcard ? contextPattern.Substring(0, contextPattern.Length - 1) : contextPattern;

            return _correlationContexts
                .AsValueEnumerable()
                .Where(kvp => isWildcard ? kvp.Value.StartsWith(pattern) : kvp.Value == pattern)
                .Select(kvp => kvp.Key)
                .ToList();
        }

        /// <summary>
        /// Creates a set of related correlation IDs for complex test scenarios.
        /// </summary>
        /// <param name="baseContext">The base context name</param>
        /// <param name="count">The number of related IDs to create</param>
        /// <returns>A list of related correlation IDs</returns>
        public IList<Guid> CreateRelatedCorrelationIds(string baseContext, int count)
        {
            if (count <= 0)
                throw new ArgumentException("Count must be positive", nameof(count));

            var ids = new List<Guid>();
            for (int i = 0; i < count; i++)
            {
                var context = $"{baseContext}_Related_{i}";
                ids.Add(CreateCorrelationId(context));
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
            return expectedContexts.AsValueEnumerable().All(context => _correlationIds.ContainsKey(context));
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
        /// Clears all stored correlation IDs and contexts.
        /// </summary>
        public void Clear()
        {
            _correlationIds.Clear();
            _correlationContexts.Clear();
            _createdIds.Clear();
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