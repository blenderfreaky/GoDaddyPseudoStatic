namespace GoDaddyPseudoStatic.RunSchedules
{
    using System;

    public record RunNever() : IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start) => DateTime.MaxValue;
    }
}