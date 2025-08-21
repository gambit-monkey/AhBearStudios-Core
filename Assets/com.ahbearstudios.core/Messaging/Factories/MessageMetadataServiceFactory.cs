using AhBearStudios.Core.Messaging.Services;
using Cysharp.Threading.Tasks;

namespace AhBearStudios.Core.Messaging.Factories;

/// <summary>
/// Factory for creating message metadata services.
/// Simple creation following CLAUDE.md guidelines - no lifecycle management.
/// </summary>
public sealed class MessageMetadataServiceFactory : IMessageMetadataServiceFactory
{
    /// <inheritdoc />
    public async UniTask<IMessageMetadataService> CreateServiceAsync()
    {
        await UniTask.SwitchToMainThread();

        // Simple creation - factory doesn't track the created instance
        return new MessageMetadataService();
    }
}