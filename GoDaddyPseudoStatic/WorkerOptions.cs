namespace GoDaddyPseudoStatic
{
    using GoDaddyPseudoStatic.RunSchedules;

    public record WorkerOptions
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
        /// The API endpoint to call.
        /// String format parameters:
        /// {0} = Domain
        /// {1} = Name
        /// </summary>
        /// <example>"https://api.godaddy.com/v1/domains/{0}/records/A/{1}" for GoDaddy</example>
        /// <example>"https://api.gandi.net/v5/livedns/domains/{0}/records" for Gandi</example>
        public string Endpoint { get; init; }

        /// <summary>
        /// The schedule to update the ip at.
        /// </summary>
        public IRunSchedule RunSchedule { get; init; }
    }

    public record WorkerSecrets
    {
        /// <summary>
        /// The HTTPS authorization header.
        /// </summary>
        /// <example>"[Key]:[Secret]" for GoDaddy</example>
        /// <example>"Apikey [Api-Key]"</example>
        public string AuthorizationHeader { get; init; }
    }
}