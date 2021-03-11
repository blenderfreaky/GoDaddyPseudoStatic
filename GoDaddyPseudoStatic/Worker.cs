namespace GoDaddyPseudoStatic
{
    using System;
    using System.Net;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;
    using Microsoft.Extensions.Hosting;
    using Microsoft.Extensions.Logging;

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

                var timeOfDay = DateTime.Now.TimeOfDay;

                TimeSpan interval;

                bool afterStart = timeOfDay >= _options.TimeStart;
                bool beforeEnd = timeOfDay < _options.TimeEnd;

                if (afterStart && beforeEnd)
                {
                    interval = TimeSpan.FromSeconds(_options.Interval);
                    _logger.LogInformation("Inside of operational times, repeating until {end}", _options.TimeEnd);
                }
                else
                {
                    var diff = _options.TimeStart - timeOfDay;
                    interval = !afterStart ? diff : TimeSpan.FromDays(1) + diff;
                    _logger.LogInformation("Outside of operational times, waiting until {start}", _options.TimeStart);
                }

                _logger.LogDebug("Waiting for {interval}", interval);
                await Task.Delay(interval, cancellationToken).ConfigureAwait(false);
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
            }
            else
            {
                _logger.LogWarning("Failed updating IP, got response:\n{response}", domainUpdateResponse);
            }
        }
    }
}
