using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Block filter that suppresses all alerts.
    /// Designed for Unity game development with zero-allocation patterns.
    /// </summary>
    internal sealed class BlockAlertFilter : BaseAlertFilter
    {
        private readonly FixedString64Bytes _name;

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => _name;

        /// <summary>
        /// Initializes a new instance of the BlockAlertFilter class.
        /// </summary>
        /// <param name="messageBusService">Message bus service for publishing filter events</param>
        /// <param name="name">The name of this filter</param>
        public BlockAlertFilter(IMessageBusService messageBusService, string name = "BlockFilter") : base(messageBusService)
        {
            _name = name;
        }

        /// <summary>
        /// Core implementation to determine if filter can handle an alert.
        /// Block filter handles all alerts.
        /// </summary>
        /// <param name="alert">Alert to check</param>
        /// <returns>Always returns true</returns>
        protected override bool CanHandleCore(Alert alert) => true;

        /// <summary>
        /// Core implementation of alert evaluation.
        /// Always suppresses all alerts.
        /// </summary>
        /// <param name="alert">Alert to evaluate</param>
        /// <param name="context">Filtering context</param>
        /// <returns>Suppress result</returns>
        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            return FilterResult.Suppress("Block filter suppresses all alerts");
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// Block filter has no configuration to apply.
        /// </summary>
        /// <param name="configuration">Configuration to apply</param>
        /// <param name="correlationId">Correlation ID</param>
        /// <returns>Always returns true</returns>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            // Block filter has no configuration
            return true;
        }

        /// <summary>
        /// Core implementation of configuration validation.
        /// Block filter accepts any configuration.
        /// </summary>
        /// <param name="configuration">Configuration to validate</param>
        /// <returns>Always returns valid</returns>
        protected override FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration)
        {
            // Block filter accepts any configuration
            return FilterValidationResult.Valid();
        }
    }
}