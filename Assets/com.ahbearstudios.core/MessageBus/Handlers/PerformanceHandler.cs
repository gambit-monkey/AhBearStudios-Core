using System;
using AhBearStudios.Core.Profiling;
using AhBearStudios.Core.Profiling.Interfaces;
using MessagePipe;
using Unity.Profiling;

namespace AhBearStudios.Core.MessageBus.Handlers
{
    /// <summary>
    /// Performance handler for MessagePipe that integrates with the profiling system.
    /// </summary>
    internal sealed class PerformanceHandler : MessageHandlerFilter<object>
    {
        private readonly IProfilerService _profilerService;
        
        /// <summary>
        /// Initializes a new instance of the PerformanceHandler class.
        /// </summary>
        /// <param name="profilerService">The profiler to use for performance monitoring.</param>
        public PerformanceHandler(IProfilerService profilerService)
        {
            _profilerService = profilerService ?? throw new ArgumentNullException(nameof(profilerService));
        }
        
        /// <inheritdoc />
        public override void Handle(object message, Action<object> next)
        {
            var messageType = message.GetType();
            var tag = new ProfilerTag(new ProfilerCategory("MessageBusService"), $"HandleMessage_{messageType.Name}");
            
            using var scope = _profilerService.BeginScope(tag);
            next(message);
        }
    }
}