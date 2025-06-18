using Unity.Collections;
using AhBearStudios.Core.Logging;
using AhBearStudios.Core.Alerts.Interfaces;
using AhBearStudios.Core.Alerts.Configuration;
using AhBearStudios.Core.Alerts.Targets;

namespace AhBearStudios.Core.Alerts.Factories
{
    public static class AlertServiceFactory
    {
        public static IAlertService CreateDefaultAlertService(ILogService log, AlertConfiguration config, Allocator allocator)
        {
            var service = new AlertService(allocator, config.MinimumSeverity, config.CooldownMilliseconds);
            service.RegisterTarget(new LoggingAlertTarget(log, config));
            return service;
        }
    }
}