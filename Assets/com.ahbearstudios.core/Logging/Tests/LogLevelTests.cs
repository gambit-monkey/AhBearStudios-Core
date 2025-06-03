using NUnit.Framework;
using AhBearStudios.Core.Logging;

namespace AhBearStudios.Core.Tests.Logging
{
    /// <summary>
    /// Tests for LogLevel enum functionality
    /// </summary>
    [TestFixture]
    public class LogLevelTests
    {
        [Test]
        public void LogLevel_AllValues_AreWithinByteRange()
        {
            // Arrange & Act & Assert
            Assert.That((byte)LogLevel.Trace, Is.EqualTo(0));
            Assert.That((byte)LogLevel.Debug, Is.EqualTo(10));
            Assert.That((byte)LogLevel.Info, Is.EqualTo(20));
            Assert.That((byte)LogLevel.Warning, Is.EqualTo(30));
            Assert.That((byte)LogLevel.Error, Is.EqualTo(40));
            Assert.That((byte)LogLevel.Critical, Is.EqualTo(50));
        }

        [Test]
        public void LogLevel_Comparison_WorksCorrectly()
        {
            // Arrange & Act & Assert
            Assert.That(LogLevel.Trace < LogLevel.Debug, Is.True);
            Assert.That(LogLevel.Debug < LogLevel.Info, Is.True);
            Assert.That(LogLevel.Info < LogLevel.Warning, Is.True);
            Assert.That(LogLevel.Warning < LogLevel.Error, Is.True);
            Assert.That(LogLevel.Error < LogLevel.Critical, Is.True);
        }

        [Test]
        public void LogLevel_ToString_ReturnsCorrectNames()
        {
            // Arrange & Act & Assert
            Assert.That(LogLevel.Trace.ToString(), Is.EqualTo("Trace"));
            Assert.That(LogLevel.Debug.ToString(), Is.EqualTo("Debug"));
            Assert.That(LogLevel.Info.ToString(), Is.EqualTo("Info"));
            Assert.That(LogLevel.Warning.ToString(), Is.EqualTo("Warning"));
            Assert.That(LogLevel.Error.ToString(), Is.EqualTo("Error"));
            Assert.That(LogLevel.Critical.ToString(), Is.EqualTo("Critical"));
        }

        [Test]
        public void LogLevel_CanBeCastToByte()
        {
            // Arrange
            LogLevel level = LogLevel.Error;

            // Act
            byte byteValue = (byte)level;

            // Assert
            Assert.That(byteValue, Is.EqualTo(40));
        }

        [Test]
        public void LogLevel_CanBeCastFromByte()
        {
            // Arrange
            byte byteValue = 20;

            // Act
            LogLevel level = (LogLevel)byteValue;

            // Assert
            Assert.That(level, Is.EqualTo(LogLevel.Info));
        }
    }
}