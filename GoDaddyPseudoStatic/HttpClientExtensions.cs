namespace GoDaddyPseudoStatic
{
    using System.Net.Http;
    using System.Text.Json;
    using System.Threading;
    using System.Threading.Tasks;

    public static class HttpContentExtensions
    {
        public static async ValueTask<T> DeserializeAsync<T>(this HttpContent content, JsonSerializerOptions options = null, CancellationToken cancellationToken = default)
        {
            var stream = await content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);

            return await JsonSerializer.DeserializeAsync<T>(stream, options, cancellationToken).ConfigureAwait(false);
        }
    }
}
