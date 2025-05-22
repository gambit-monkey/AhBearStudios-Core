using System;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using Unity.Profiling;

namespace AhBearStudios.Core.Messaging.Handlers
{
    /// <summary>
    /// Performance handler for MessagePipe that integrates with the profiling system.
    /// </summary>
    internal sealed class PerformanceHandler : MessageHandlerFilter<object>
    {
        private readonly IProfiler _profiler;
        
        /// <summary>
        /// Initializes a new instance of the PerformanceHandler class.
        /// </summary>
        /// <param name="profiler">The profiler to use for performance monitoring.</param>
        public PerformanceHandler(IProfiler profiler)
        {
            _profiler = profiler ?? throw new ArgumentNullException(nameof(profiler));
        }
        
        /// <inheritdoc />
        public override void Handle(object message, Action<object> next)
        {
            var messageType = message.GetType();
            var tag = new ProfilerTag(new ProfilerCategory("MessageBus"), $"HandleMessage_{messageType.Name}");
            
            using var scope = _profiler.BeginScope(tag);
            next(message);
        }
    }
}