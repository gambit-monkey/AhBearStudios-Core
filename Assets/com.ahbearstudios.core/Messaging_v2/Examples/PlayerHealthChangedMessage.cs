using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Messaging.Attributes;
using AhBearStudios.Core.Messaging.Configuration;
using AhBearStudios.Core.Messaging.Handlers;
using AhBearStudios.Core.Messaging.Installation;
using AhBearStudios.Core.Messaging.Interfaces;
using AhBearStudios.Core.Profiling.Interfaces;
using UnityEngine;

namespace AhBearStudios.Core.Messaging.Examples
{
    /// <summary>
    /// Sample message data class.
    /// </summary>
    public class PlayerHealthChangedMessage
    {
        /// <summary>
        /// Gets or sets the player ID.
        /// </summary>
        public string PlayerId { get; set; }

        /// <summary>
        /// Gets or sets the current health value.
        /// </summary>
        public float CurrentHealth { get; set; }

        /// <summary>
        /// Gets or sets the maximum health value.
        /// </summary>
        public float MaxHealth { get; set; }
    }

    /// <summary>
    /// Sample message handler class.
    /// </summary>
    [MessageHandler(0, true)]
    public class PlayerHealthChangedHandler : BaseMessageHandler<PlayerHealthChangedMessage>
    {
        /// <summary>
        /// Initializes a new instance of the PlayerHealthChangedHandler class.
        /// </summary>
        /// <param name="messageBus">The message bus to use.</param>
        /// <param name="logger">The logger to use for logging.</param>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public PlayerHealthChangedHandler(IMessageBus messageBus, IBurstLogger logger, IProfiler profiler)
            : base(messageBus, logger, profiler)
        {
        }

        /// <inheritdoc />
        protected override void HandleMessage(PlayerHealthChangedMessage message)
        {
            // Process the message
            Debug.Log($"Player {message.PlayerId} health changed to {message.CurrentHealth}/{message.MaxHealth}");
        }
    }

    /// <summary>
    /// Sample installer class for the message bus.
    /// </summary>
    public class GameLifetimeScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            // Create a message bus configuration
            var config = new MessageBusConfigBuilder()
                .WithDiagnosticLogging()
                .WithPerformanceProfiling()
                .WithMaxSubscribers(10)
                .Build();

            // Install the message bus
            builder.RegisterComponent<MessageBusInstaller>()
                .WithParameter(config);

            // Register message handlers
            builder.Register<PlayerHealthChangedHandler>(Lifetime.Singleton);
        }
    }
}
    
    