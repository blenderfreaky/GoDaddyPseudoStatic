namespace GoDaddyPseudoStatic.RunSchedules
{
    using System;

    public record RunInterval(TimeSpan Interval, DateTime? PhaseOffset = null) : IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start)
        {
            var ticks = start.Ticks;
            ticks -= (PhaseOffset ?? DateTime.UnixEpoch).Ticks;
            ticks %= Interval.Ticks;

            return start.AddTicks(Interval.Ticks - ticks);
        }
    }
}