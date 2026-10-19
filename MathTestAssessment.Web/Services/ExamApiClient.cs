using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using Microsoft.Extensions.Logging;

namespace MathTestAssessment.Web.Services;
public sealed class ExamApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ExamApiClient> _logger;

    public ExamApiClient(
        HttpClient httpClient,
        ILogger<ExamApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    // GET - me ose pa JWT
    public async Task<T?> GetAsync<T>(
        string endpoint,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Get,
            endpoint,
            accessToken);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent) return default;
        return await response.Content.ReadFromJsonAsync<T>(
            cancellationToken: cancellationToken);
    }

    // POST - me JSON request dhe JSON response
    public async Task<TResponse?> PostAsync<TRequest, TResponse>(
        string endpoint,
        TRequest data,
        string? accessToken = null,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            endpoint,
            accessToken);

        request.Content = JsonContent.Create(data);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        await EnsureSuccessAsync(
            response,
            cancellationToken);

        if (response.StatusCode == System.Net.HttpStatusCode.NoContent)
        {
            return default;
        }

        return await response.Content.ReadFromJsonAsync<TResponse>(
            cancellationToken: cancellationToken);
    }

    // POST - pa request body, p.sh. Logout
    public async Task PostAsync(
        string endpoint,
        string accessToken,
        CancellationToken cancellationToken = default)
    {
        using var request = CreateRequest(
            HttpMethod.Post,
            endpoint,
            accessToken);

        using var response = await _httpClient.SendAsync(
            request,
            cancellationToken);

        await EnsureSuccessAsync(
            response,
            cancellationToken);
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string endpoint,
        string? accessToken,
        HttpContent? content)
    {
        var request = CreateRequest(method, endpoint, accessToken);
        request.Content = content;
        return request;
    }

    public async Task SendAsync(HttpMethod method, string endpoint, object? data, string? token)
    {
        using var request = CreateRequest(method, endpoint, token,
            data is null ? null : JsonContent.Create(data));
        using var response = await _httpClient.SendAsync(request);
        await EnsureSuccessAsync(response, CancellationToken.None);
    }

    public async Task<Models.CreatedResource?> UploadAsync(Stream stream, string filename, string? token)
    {
        using var content = new MultipartFormDataContent();
        content.Add(new StreamContent(stream), "file", Path.GetFileName(filename));
        using var request = CreateRequest(HttpMethod.Post, "api/import-jobs", token, content);
        using var response = await _httpClient.SendAsync(request);
        await EnsureSuccessAsync(response, CancellationToken.None);
        return await response.Content.ReadFromJsonAsync<Models.CreatedResource>();
    }

    private static HttpRequestMessage CreateRequest(
        HttpMethod method,
        string endpoint,
        string? accessToken)
    {
        var request = new HttpRequestMessage(
            method,
            endpoint);

        if (!string.IsNullOrWhiteSpace(accessToken))
        {
            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    "Bearer",
                    accessToken);
        }

        return request;
    }

    private async Task EnsureSuccessAsync(
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        if (response.IsSuccessStatusCode)
        {
            return;
        }

        if ((int)response.StatusCode is >= 300 and < 400)
        {
            throw new HttpRequestException(
                "The API redirected the request. Configure Api:BaseUrl with the final HTTPS address.",
                null, System.Net.HttpStatusCode.Conflict);
        }

        _logger.LogWarning(
            "API request failed. StatusCode: {StatusCode}",
            (int)response.StatusCode);

        var responseBody = await response.Content.ReadAsStringAsync(
            cancellationToken);
        var errorMessage = ExtractErrorMessage(responseBody)
            ?? $"The request failed with status code {(int)response.StatusCode}.";

        throw new HttpRequestException(
            errorMessage,
            null,
            response.StatusCode);
    }

    private static string? ExtractErrorMessage(string responseBody)
    {
        if (string.IsNullOrWhiteSpace(responseBody))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(responseBody);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.String) return root.GetString();
            if (root.ValueKind != JsonValueKind.Object) return null;

            var errors = root.EnumerateObject().FirstOrDefault(property =>
                property.Name.Equals("errors", StringComparison.OrdinalIgnoreCase)).Value;
            var validationMessages = ReadMessages(errors).Distinct().ToArray();
            if (validationMessages.Length > 0) return string.Join(" ", validationMessages);

            foreach (var propertyName in new[] { "message", "error", "detail", "title" })
            {
                var property = root.EnumerateObject().FirstOrDefault(candidate =>
                    candidate.Name.Equals(propertyName, StringComparison.OrdinalIgnoreCase)).Value;
                if (property.ValueKind == JsonValueKind.String
                    && !string.IsNullOrWhiteSpace(property.GetString()))
                {
                    return property.GetString();
                }
            }

            return null;
        }
        catch (JsonException)
        {
            // Some endpoints may return plain text instead of ProblemDetails.
        }

        return responseBody.Trim().Trim('"');
    }

    private static IEnumerable<string> ReadMessages(JsonElement value) => value.ValueKind switch
    {
        JsonValueKind.String => string.IsNullOrWhiteSpace(value.GetString()) ? [] : [value.GetString()!],
        JsonValueKind.Array => value.EnumerateArray().SelectMany(ReadMessages),
        JsonValueKind.Object => value.EnumerateObject().SelectMany(property => ReadMessages(property.Value)),
        _ => []
    };
}
