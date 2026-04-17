using System.Net.Http.Headers;
using System.Text;
using Microsoft.Extensions.Options;

namespace SageX3WebApi.SageX3SoapClient;

public interface ISageX3Client
{
    Task<SoapResponseParser> RunAsync(string publicName, string inputXml, CancellationToken ct = default);
    Task<SoapResponseParser> SaveAsync(string publicName, string objectXml, CancellationToken ct = default);
    Task<SoapResponseParser> QueryAsync(string publicName, string objectKeysXml, int listSize = 100, CancellationToken ct = default);
}

/// <summary>
/// Typed HttpClient that talks to Sage X3's CAdxWebServiceXmlCC SOAP endpoint.
/// Registered via AddHttpClient&lt;ISageX3Client, SageX3Client&gt;() with Polly retry.
/// </summary>
public class SageX3Client : ISageX3Client
{
    private readonly HttpClient _http;
    private readonly SageX3Options _options;
    private readonly ILogger<SageX3Client> _logger;

    public SageX3Client(HttpClient http, IOptions<SageX3Options> options, ILogger<SageX3Client> logger)
    {
        _http = http;
        _options = options.Value;
        _logger = logger;

        ConfigureHttpClient();
    }

    private void ConfigureHttpClient()
    {
        if (string.IsNullOrWhiteSpace(_options.EndpointUrl))
            throw new InvalidOperationException("SageX3:EndpointUrl is not configured.");

        if (_http.BaseAddress is null)
            _http.BaseAddress = new Uri(_options.EndpointUrl);

        _http.Timeout = TimeSpan.FromSeconds(_options.TimeoutSeconds);

        // HTTP Basic auth (Sage also accepts credentials inside callContext; we send both).
        if (!string.IsNullOrEmpty(_options.Username))
        {
            var token = Convert.ToBase64String(
                Encoding.UTF8.GetBytes($"{_options.Username}:{_options.Password}"));
            _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", token);
        }
    }

    public Task<SoapResponseParser> RunAsync(string publicName, string inputXml, CancellationToken ct = default)
    {
        var envelope = SoapEnvelopeBuilder.BuildRunEnvelope(_options, publicName, inputXml);
        return PostAsync(envelope, "CAdxWebServiceXmlCC#run", ct);
    }

    public Task<SoapResponseParser> SaveAsync(string publicName, string objectXml, CancellationToken ct = default)
    {
        var envelope = SoapEnvelopeBuilder.BuildSaveEnvelope(_options, publicName, objectXml);
        return PostAsync(envelope, "CAdxWebServiceXmlCC#save", ct);
    }

    public Task<SoapResponseParser> QueryAsync(string publicName, string objectKeysXml, int listSize = 100, CancellationToken ct = default)
    {
        var envelope = SoapEnvelopeBuilder.BuildQueryEnvelope(_options, publicName, objectKeysXml, listSize);
        return PostAsync(envelope, "CAdxWebServiceXmlCC#query", ct);
    }

    private async Task<SoapResponseParser> PostAsync(string envelope, string soapAction, CancellationToken ct)
    {
        using var content = new StringContent(envelope, Encoding.UTF8, "text/xml");
        // Some Sage X3 deployments are strict about charset; set explicitly.
        content.Headers.ContentType = new MediaTypeHeaderValue("text/xml") { CharSet = "utf-8" };

        using var request = new HttpRequestMessage(HttpMethod.Post, _http.BaseAddress)
        {
            Content = content
        };
        request.Headers.TryAddWithoutValidation("SOAPAction", $"\"{soapAction}\"");

        _logger.LogDebug("POST {Url} action={Action}", _http.BaseAddress, soapAction);

        string body;
        try
        {
            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseContentRead, ct)
                                            .ConfigureAwait(false);
            body = await response.Content.ReadAsStringAsync(ct).ConfigureAwait(false);

            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("Sage X3 returned HTTP {Status} for action={Action}",
                    (int)response.StatusCode, soapAction);
                // Sage returns SOAP faults with HTTP 500 + a valid fault body; still parse it.
            }
        }
        catch (TaskCanceledException ex) when (!ct.IsCancellationRequested)
        {
            _logger.LogError(ex, "Sage X3 call timed out after {Seconds}s", _options.TimeoutSeconds);
            throw new TimeoutException($"Sage X3 call timed out after {_options.TimeoutSeconds}s.", ex);
        }

        return SoapResponseParser.Parse(body);
    }
}
