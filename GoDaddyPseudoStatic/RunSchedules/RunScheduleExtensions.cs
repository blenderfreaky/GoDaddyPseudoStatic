namespace GoDaddyPseudoStatic.RunSchedules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class RunScheduleExtensions
    {
        public static IEnumerable<DateTime> GetNextExecutions(this IRunSchedule schedule, DateTime start)
        {
            while (true)
            {
                var next = schedule.GetNextExecution(start);
                yield return next;
                start = next;
            }
        }
    }
}
