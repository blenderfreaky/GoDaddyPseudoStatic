namespace GoDaddyPseudoStatic.RunSchedules
{
    using System;

    [JsonInterfaceConverter(typeof(InheritanceConverter<IRunSchedule>))]
    public interface IRunSchedule
    {
        public DateTime GetNextExecution(DateTime start) => DateTime.MaxValue;
    }
}