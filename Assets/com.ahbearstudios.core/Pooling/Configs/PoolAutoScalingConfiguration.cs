using System;

namespace AhBearStudios.Core.Pooling.Configs
{
    public sealed class PoolAutoScalingConfiguration
    {
        public bool EnableAutoScaling { get; init; } = true;
        
        public TimeSpan CheckInterval { get; init; } = TimeSpan.FromSeconds(30);
        
        public double ScaleUpThreshold { get; init; } = 0.8;
        
        public double ScaleDownThreshold { get; init; } = 0.3;
        
        public int MinScaleUpAmount { get; init; } = 1;
        
        public int MaxScaleUpAmount { get; init; } = 10;
        
        public double ScaleUpFactor { get; init; } = 1.5;
        
        public double ScaleDownFactor { get; init; } = 0.5;
        
        public int MinPoolSize { get; init; } = 1;
        
        public int MaxPoolSize { get; init; } = 1000;
        
        public bool LogScalingDecisions { get; init; } = true;
        
        public bool EnablePredictiveScaling { get; init; } = false;
        
        public TimeSpan PredictionWindow { get; init; } = TimeSpan.FromMinutes(5);
        
        public int MinOperationCountForScaling { get; init; } = 10;
        
        public bool EnableCooldownPeriod { get; init; } = true;
        
        public TimeSpan ScaleUpCooldown { get; init; } = TimeSpan.FromSeconds(60);
        
        public TimeSpan ScaleDownCooldown { get; init; } = TimeSpan.FromSeconds(120);

        public static PoolAutoScalingConfiguration Default => new();
        
        public static PoolAutoScalingConfiguration Conservative => new()
        {
            EnableAutoScaling = true,
            CheckInterval = TimeSpan.FromMinutes(1),
            ScaleUpThreshold = 0.9,
            ScaleDownThreshold = 0.2,
            ScaleUpFactor = 1.2,
            ScaleDownFactor = 0.8,
            ScaleUpCooldown = TimeSpan.FromMinutes(2),
            ScaleDownCooldown = TimeSpan.FromMinutes(5),
            EnablePredictiveScaling = false
        };
        
        public static PoolAutoScalingConfiguration Aggressive => new()
        {
            EnableAutoScaling = true,
            CheckInterval = TimeSpan.FromSeconds(15),
            ScaleUpThreshold = 0.7,
            ScaleDownThreshold = 0.4,
            ScaleUpFactor = 2.0,
            ScaleDownFactor = 0.3,
            ScaleUpCooldown = TimeSpan.FromSeconds(30),
            ScaleDownCooldown = TimeSpan.FromSeconds(60),
            EnablePredictiveScaling = true
        };
        
        public static PoolAutoScalingConfiguration Disabled => new()
        {
            EnableAutoScaling = false
        };
    }
}