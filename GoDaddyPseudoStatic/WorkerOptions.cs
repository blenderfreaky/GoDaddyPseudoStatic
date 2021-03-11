namespace GoDaddyPseudoStatic
{
    using GoDaddyPseudoStatic.RunSchedule;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text.Json;

    public class WorkerOptions
    {
        /// <summary>
        /// The domain to update.
        /// </summary>
        /// <example>google.com</example>
        public string Domain { get; init; }

        /// <summary>
        /// The name of the record to update to the current public ip.
        /// </summary>
        public string Name { get; init; }

        ///// <summary>
        ///// Time at which the worker starts running.
        ///// </summary>
        //public TimeSpan TimeStart { get; init; }

        ///// <summary>
        ///// Time at which the worker stops running.
        ///// </summary>
        //public TimeSpan TimeEnd { get; init; }

        ///// <summary>
        ///// Time between runs in seconds.
        ///// </summary>
        //public double Interval { get; init; }

        /// <summary>
        /// The schedule to update the ip at.
        /// </summary>
        public IRunSchedule RunSchedule { get; init; }
    }
}
