using System;
using System.Collections.Generic;
using AhBearStudios.Core.Logging.Models;
using AhBearStudios.Core.Logging.Services;
using AhBearStudios.Core.Pooling.Pools;

namespace AhBearStudios.Core.Logging.Tests
{
    /// <summary>
    /// Test class to verify LogScope works correctly with LogContextService.
    /// </summary>
    public static class LogScopeContextTest
    {
        /// <summary>
        /// Tests that LogScope can be constructed with LogContextService.
        /// </summary>
        public static void TestLogScopeWithContextService()
        {
            Console.WriteLine("Testing LogScope with LogContextService...");
            
            var contextService = new LogContextService();
            var pool = new ManagedLogDataPool(null, null);
            
            try
            {
                var properties = new Dictionary<string, object>
                {
                    ["TestProperty"] = "TestValue"
                };
                
                // Test that LogScope can be created with LogContextService
                using (var scope = contextService.PushContext("TestScope", properties))
                {
                    Assert(scope != null, "Scope should not be null");
                    Assert(scope is ILogScope, "Scope should implement ILogScope");
                    
                    // Test that context is properly set
                    var currentContext = contextService.CurrentContext;
                    Assert(!currentContext.Equals(LogContext.Empty), "Context should not be empty");
                    Assert(currentContext.Operation == "TestScope", "Context operation should match scope name");
                    
                    // Test log entry enrichment with scope
                    var logEntry = LogEntry.Create(
                        LogLevel.Info,
                        "TestChannel",
                        "Test message",
                        managedDataPool: pool);
                    
                    var enrichedEntry = contextService.EnrichLogEntry(logEntry, pool);
                    Assert(enrichedEntry.Properties.ContainsKey("TestProperty"), "Enriched entry should contain scope properties");
                    Assert(enrichedEntry.Scope != null, "Enriched entry should have scope");
                }
                
                // Test that context is properly popped after scope disposal
                var contextAfterDispose = contextService.CurrentContext;
                Assert(contextAfterDispose.Equals(LogContext.Empty), "Context should be empty after scope disposal");
                
                Console.WriteLine("✓ LogScope with LogContextService test passed");
            }
            finally
            {
                contextService.Dispose();
                pool.Dispose();
            }
        }
        
        /// <summary>
        /// Tests nested scopes with LogContextService.
        /// </summary>
        public static void TestNestedScopes()
        {
            Console.WriteLine("Testing nested scopes with LogContextService...");
            
            var contextService = new LogContextService();
            
            try
            {
                using (var outerScope = contextService.PushContext("OuterScope"))
                {
                    var outerContext = contextService.CurrentContext;
                    Assert(outerContext.Operation == "OuterScope", "Outer context should be set");
                    
                    using (var innerScope = contextService.PushContext("InnerScope"))
                    {
                        var innerContext = contextService.CurrentContext;
                        Assert(innerContext.Operation == "InnerScope", "Inner context should be set");
                        Assert(contextService.ContextDepth == 2, "Should have 2 contexts in stack");
                    }
                    
                    // After inner scope disposal, should be back to outer scope
                    var contextAfterInner = contextService.CurrentContext;
                    Assert(contextAfterInner.Operation == "OuterScope", "Should be back to outer context");
                    Assert(contextService.ContextDepth == 1, "Should have 1 context in stack");
                }
                
                // After outer scope disposal, should be empty
                var contextAfterOuter = contextService.CurrentContext;
                Assert(contextAfterOuter.Equals(LogContext.Empty), "Should be empty after all scopes disposed");
                Assert(contextService.ContextDepth == 0, "Should have 0 contexts in stack");
                
                Console.WriteLine("✓ Nested scopes test passed");
            }
            finally
            {
                contextService.Dispose();
            }
        }
        
        /// <summary>
        /// Runs all LogScope context tests.
        /// </summary>
        public static void RunAllTests()
        {
            Console.WriteLine("=== Starting LogScope Context Tests ===");
            
            try
            {
                TestLogScopeWithContextService();
                TestNestedScopes();
                
                Console.WriteLine("=== All LogScope Context Tests Completed Successfully! ===");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Test Failed: {ex.Message} ===");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
            }
        }
        
        /// <summary>
        /// Simple assertion helper.
        /// </summary>
        private static void Assert(bool condition, string message)
        {
            if (!condition)
            {
                throw new Exception($"Assertion failed: {message}");
            }
        }
    }
}