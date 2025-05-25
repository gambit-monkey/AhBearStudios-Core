using Unity.Jobs;
using Unity.Collections;
using Unity.Mathematics;

/// <summary>
/// Job that generates random positions for object spawning
/// </summary>
public struct GeneratePositionsJob : IJob
{
    /// <summary>
    /// Random seed for position generation
    /// </summary>
    public uint Seed;
    
    /// <summary>
    /// Number of positions to generate
    /// </summary>
    public int Count;
    
    /// <summary>
    /// Minimum bounds for position generation
    /// </summary>
    public float3 MinBounds;
    
    /// <summary>
    /// Maximum bounds for position generation
    /// </summary>
    public float3 MaxBounds;
    
    /// <summary>
    /// Output array for generated positions
    /// </summary>
    public NativeArray<float3> Positions;

    /// <summary>
    /// Executes the job to generate random positions
    /// </summary>
    public void Execute()
    {
        var random = new Unity.Mathematics.Random(Seed);
        
        for (int i = 0; i < Count; i++)
        {
            Positions[i] = random.NextFloat3(MinBounds, MaxBounds);
        }
    }
}