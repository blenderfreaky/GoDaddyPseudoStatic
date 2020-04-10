namespace GoDaddyPseudoStatic
{
    using System;

    public class WorkerOptions
    {
        /// <summary>
        /// The domain to update.
        /// </summary>
        /// <example>google.com</example>
        public string Domain { get; set; }

        /// <summary>
        /// The name of the record to update to the current public ip.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Time at which the worker starts running.
        /// </summary>
        public TimeSpan TimeStart { get; set; }

        /// <summary>
        /// Time at which the worker stops running.
        /// </summary>
        public TimeSpan TimeEnd { get; set; }

        /// <summary>
        /// Time between runs in seconds.
        /// </summary>
        public double Interval { get; set; }
    }
}
