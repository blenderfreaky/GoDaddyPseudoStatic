namespace GoDaddyPseudoStatic.RunSchedule
{
    using System;
    using System.Collections.Generic;
    using System.Linq;

    [JsonInterfaceConverter(typeof(InheritanceConverter<IRunSchedule>))]
    public interface IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start);
    }

    public record RunParallel(IReadOnlyList<IRunSchedule> Schedules)
    {
        public DateTime GetNextExecution(DateTime start) => Schedules.Min(x => x.GetNextExecution(start));
    }

    public record RunSerial(IReadOnlyList<(TimeSpan Duration, IRunSchedule Schedule)> Schedules, DateTime? PhaseOffset = null) : IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start)
        {
            var ticks = start.Ticks;
            ticks -= (PhaseOffset ?? DateTime.UnixEpoch).Ticks;

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

    public record RunNever() : IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start) => DateTime.MaxValue;
    }

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
