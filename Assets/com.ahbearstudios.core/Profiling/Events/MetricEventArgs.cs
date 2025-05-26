// using System;
//
// namespace AhBearStudios.Core.Profiling.Events
// {
//     /// <summary>
//     /// Event data for system metric events
//     /// </summary>
//     public class MetricEventArgs : EventArgs
//     {
//         /// <summary>
//         /// The metric that triggered the event
//         /// </summary>
//         public SystemMetric Metric { get; }
//         
//         /// <summary>
//         /// The value that triggered the event
//         /// </summary>
//         public double Value { get; }
//         
//         /// <summary>
//         /// The tag of the metric that triggered the event
//         /// </summary>
//         public ProfilerTag MetricTag => Metric?.Tag ?? ProfilerTag.Uncategorized;
//         
//         /// <summary>
//         /// The unit of the metric value
//         /// </summary>
//         public string Unit => Metric?.Unit;
//         
//         /// <summary>
//         /// Create new metric event args
//         /// </summary>
//         public MetricEventArgs(SystemMetric metric, double value)
//         {
//             Metric = metric;
//             Value = value;
//         }
//     }
// }