using System;
using System.Linq;
using Windows.ApplicationModel.Background;
using Light.Managed.Tools;

namespace Light.Managed.Background
{
    internal static class BackgroundTaskRegisterationHelper
    {
        private const string BackgroundTaskName = "Light Media Library Background Helper";

        public static async void Register()
        {
            try
            {
                var trigger = new TimeTrigger(60, false);
                var result = await BackgroundExecutionManager.RequestAccessAsync();
                if (result == BackgroundAccessStatus.DeniedByUser ||
                    result == BackgroundAccessStatus.DeniedBySystemPolicy)
                {
                    return;
                }
                var query = from c in BackgroundTaskRegistration.AllTasks
                    where c.Value.Name == BackgroundTaskName
                    select c;
                if (query.Any()) return;
                var builder = new BackgroundTaskBuilder
                {
                    Name = BackgroundTaskName,
                    IsNetworkRequested = true,
                    CancelOnConditionLoss = true,
                    TaskEntryPoint = "Light.Maintenance.MaintenanceTask"
                };

                builder.AddCondition(new SystemCondition(SystemConditionType.BackgroundWorkCostNotHigh));
                builder.AddCondition(new SystemCondition(SystemConditionType.InternetAvailable));
                builder.SetTrigger(trigger);
                // builder.Register();
            }
            catch (Exception ex)
            {
                TelemetryHelper.TrackExceptionAsync(ex);
            }
        }
    }
}
