namespace GoDaddyPseudoStatic
{
    using GoDaddyPseudoStatic.RunSchedules;
    using System.Text.Json.Serialization;

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
        /// The provider to use.
        /// Supported providers are: GoDaddy, Gandi
        /// </summary>
        public ProviderTypes Provider { get; init; }

        [JsonConverter(typeof(JsonStringEnumConverter))]
        public enum ProviderTypes
        {
            Gandi,
            GoDaddy
        }

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