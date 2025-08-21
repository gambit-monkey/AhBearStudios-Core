using AhBearStudios.Core.Messaging.Services;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory interface for creating message type statistics services.
/// Follows CLAUDE.md Builder → Config → Factory → Service pattern.
/// </summary>
public interface IMessageTypeStatisticsServiceFactory
{
    /// <summary>
    /// Creates a message type statistics service instance.
    /// </summary>
    /// <returns>Configured message type statistics service</returns>
    UniTask<IMessageTypeStatisticsService> CreateServiceAsync();
}