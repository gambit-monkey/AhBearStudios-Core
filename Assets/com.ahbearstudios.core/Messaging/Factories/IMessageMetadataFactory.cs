using AhBearStudios.Core.Messaging.Configs;
using AhBearStudios.Core.Messaging.Models;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating MessageMetadata instances from configurations.
/// Simple creation only - no lifecycle management.
/// </summary>
public interface IMessageMetadataFactory
{
    /// <summary>
    /// Creates a MessageMetadata instance from a validated configuration.
    /// </summary>
    /// <param name="config">The validated configuration from MessageMetadataBuilder</param>
    /// <returns>A new MessageMetadata instance</returns>
    MessageMetadata Create(MessageMetadataConfig config);

    /// <summary>
    /// Creates a default MessageMetadata instance with minimal required fields.
    /// </summary>
    /// <returns>A new MessageMetadata instance with default values</returns>
    MessageMetadata CreateDefault();
}