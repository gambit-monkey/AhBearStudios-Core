using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Pass-through filter that allows all alerts.
    /// </summary>
    internal sealed class PassThroughAlertFilter : BaseAlertFilter
    {
        public PassThroughAlertFilter(string name) : base(name) { }

        public override bool CanHandle(Alert alert) => true;

        public override FilterResult Evaluate(Alert alert, FilterContext context)
        {
            return FilterResult.Allow(alert, "Pass-through filter");
        }
    }
}