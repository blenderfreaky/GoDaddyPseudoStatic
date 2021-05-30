namespace GoDaddyPseudoStatic
{
    using GoDaddyPseudoStatic.RunSchedules;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.Globalization;
    using System.IO;
    using System.Linq;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public class Worker : BackgroundService
    {
        private readonly ILogger<Worker> _logger;

        private readonly WorkerOptions _options;
        private readonly WorkerSecrets _secrets;

        private readonly HttpClient _ipInfoClient;
        private readonly HttpClient _dnsClient;

        private readonly Uri _ipInfoUri;
        private readonly Uri _dnsEndpoint;

        public Worker(ILogger<Worker> logger, WorkerOptions options, WorkerSecrets secrets)
        {
            _logger = logger;

            _options = options;
            _secrets = secrets;

            _ipInfoUri = new Uri("https://ipinfo.io/json");
            _ipInfoClient = new HttpClient();
            _dnsClient = new HttpClient();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            _dnsEndpoint = new Uri(string.Format(_options.Endpoint, _options.Domain, _options.Name));

            _dnsClient.DefaultRequestHeaders.TryAddWithoutValidation("Authorization", _secrets.AuthorizationHeader);
        }

        public override void Dispose()
        {
            _ipInfoClient.Dispose();
            _dnsClient.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await AttemptUpdate().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "Error while attempting IP update");
                }

                var next = _options.RunSchedule.GetNextExecutions(DateTime.Now).Take(4).ToList();

                _logger.LogDebug("Ran update. Next updates at {nextUpdateTimes}", next);

                await Task.Delay(next[0].Subtract(DateTime.Now), cancellationToken).ConfigureAwait(false);
            }
        }

        public record DomainInfo(string Data, string Name, int Ttl, string Type);
        public record IpInfo(string IP);

        private async ValueTask AttemptUpdate()
        {
            JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

            var ipInfoResponse = await _ipInfoClient.GetAsync(_ipInfoUri).ConfigureAwait(false);
            if (!ipInfoResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error calling IpInfo API:\n{response}", ipInfoResponse);
                return;
            }
            var ipInfo = await ipInfoResponse.Content.DeserializeAsync<IpInfo>(jsonOptions).ConfigureAwait(false);
            var ip = ipInfo.IP;

            var domainInfoRespone = await _dnsClient.GetAsync(_dnsEndpoint).ConfigureAwait(false);
            if (!domainInfoRespone.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error calling GoDaddy API:\n{response}", domainInfoRespone);
                return;
            }
            var domainInfo = await domainInfoRespone.Content.DeserializeAsync<DomainInfo[]>(jsonOptions).ConfigureAwait(false);
            var domainIp = domainInfo[0].Data;

            if (string.Equals(ip, domainIp, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Up-To-Date, IPv4: {publicIp}", ip);
                return;
            }

            StringContent domainUpdateRequest = new("[{\"data\":\"" + ip + "\"}]", Encoding.UTF8, "application/json");
            var domainUpdateResponse = await _dnsClient.PutAsync(_dnsEndpoint, domainUpdateRequest).ConfigureAwait(false);

            if (domainUpdateResponse.IsSuccessStatusCode)
            {
                _logger.LogInformation("Updated IP sucessfully from {oldIp} to {newIp}", domainIp, ip);

                if (!File.Exists("ipUpdates.csv")) await File.WriteAllTextAsync("ipUpdates.csv", "Time, UTC Time, Old IP, New IP").ConfigureAwait(false);
                await File.AppendAllTextAsync("ipUpdates.csv", $"\n{DateTime.Now}, {DateTime.UtcNow}, {domainIp}, {ip}").ConfigureAwait(false);
            }
            else
            {
                _logger.LogWarning("Failed updating IP, got response:\n{response}", domainUpdateResponse);
            }
        }
    }
}