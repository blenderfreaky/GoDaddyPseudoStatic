namespace GoDaddyPseudoStatic.RunSchedules
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    public record RunSerial(IReadOnlyList<RunSerialEntry> Schedules, DateTime? PhaseOffset = null) : IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start)
        {
            var ticks = start.Subtract(PhaseOffset ?? DateTime.UnixEpoch).Ticks;

            var intervalDuration = Schedules.Sum(x => x.Duration.Ticks);
            ticks %= intervalDuration;

            foreach (var (duration, schedule) in Schedules)
            {
                if (ticks >= duration.Ticks) ticks -= duration.Ticks;
                else return schedule.GetNextExecution(start);
            }

            return DateTime.MaxValue;
        }
    }

    public record RunSerialEntry(TimeSpan Duration, IRunSchedule Schedule);
}