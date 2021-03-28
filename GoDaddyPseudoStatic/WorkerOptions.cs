namespace GoDaddyPseudoStatic
{
    using GoDaddyPseudoStatic.RunSchedules;

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

        /// <summary>
        /// The schedule to update the ip at.
        /// </summary>
        public IRunSchedule RunSchedule { get; init; }
    }

    public class WorkerSecrets
    {
        public string Key { get; init; }
        public string Secret { get; init; }
    }
}