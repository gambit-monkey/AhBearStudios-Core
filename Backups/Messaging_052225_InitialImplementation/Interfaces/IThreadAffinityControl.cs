using System;

namespace AhBearStudios.Core.Messaging.Interfaces
{
    /// <summary>
    /// Interface for controlling thread affinity of message processing
    /// </summary>
    public interface IThreadAffinityControl
    {
        /// <summary>
        /// Gets or sets the thread affinity mode
        /// </summary>
        ThreadAffinityMode ThreadAffinityMode { get; set; }
    
        /// <summary>
        /// Gets a value indicating whether the current thread is the main thread
        /// </summary>
        bool IsMainThread { get; }
    
        /// <summary>
        /// Executes an action on the main thread
        /// </summary>
        /// <param name="action">The action to execute</param>
        /// <param name="waitForCompletion">Whether to wait for the action to complete</param>
        void ExecuteOnMainThread(Action action, bool waitForCompletion = false);
    
        /// <summary>
        /// Schedules an action to be executed on the main thread
        /// </summary>
        /// <param name="action">The action to execute</param>
        void ScheduleOnMainThread(Action action);
    }
}