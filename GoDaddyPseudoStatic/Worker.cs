namespace GoDaddyPseudoStatic
{
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;
    using System;
    using System.IO;
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
        private readonly HttpClient _goDaddyClient;

        private readonly Uri _ipInfoUri;
        private Uri _goDaddyUri;

        public Worker(ILogger<Worker> logger, WorkerOptions options, WorkerSecrets secrets)
        {
            _logger = logger;

            _options = options;
            _secrets = secrets;

            _ipInfoUri = new Uri("https://ipinfo.io/json");
            _ipInfoClient = new HttpClient();
            _goDaddyClient = new HttpClient();

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            InitFromConfig();
        }

        private void InitFromConfig()
        {
            _goDaddyUri = new Uri($"https://api.godaddy.com/v1/domains/{_options.Domain}/records/A/{_options.Name}");

            _goDaddyClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("sso-key", $"{_secrets.Key}:{_secrets.Secret}");
        }

        public override void Dispose()
        {
            _ipInfoClient.Dispose();
            _goDaddyClient.Dispose();
            base.Dispose();
            GC.SuppressFinalize(this);
        }

        protected override async Task ExecuteAsync(CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    InitFromConfig();

                    await AttemptUpdate().ConfigureAwait(false);
                }
                catch (Exception e)
                {
                    _logger.LogError(e.Message);
                }

                var next = _options.RunSchedule.GetNextExecution(DateTime.Now);

                _logger.LogInformation("Ran update. Next update at {nextUpdateTime}", next);

                await Task.Delay(next.Subtract(DateTime.Now), cancellationToken).ConfigureAwait(false);
            }
        }

        private async ValueTask AttemptUpdate()
        {
            var jsonOptions = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var ipInfoResponse = await _ipInfoClient.GetAsync(_ipInfoUri).ConfigureAwait(false);
            if (!ipInfoResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error calling IpInfo API:\n{response}", ipInfoResponse);
                return;
            }
            var ipInfo = await ipInfoResponse.Content.DeserializeAsync<IpInfo>(jsonOptions).ConfigureAwait(false);
            var ip = ipInfo.IP;

            var domainInfoRespone = await _goDaddyClient.GetAsync(_goDaddyUri).ConfigureAwait(false);
            if (!domainInfoRespone.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error calling GoDaddy API:\n{response}", domainInfoRespone);
                return;
            }
            var domainInfo = await domainInfoRespone.Content.DeserializeAsync<DomainInfo[]>(jsonOptions).ConfigureAwait(false);
            var domainIp = domainInfo[0].Data;

            if (string.Equals(ip, domainIp, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogInformation("Up-To-Date, IPv4: {publicIp}", ip);
                return;
            }

            var domainUpdateRequest = new StringContent("[{\"data\":\"" + ip + "\"}]", Encoding.UTF8, "application/json");
            var domainUpdateResponse = await _goDaddyClient.PutAsync(_goDaddyUri, domainUpdateRequest).ConfigureAwait(false);

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