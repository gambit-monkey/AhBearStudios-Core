using AhBearStudios.Core.Alerting.Models;

namespace AhBearStudios.Core.Alerting.Filters
{
    /// <summary>
    /// Block filter that suppresses all alerts.
    /// </summary>
    internal sealed class BlockAlertFilter : BaseAlertFilter
    {
        public BlockAlertFilter(string name) : base(name) { }

        public override bool CanHandle(Alert alert) => true;

        public override FilterResult Evaluate(Alert alert, FilterContext context)
        {
            return FilterResult.Suppress(alert, "Block filter");
        }
    }
}