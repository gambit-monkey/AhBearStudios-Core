using NUnit.Framework;
using Unity.Collections;
using AhBearStudios.Core.Logging.Data;
using System;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Tests for LogProperties struct functionality
    /// </summary>
    [TestFixture]
    public class LogPropertiesTests
    {
        [TearDown]
        public void TearDown()
        {
            // Ensure any native containers are disposed
        }

        [Test]
        public void LogProperties_DefaultConstructor_IsEmpty()
        {
            // Act
            var properties = default(LogProperties);

            // Assert
            Assert.That(properties.IsEmpty, Is.True);
            Assert.That(properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void LogProperties_Constructor_WithAllocator_CreatesValidContainer()
        {
            // Act
            using var properties = new LogProperties(Allocator.Temp);

            // Assert
            Assert.That(properties.IsCreated, Is.True);
            Assert.That(properties.IsEmpty, Is.True);
            Assert.That(properties.Count, Is.EqualTo(0));
        }

        [Test]
        public void LogProperties_Add_AddsKeyValuePair()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            var key = "TestKey";
            var value = "TestValue";

            // Act
            properties.Add(key, value);

            // Assert
            Assert.That(properties.Count, Is.EqualTo(1));
            Assert.That(properties.IsEmpty, Is.False);
            Assert.That(properties.ContainsKey(key), Is.True);
        }

        [Test]
        public void LogProperties_Add_MultipleItems_IncrementsCount()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);

            // Act
            properties.Add("Key1", "Value1");
            properties.Add("Key2", "Value2");
            properties.Add("Key3", "Value3");

            // Assert
            Assert.That(properties.Count, Is.EqualTo(3));
        }

        [Test]
        public void LogProperties_ContainsKey_ReturnsCorrectValue()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("ExistingKey", "Value");

            // Act & Assert
            Assert.That(properties.ContainsKey("ExistingKey"), Is.True);
            Assert.That(properties.ContainsKey("NonExistentKey"), Is.False);
        }

        [Test]
        public void LogProperties_TryGetValue_ReturnsCorrectResults()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            var key = "TestKey";
            var expectedValue = "TestValue";
            properties.Add(key, expectedValue);

            // Act
            var found = properties.TryGetValue(key, out var actualValue);
            var notFound = properties.TryGetValue("NonExistentKey", out var missingValue);

            // Assert
            Assert.That(found, Is.True);
            Assert.That(actualValue.ToString(), Is.EqualTo(expectedValue));
            Assert.That(notFound, Is.False);
            Assert.That(missingValue.ToString(), Is.Empty);
        }

        [Test]
        public void LogProperties_Remove_RemovesItem()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            var key = "TestKey";
            properties.Add(key, "TestValue");

            // Act
            var removed = properties.Remove(key);

            // Assert
            Assert.That(removed, Is.True);
            Assert.That(properties.Count, Is.EqualTo(0));
            Assert.That(properties.ContainsKey(key), Is.False);
        }

        [Test]
        public void LogProperties_Remove_NonExistentKey_ReturnsFalse()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);

            // Act
            var removed = properties.Remove("NonExistentKey");

            // Assert
            Assert.That(removed, Is.False);
        }

        [Test]
        public void LogProperties_Clear_RemovesAllItems()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key1", "Value1");
            properties.Add("Key2", "Value2");

            // Act
            properties.Clear();

            // Assert
            Assert.That(properties.Count, Is.EqualTo(0));
            Assert.That(properties.IsEmpty, Is.True);
        }

        [Test]
        public void LogProperties_GetKeys_ReturnsAllKeys()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key1", "Value1");
            properties.Add("Key2", "Value2");

            // Act
            using var keys = properties.GetKeys(Allocator.Temp);

            // Assert
            Assert.That(keys.Length, Is.EqualTo(2));
            var keyArray = keys.ToArray();
            Assert.That(keyArray, Contains.Item(new FixedString64Bytes("Key1")));
            Assert.That(keyArray, Contains.Item(new FixedString64Bytes("Key2")));
        }

        [Test]
        public void LogProperties_GetValues_ReturnsAllValues()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key1", "Value1");
            properties.Add("Key2", "Value2");

            // Act
            using var values = properties.GetValues(Allocator.Temp);

            // Assert
            Assert.That(values.Length, Is.EqualTo(2));
            var valueArray = values.ToArray();
            Assert.That(valueArray, Contains.Item(new FixedString128Bytes("Value1")));
            Assert.That(valueArray, Contains.Item(new FixedString128Bytes("Value2")));
        }

        [Test]
        public void LogProperties_ToString_ReturnsFormattedString()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key1", "Value1");
            properties.Add("Key2", "Value2");

            // Act
            var result = properties.ToString();

            // Assert
            Assert.That(result, Does.Contain("Key1"));
            Assert.That(result, Does.Contain("Value1"));
            Assert.That(result, Does.Contain("Key2"));
            Assert.That(result, Does.Contain("Value2"));
        }

        [Test]
        public void LogProperties_ToString_EmptyProperties_ReturnsEmptyString()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);

            // Act
            var result = properties.ToString();

            // Assert
            Assert.That(result, Is.EqualTo(string.Empty));
        }

        [Test]
        public void LogProperties_WithLongKey_TruncatesCorrectly()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            var longKey = new string('K', 100); // Longer than FixedString64Bytes

            // Act & Assert
            Assert.DoesNotThrow(() => properties.Add(longKey, "Value"));
            Assert.That(properties.Count, Is.EqualTo(1));
        }

        [Test]
        public void LogProperties_WithLongValue_TruncatesCorrectly()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            var longValue = new string('V', 200); // Longer than FixedString128Bytes

            // Act & Assert
            Assert.DoesNotThrow(() => properties.Add("Key", longValue));
            Assert.That(properties.Count, Is.EqualTo(1));
        }

        [Test]
        public void LogProperties_AddDuplicateKey_UpdatesValue()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            var key = "TestKey";

            // Act
            properties.Add(key, "Value1");
            properties.Add(key, "Value2");

            // Assert
            Assert.That(properties.Count, Is.EqualTo(1));
            Assert.That(properties.TryGetValue(key, out var value), Is.True);
            Assert.That(value.ToString(), Is.EqualTo("Value2"));
        }

        [Test]
        public void LogProperties_Capacity_CanBeExpanded()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);

            // Act - Add many items to test capacity expansion
            for (int i = 0; i < 20; i++)
            {
                properties.Add($"Key{i}", $"Value{i}");
            }

            // Assert
            Assert.That(properties.Count, Is.EqualTo(20));
        }

        [Test]
        public void LogProperties_Dispose_ReleasesMemory()
        {
            // Arrange
            var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key", "Value");

            // Act
            properties.Dispose();

            // Assert
            Assert.That(properties.IsCreated, Is.False);
        }

        [Test]
        public void LogProperties_IsCreated_ReflectsState()
        {
            // Arrange
            var properties = new LogProperties(Allocator.Temp);

            // Assert - Initially created
            Assert.That(properties.IsCreated, Is.True);

            // Act
            properties.Dispose();

            // Assert - After disposal
            Assert.That(properties.IsCreated, Is.False);
        }

        [Test]
        public void LogProperties_Equality_WorksCorrectly()
        {
            // Arrange
            using var props1 = new LogProperties(Allocator.Temp);
            using var props2 = new LogProperties(Allocator.Temp);
            using var props3 = new LogProperties(Allocator.Temp);

            props1.Add("Key1", "Value1");
            props2.Add("Key1", "Value1");
            props3.Add("Key1", "Value2");

            // Act & Assert
            Assert.That(props1.Equals(props2), Is.True);
            Assert.That(props1.Equals(props3), Is.False);
        }

        [Test]
        public void LogProperties_GetHashCode_IsConsistent()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);
            properties.Add("Key", "Value");

            // Act
            var hash1 = properties.GetHashCode();
            var hash2 = properties.GetHashCode();

            // Assert
            Assert.That(hash1, Is.EqualTo(hash2));
        }

        [Test]
        public void LogProperties_AddNullOrEmptyValues_HandlesGracefully()
        {
            // Arrange
            using var properties = new LogProperties(Allocator.Temp);

            // Act & Assert
            Assert.DoesNotThrow(() => properties.Add("Key1", null));
            Assert.DoesNotThrow(() => properties.Add("Key2", string.Empty));
            Assert.That(properties.Count, Is.EqualTo(2));
        }

        [Test]
        public void LogProperties_ThreadSafety_BasicOperations()
        {
            // Note: This is a basic test. Full thread safety testing would require more complex scenarios
            // Arrange
            using var properties = new LogProperties(Allocator.TempJob);

            // Act & Assert - Should not throw in single-threaded context
            Assert.DoesNotThrow(() =>
            {
                properties.Add("Key1", "Value1");
                properties.ContainsKey("Key1");
                properties.Remove("Key1");
            });
        }
    }
}