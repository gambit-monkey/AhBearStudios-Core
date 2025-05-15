using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Profiling;
using UnityEngine;

namespace AhBearStudios.Core.Profiling.Examples
{
    /// <summary>
    /// Sample job that uses profiler markers with Burst compilation
    /// </summary>
    [BurstCompile]
    public struct SampleProfiledJob : IJob
    {
        // Static ProfilerMarker is safe with Burst
        // Note: string must be a literal, not constructed at runtime
        private static readonly ProfilerMarker JobMarker = new ProfilerMarker("SampleProfiledJob.Execute");
        
        // Data for job
        public NativeArray<float> Input;
        public NativeArray<float> Output;
        
        // Sample work parameter
        public float Multiplier;
        
        /// <summary>
        /// Execute job with profiling markers
        /// </summary>
        public void Execute()
        {
            // Begin profiling this job
            JobMarker.Begin();
            
            try
            {
                // Example computation
                for (int i = 0; i < Input.Length; i++)
                {
                    Output[i] = ProcessValue(Input[i]);
                }
            }
            finally
            {
                // Always end the marker, even if an exception occurs
                JobMarker.End();
            }
        }
        
        /// <summary>
        /// Process a single value (example computation)
        /// </summary>
        private float ProcessValue(float value)
        {
            // Simple math operation as example
            return value * Multiplier;
        }
    }
    
    /// <summary>
    /// More complex job that uses multiple profiler markers with Burst compilation
    /// </summary>
    [BurstCompile]
    public struct ComplexProfiledJob : IJobParallelFor
    {
        // Multiple static markers for different sections
        private static readonly ProfilerMarker MainMarker = new ProfilerMarker("ComplexProfiledJob.Execute");
        private static readonly ProfilerMarker ProcessMarker = new ProfilerMarker("ComplexProfiledJob.Process");
        private static readonly ProfilerMarker FinalizeMarker = new ProfilerMarker("ComplexProfiledJob.Finalize");
        
        // Data for job
        [ReadOnly] public NativeArray<float> Input;
        public NativeArray<float> Output;
        
        // Job parameters
        public float Multiplier;
        public float Offset;
        
        /// <summary>
        /// Execute job with profiling markers for each iteration
        /// </summary>
        public void Execute(int index)
        {
            // Begin profiling this iteration
            MainMarker.Begin();
            
            try
            {
                float value = Input[index];
                
                // Profile the processing step
                ProcessMarker.Begin();
                float processed = value * Multiplier;
                ProcessMarker.End();
                
                // Profile the finalization step
                FinalizeMarker.Begin();
                Output[index] = processed + Offset;
                FinalizeMarker.End();
            }
            finally
            {
                // Always end the marker, even if an exception occurs
                MainMarker.End();
            }
        }
    }
    
    /// <summary>
    /// Example class that demonstrates using profiled jobs
    /// </summary>
    public class JobProfilerExample : MonoBehaviour
    {
        [SerializeField] private int _dataSize = 10000;
        [SerializeField] private float _multiplier = 2.0f;
        [SerializeField] private float _offset = 1.0f;
        [SerializeField] private bool _useParallelJob = true;
        [SerializeField] private bool _logResults = false;
        
        // ProfilerMarker for the main thread work
        private static readonly ProfilerMarker PrepareMarker = new ProfilerMarker("JobProfilerExample.Prepare");
        private static readonly ProfilerMarker ScheduleMarker = new ProfilerMarker("JobProfilerExample.Schedule");
        private static readonly ProfilerMarker CompleteMarker = new ProfilerMarker("JobProfilerExample.Complete");
        
        private void Update()
        {
            // Profile preparation
            PrepareMarker.Begin();
            
            // Create input and output arrays
            NativeArray<float> input = new NativeArray<float>(_dataSize, Allocator.TempJob);
            NativeArray<float> output = new NativeArray<float>(_dataSize, Allocator.TempJob);
            
            // Fill input with sample data
            for (int i = 0; i < _dataSize; i++)
            {
                input[i] = UnityEngine.Random.Range(0f, 100f);
            }
            
            PrepareMarker.End();
            
            // Profile job scheduling
            ScheduleMarker.Begin();
            
            JobHandle jobHandle;
            
            // Schedule appropriate job type
            if (_useParallelJob)
            {
                var job = new ComplexProfiledJob
                {
                    Input = input,
                    Output = output,
                    Multiplier = _multiplier,
                    Offset = _offset
                };
                
                // Schedule parallel job
                jobHandle = job.Schedule(_dataSize, 64);
            }
            else
            {
                var job = new SampleProfiledJob
                {
                    Input = input,
                    Output = output,
                    Multiplier = _multiplier
                };
                
                // Schedule simple job
                jobHandle = job.Schedule();
            }
            
            ScheduleMarker.End();
            
            // Profile job completion
            CompleteMarker.Begin();
            
            // Wait for job to complete
            jobHandle.Complete();
            
            // Log results if requested
            if (_logResults)
            {
                Debug.Log($"Job completed. First few results: {output[0]}, {output[1]}, {output[2]}");
            }
            
            // Clean up
            input.Dispose();
            output.Dispose();
            
            CompleteMarker.End();
        }
    }
}