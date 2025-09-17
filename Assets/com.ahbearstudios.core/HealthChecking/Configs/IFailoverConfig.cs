using System;
using System.Collections.Generic;
using AhBearStudios.Core.HealthChecking.Models;

namespace AhBearStudios.Core.HealthChecking.Configs
{
    /// <summary>
    /// Configuration interface for failover behavior in circuit breakers
    /// </summary>
    public interface IFailoverConfig
    {
        /// <summary>
        /// Whether to enable failover when circuit is open
        /// </summary>
        bool EnableFailover { get; }

        /// <summary>
        /// List of failover endpoints or alternatives
        /// </summary>
        List<string> FailoverEndpoints { get; }

        /// <summary>
        /// Timeout for failover attempts
        /// </summary>
        TimeSpan FailoverTimeout { get; }

        /// <summary>
        /// Strategy to use when failover is enabled
        /// </summary>
        FailoverStrategy FailoverStrategy { get; }

        /// <summary>
        /// Default value to return when using ReturnDefault failover strategy
        /// </summary>
        object FailoverDefaultValue { get; }
    }
}