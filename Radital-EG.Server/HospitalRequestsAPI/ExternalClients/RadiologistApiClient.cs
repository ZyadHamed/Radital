using System.Net;
using System.Text.Json;

namespace HospitalRequestsAPI.ExternalClients
{
    public interface IRadiologistApiClient
    {
        Task<(byte[] Content, string FileName)> DownloadReportPdfAsync(Guid reportId, CancellationToken ct = default);
        Task NotifyEmergencyAsync(Guid requestId, Guid radiologistId, CancellationToken ct = default);
    }

    public class RadiologistApiClient : IRadiologistApiClient
    {
        private readonly HttpClient _httpClient;
        private readonly ILogger<RadiologistApiClient> _logger;

        public RadiologistApiClient(HttpClient httpClient, ILogger<RadiologistApiClient> logger)
        {
            _httpClient = httpClient;
            _logger = logger;
        }

        public async Task<(byte[] Content, string FileName)> DownloadReportPdfAsync(Guid reportId, CancellationToken ct = default)
        {
            using var response = await _httpClient.GetAsync($"api/Reports/{reportId}/pdf", ct);

            if (!response.IsSuccessStatusCode)
            {
                var errorMessage = await ExtractErrorMessageAsync(response, ct);

                _logger.LogWarning(
                    "RadiologistAPI returned {StatusCode} for report {ReportId}: {Message}",
                    (int)response.StatusCode, reportId, errorMessage);

                throw response.StatusCode switch
                {
                    HttpStatusCode.NotFound => new KeyNotFoundException(errorMessage),
                    HttpStatusCode.Forbidden => new UnauthorizedAccessException(errorMessage),
                    HttpStatusCode.Unauthorized => new UnauthorizedAccessException(errorMessage),
                    _ => new HttpRequestException(
                                                       $"RadiologistAPI call failed ({(int)response.StatusCode}): {errorMessage}")
                };
            }

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var fileName = response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                           ?? $"Report_{reportId}.pdf";

            return (bytes, fileName);
        }

        private static async Task<string> ExtractErrorMessageAsync(HttpResponseMessage response, CancellationToken ct)
        {
            string body;
            try
            {
                body = await response.Content.ReadAsStringAsync(ct);
            }
            catch
            {
                return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";
            }

            if (string.IsNullOrWhiteSpace(body))
                return $"HTTP {(int)response.StatusCode} {response.ReasonPhrase}";

            // Try to parse the standard `{ "message": "..." }` shape your APIs return.
            var contentType = response.Content.Headers.ContentType?.MediaType;
            if (contentType is "application/json" or "application/problem+json")
            {
                try
                {
                    using var doc = JsonDocument.Parse(body);
                    var root = doc.RootElement;

                    // Your controllers: { "message": "..." }
                    if (root.TryGetProperty("message", out var msg) && msg.ValueKind == JsonValueKind.String)
                        return msg.GetString() ?? body;

                    // ASP.NET Core ProblemDetails: { "title": "...", "detail": "..." }
                    if (root.TryGetProperty("detail", out var detail) && detail.ValueKind == JsonValueKind.String)
                        return detail.GetString() ?? body;

                    if (root.TryGetProperty("title", out var title) && title.ValueKind == JsonValueKind.String)
                        return title.GetString() ?? body;
                }
                catch (JsonException)
                {
                    // Fall through and return the raw body
                }
            }

            return body;
        }
        public async Task NotifyEmergencyAsync(Guid requestId, Guid radiologistId, CancellationToken ct = default)
        {
            var response = await _httpClient.PostAsync(
                $"api/workload/{requestId}/notify-emergency/{radiologistId}", null, ct);
            response.EnsureSuccessStatusCode();
        }
    }
}