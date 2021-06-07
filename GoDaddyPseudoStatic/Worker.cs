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
    using System.Text.Json.Serialization;
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
            _ipInfoClient = new HttpClient() { BaseAddress = _ipInfoUri };

            ServicePointManager.SecurityProtocol = SecurityProtocolType.Tls12 | SecurityProtocolType.Tls11 | SecurityProtocolType.Tls;

            _dnsEndpoint = new Uri(_options.Provider switch
            {
                WorkerOptions.ProviderTypes.Gandi => $"https://api.gandi.net/v5/livedns/domains/{_options.Domain}/records/{_options.Name}/A",
                WorkerOptions.ProviderTypes.GoDaddy => $"https://api.godaddy.com/v1/domains/{_options.Domain}/records/A/{_options.Name}",
                _ => throw new InvalidOperationException()
            });

            _dnsClient = new HttpClient() { BaseAddress = _dnsEndpoint };
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

        public record GoDaddyDomainInfo(string Data, string Name, int Ttl, string Type);
        public record GandiDomainInfos(params GandiDomainInfo[] Items);
        public record GandiDomainInfo(string Rrset_name, string Rrset_type, string[] Rrset_values, string Rrset_href = null, int? Rrset_ttl = null);

        public record IpInfo(string IP);

        private async ValueTask AttemptUpdate()
        {
            JsonSerializerOptions jsonOptions = new() { PropertyNameCaseInsensitive = true };

            var ipInfoResponse = await _ipInfoClient.GetAsync("").ConfigureAwait(false);
            if (!ipInfoResponse.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error calling IpInfo API:\n{response}", ipInfoResponse);
                return;
            }
            var ipInfo = await ipInfoResponse.Content.DeserializeAsync<IpInfo>(jsonOptions).ConfigureAwait(false);
            var ip = ipInfo.IP;

            var domainInfoRespone = await _dnsClient.GetAsync(""
            //_options.Provider switch
            //{
            //    WorkerOptions.ProviderTypes.Gandi => _dnsEndpoint.ToString() + "?rrset_type=\"A\"",
            //    WorkerOptions.ProviderTypes.GoDaddy => "",
            //    _ => throw new InvalidOperationException()
            //}
            ).ConfigureAwait(false);
            if (!domainInfoRespone.IsSuccessStatusCode)
            {
                _logger.LogWarning("Error calling API:\n{response}", domainInfoRespone);
                return;
            }
            var domainIp = _options.Provider switch
            {
                WorkerOptions.ProviderTypes.Gandi => (await domainInfoRespone.Content.DeserializeAsync<GandiDomainInfo>(jsonOptions).ConfigureAwait(false)).Rrset_values[0],
                WorkerOptions.ProviderTypes.GoDaddy => (await domainInfoRespone.Content.DeserializeAsync<GoDaddyDomainInfo[]>(jsonOptions).ConfigureAwait(false))[0].Data,
                _ => throw new InvalidOperationException()
            };

            if (string.Equals(ip, domainIp, StringComparison.OrdinalIgnoreCase))
            {
                _logger.LogDebug("Up-To-Date, IPv4: {publicIp}", ip);
                return;
            }

            StringContent domainUpdateRequest = _options.Provider switch
            {
                //JsonSerializer.Serialize(new[] { new GandiDomainInfo(_options.Name, "A", new[] { ip }) })
                WorkerOptions.ProviderTypes.Gandi => new($"[{{\"rrset_name\":\"{_options.Name}\",\"rrset_type\":\"A\",\"rrset_values\":[\"{ip}\"]}}]", Encoding.UTF8, "application/json"),
                WorkerOptions.ProviderTypes.GoDaddy => new("[{\"data\":\"" + ip + "\"}]", Encoding.UTF8, "application/json"),
                _ => throw new InvalidOperationException()
            };
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