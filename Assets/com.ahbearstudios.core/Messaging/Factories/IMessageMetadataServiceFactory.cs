using AhBearStudios.Core.Messaging.Services;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating message metadata services.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageMetadataServiceFactory
{
    /// <summary>
    /// Creates a message metadata service instance.
    /// </summary>
    /// <returns>Configured message metadata service</returns>
    UniTask<IMessageMetadataService> CreateServiceAsync();
}