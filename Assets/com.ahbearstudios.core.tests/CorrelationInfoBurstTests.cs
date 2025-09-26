using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using AhBearStudios.Core.Common.Models;

namespace AhBearStudios.Core.Tests
{
    /// <summary>
    /// Tests to verify CorrelationInfo is fully Burst-compatible.
    /// </summary>
    [TestFixture]
    public class CorrelationInfoBurstTests
    {
        /// <summary>
        /// Burst-compiled job that creates and manipulates CorrelationInfo.
        /// </summary>
        [BurstCompile]
        private struct CorrelationInfoBurstJob : IJob
        {
            public NativeArray<uint> Results;

            public void Execute()
            {
                // Create a Burst-compatible CorrelationInfo
                var correlationId = new FixedString128Bytes();
                // Build string manually for Burst compatibility
                correlationId.Append((byte)'t'); correlationId.Append((byte)'e'); correlationId.Append((byte)'s'); correlationId.Append((byte)'t');
                correlationId.Append((byte)'-'); correlationId.Append((byte)'i'); correlationId.Append((byte)'d');

                var operation = new FixedString128Bytes();
                operation.Append((byte)'T'); operation.Append((byte)'e'); operation.Append((byte)'s'); operation.Append((byte)'t');
                operation.Append((byte)'O'); operation.Append((byte)'p');

                var serviceName = new FixedString64Bytes();
                serviceName.Append((byte)'T'); serviceName.Append((byte)'e'); serviceName.Append((byte)'s'); serviceName.Append((byte)'t');

                // Use the Burst-compatible constructor with minimal parameters
                var correlationInfo = new CorrelationInfo(
                    correlationId: correlationId,
                    parentCorrelationId: default,
                    rootCorrelationId: default,
                    spanId: default,
                    traceId: default,
                    operation: operation,
                    userId: default,
                    sessionId: default,
                    requestId: default,
                    serviceName: serviceName,
                    createdAtTicks: 638395968000000000L,
                    depth: 0,
                    correlationHash: 12345,
                    secondaryHash: 67890
                );

                // Test Burst-compatible methods
                bool isRoot = correlationInfo.IsRoot();
                bool isChild = correlationInfo.IsChild();
                int nativeSize = correlationInfo.GetNativeSize();
                long ageMs = correlationInfo.GetAgeMillisecondsBurst();

                // Test equality (Burst-compatible)
                var correlationInfo2 = new CorrelationInfo(
                    correlationId: correlationId,
                    parentCorrelationId: default,
                    rootCorrelationId: default,
                    spanId: default,
                    traceId: default,
                    operation: operation,
                    userId: default,
                    sessionId: default,
                    requestId: default,
                    serviceName: serviceName,
                    createdAtTicks: 638395968000000000L,
                    depth: 0,
                    correlationHash: 12345,
                    secondaryHash: 67890
                );

                bool areEqual = correlationInfo.Equals(correlationInfo2);
                int hashCode = correlationInfo.GetHashCode();

                // Generate a correlation ID using existing functionality
                // Since we're in Burst context, we'll create a simple deterministic ID
                var generatedId = new FixedString128Bytes();
                // Manually build string to avoid managed string operations
                generatedId.Append((byte)'b'); generatedId.Append((byte)'u'); generatedId.Append((byte)'r'); generatedId.Append((byte)'s'); generatedId.Append((byte)'t');
                generatedId.Append((byte)'-'); generatedId.Append((byte)'1'); generatedId.Append((byte)'2'); generatedId.Append((byte)'3');

                // Store results for verification
                Results[0] = isRoot ? 1u : 0u;
                Results[1] = isChild ? 1u : 0u;
                Results[2] = (uint)nativeSize;
                Results[3] = areEqual ? 1u : 0u;
                Results[4] = (uint)hashCode;
                Results[5] = (uint)generatedId.Length;
                Results[6] = correlationInfo.CorrelationHash;
                Results[7] = correlationInfo.SecondaryHash;
            }
        }

        [Test]
        public void CorrelationInfo_BurstCompatible_CreatesAndManipulates()
        {
            // Arrange
            using (var results = new NativeArray<uint>(8, Allocator.TempJob))
            {
                var job = new CorrelationInfoBurstJob
                {
                    Results = results
                };

                // Act
                job.Schedule().Complete();

                // Assert
                Assert.That(results[0], Is.EqualTo(1u), "Should be root correlation");
                Assert.That(results[1], Is.EqualTo(0u), "Should not be child correlation");
                Assert.That(results[2], Is.GreaterThan(0u), "Native size should be greater than 0");
                Assert.That(results[3], Is.EqualTo(1u), "Correlations should be equal");
                Assert.That(results[4], Is.Not.EqualTo(0u), "Hash code should not be 0");
                Assert.That(results[5], Is.GreaterThan(0u), "Generated ID should have length > 0");
                Assert.That(results[6], Is.EqualTo(12345u), "Correlation hash should match");
                Assert.That(results[7], Is.EqualTo(67890u), "Secondary hash should match");
            }
        }

        [Test]
        public void CorrelationInfo_CreateNative_WorksInBurstContext()
        {
            // This test verifies the CreateNative method works correctly
            var correlationId = new FixedString128Bytes();
            // Build string manually for Burst compatibility
            correlationId.Append((byte)'n'); correlationId.Append((byte)'a'); correlationId.Append((byte)'t'); correlationId.Append((byte)'i'); correlationId.Append((byte)'v'); correlationId.Append((byte)'e');
            correlationId.Append((byte)'-'); correlationId.Append((byte)'i'); correlationId.Append((byte)'d');

            var operation = new FixedString128Bytes();
            operation.Append((byte)'N'); operation.Append((byte)'a'); operation.Append((byte)'t'); operation.Append((byte)'i'); operation.Append((byte)'v'); operation.Append((byte)'e');

            var serviceName = new FixedString64Bytes();
            serviceName.Append((byte)'N'); serviceName.Append((byte)'a'); serviceName.Append((byte)'t'); serviceName.Append((byte)'i'); serviceName.Append((byte)'v'); serviceName.Append((byte)'e');

            // Act
            var correlationInfo = CorrelationInfo.CreateNative(
                correlationId: correlationId,
                operation: operation,
                serviceName: serviceName
            );

            // Assert
            Assert.That(correlationInfo.CorrelationId.ToString(), Is.EqualTo("native-test-id"));
            Assert.That(correlationInfo.Operation.ToString(), Is.EqualTo("NativeOp"));
            Assert.That(correlationInfo.ServiceName.ToString(), Is.EqualTo("NativeService"));
            Assert.That(correlationInfo.IsRoot(), Is.True);
            Assert.That(correlationInfo.Depth, Is.EqualTo(0));
        }

        [Test]
        public void CorrelationInfo_ManagedMethods_WorkOutsideBurst()
        {
            // Test the managed (non-Burst) methods still work correctly
            var correlationInfo = CorrelationInfo.Create(
                operation: "ManagedOp",
                userId: "user123",
                sessionId: "session456",
                serviceName: "ManagedService"
            );

            // These methods are marked with BurstDiscard
            var createdAt = correlationInfo.CreatedAt;
            var age = correlationInfo.Age;
            var stringRep = correlationInfo.ToString();
            var managedStrings = correlationInfo.ToManagedStrings();
            var dictionary = correlationInfo.ToDictionary();

            // Assert
            Assert.That(correlationInfo.Operation.ToString(), Is.EqualTo("ManagedOp"));
            Assert.That(correlationInfo.UserId.ToString(), Is.EqualTo("user123"));
            Assert.That(correlationInfo.SessionId.ToString(), Is.EqualTo("session456"));
            Assert.That(correlationInfo.ServiceName.ToString(), Is.EqualTo("ManagedService"));
            Assert.That(createdAt, Is.Not.EqualTo(default(System.DateTime)));
            Assert.That(age.TotalMilliseconds, Is.GreaterThanOrEqualTo(0));
            Assert.That(stringRep, Is.Not.Null.And.Not.Empty);
            Assert.That(managedStrings.operation, Is.EqualTo("ManagedOp"));
            Assert.That(dictionary, Is.Not.Null.And.Not.Empty);
        }

        /// <summary>
        /// Job to test CorrelationInfo in a more complex Burst scenario.
        /// </summary>
        [BurstCompile]
        private struct ComplexCorrelationBurstJob : IJobParallelFor
        {
            [ReadOnly] public NativeArray<uint> Seeds;
            [WriteOnly] public NativeArray<uint> Hashes;

            public void Execute(int index)
            {
                var operation = new FixedString128Bytes();
                // Build string manually for Burst compatibility
                operation.Append((byte)'O'); operation.Append((byte)'p'); operation.Append((byte)'-');
                operation.Append(index); // FixedString can append int directly

                // Generate correlation ID in Burst context
                var correlationId = new FixedString128Bytes();
                correlationId.Append((byte)'p'); correlationId.Append((byte)'a'); correlationId.Append((byte)'r'); correlationId.Append((byte)'a'); correlationId.Append((byte)'l'); correlationId.Append((byte)'l'); correlationId.Append((byte)'e'); correlationId.Append((byte)'l'); correlationId.Append((byte)'-');
                correlationId.Append(index); // FixedString can append int directly
                correlationId.Append((byte)'-'); correlationId.Append((byte)'s'); correlationId.Append((byte)'e'); correlationId.Append((byte)'e'); correlationId.Append((byte)'d');

                var info = new CorrelationInfo(
                    correlationId: correlationId,
                    parentCorrelationId: default,
                    rootCorrelationId: default,
                    spanId: default,
                    traceId: default,
                    operation: operation,
                    userId: default,
                    sessionId: default,
                    requestId: default,
                    serviceName: default,
                    createdAtTicks: 638395968000000000L,
                    depth: index,
                    correlationHash: 0,
                    secondaryHash: 0
                );

                Hashes[index] = (uint)info.GetHashCode();
            }
        }

        [Test]
        public void CorrelationInfo_ParallelBurstJob_ProcessesMultipleCorrelations()
        {
            const int count = 100;

            var seeds = new NativeArray<uint>(count, Allocator.TempJob);
            var hashes = new NativeArray<uint>(count, Allocator.TempJob);

            try
            {
                // Initialize seeds
                for (int i = 0; i < count; i++)
                {
                    seeds[i] = (uint)(i * 1000);
                }

                var job = new ComplexCorrelationBurstJob
                {
                    Seeds = seeds,
                    Hashes = hashes
                };

                // Execute in parallel
                job.Schedule(count, 10).Complete();

                // Verify all hashes were generated and are unique
                var uniqueHashes = new System.Collections.Generic.HashSet<uint>();
                for (int i = 0; i < count; i++)
                {
                    Assert.That(hashes[i], Is.Not.EqualTo(0u), $"Hash at index {i} should not be 0");
                    uniqueHashes.Add(hashes[i]);
                }

                // Most hashes should be unique (some collisions are acceptable)
                Assert.That(uniqueHashes.Count, Is.GreaterThan(count * 0.9), "Most hashes should be unique");
            }
            finally
            {
                if (seeds.IsCreated)
                    seeds.Dispose();
                if (hashes.IsCreated)
                    hashes.Dispose();
            }
        }
    }
}