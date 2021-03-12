namespace GoDaddyPseudoStatic.RunSchedules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public record RunParallel(IReadOnlyList<IRunSchedule> Schedules) : IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start) => Schedules.Min(x => x.GetNextExecution(start));
    }
}