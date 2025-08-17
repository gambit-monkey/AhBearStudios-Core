using System;
using System.Collections.Generic;
using Unity.Collections;
using AhBearStudios.Core.Alerting.Models;
using AhBearStudios.Core.Messaging;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Pass-through filter that allows all alerts.
    /// </summary>
    internal sealed class PassThroughAlertFilter : BaseAlertFilter
    {
        private readonly FixedString64Bytes _name;

        /// <summary>
        /// Gets the unique name identifier for this filter.
        /// </summary>
        public override FixedString64Bytes Name => _name;

        public PassThroughAlertFilter(IMessageBusService messageBusService, string name = "PassThroughFilter") : base(messageBusService)
        {
            _name = new FixedString64Bytes(name);
        }

        protected override bool CanHandleCore(Alert alert) => true;

        protected override FilterResult EvaluateCore(Alert alert, FilterContext context)
        {
            return FilterResult.Allow("Pass-through filter");
        }

        /// <summary>
        /// Core implementation of configuration application.
        /// </summary>
        protected override bool ConfigureCore(Dictionary<string, object> configuration, Guid correlationId)
        {
            // Pass-through filter has no configuration options
            return true;
        }

        /// <summary>
        /// Core implementation of configuration validation.
        /// </summary>
        protected override FilterValidationResult ValidateConfigurationCore(Dictionary<string, object> configuration)
        {
            return FilterValidationResult.Valid();
        }
    }
}