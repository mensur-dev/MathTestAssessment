using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace MathTestAssessment.Web.Services;

public sealed class AuthService(ExamApiClient api, ProtectedSessionStorage storage)
    : AuthenticationStateProvider
{
    private const string StorageKey = "exam-session-v1";

    private static readonly ClaimsPrincipal Anonymous = new(new ClaimsIdentity());

    private string? token;
    private DateTime expires;

    public string? AccessToken =>
        DateTime.UtcNow < expires ? token : null;

    public bool IsAuthenticated => AccessToken is not null;

    public bool MustChangePassword { get; private set; }

    public override Task<AuthenticationState> GetAuthenticationStateAsync() =>
        Task.FromResult(
            new AuthenticationState(
                AccessToken is { } value
                    ? Principal(value)
                    : Anonymous));

    public async Task RestoreAsync()
    {
        try
        {
            var saved = await storage.GetAsync<LoginResponse>(StorageKey);

            if (saved.Success && saved.Value is { } session && Accept(session))
            {
                NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
                return;
            }
        }
        catch (Exception ex) when (
            ex is System.Security.Cryptography.CryptographicException
                or System.Text.Json.JsonException
                or ArgumentException
                or Microsoft.JSInterop.JSException)
        {
            // Invalid or obsolete protected data is never trusted.
        }

        await ClearSessionAsync();
    }

    public async Task<bool> LoginAsync(
        string email,
        string password,
        CancellationToken cancellationToken = default)
    {
        LoginResponse? response;

        try
        {
            response = await api.PostAsync<LoginRequest, LoginResponse>(
                "api/auth/login",
                new(email, password),
                cancellationToken: cancellationToken);
        }
        catch (HttpRequestException ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
        {
            return false;
        }

        if (response is null || !Accept(response))
        {
            return false;
        }

        try
        {
            await storage.SetAsync(StorageKey, response);
        }
        catch
        {
            token = null;
            expires = default;
            throw;
        }

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());

        return true;
    }

    public async Task ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        var response = await api.PostAsync<ChangePasswordRequest, LoginResponse>(
            "api/auth/change-password",
            new(currentPassword, newPassword),
            AccessToken,
            cancellationToken);

        if (response is null || !Accept(response))
        {
            throw new InvalidOperationException(
                "The API did not return a valid session.");
        }

        await storage.SetAsync(StorageKey, response);

        NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
    }

    public async Task LogoutAsync(
        CancellationToken cancellationToken = default)
    {
        var current = AccessToken;

        try
        {
            if (current is not null)
            {
                await api.PostAsync(
                    "api/auth/logout",
                    current,
                    cancellationToken);
            }
        }
        finally
        {
            await ClearSessionAsync();
        }
    }

    public async Task ClearSessionAsync()
    {
        token = null;
        expires = default;
        MustChangePassword = false;

        try
        {
            await storage.DeleteAsync(StorageKey);
        }
        catch (Microsoft.JSInterop.JSException)
        {
            /* Local state is cleared even if storage is unavailable. */
        }
        finally
        {
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }
    }

    private bool Accept(LoginResponse session)
    {
        if (string.IsNullOrWhiteSpace(session.Token))
        {
            return false;
        }

        try
        {
            var jwt = new JwtSecurityTokenHandler()
                .ReadJwtToken(session.Token);

            var expiry = session.ExpiresAtUtc < jwt.ValidTo
                ? session.ExpiresAtUtc
                : jwt.ValidTo;

            if (expiry <= DateTime.UtcNow)
            {
                return false;
            }

            token = session.Token;
            expires = expiry;

            MustChangePassword = string.Equals(
                jwt.Claims.FirstOrDefault(x => x.Type == "MustChangePassword")?.Value,
                "true",
                StringComparison.OrdinalIgnoreCase);

            return true;
        }
        catch (ArgumentException)
        {
            return false;
        }
    }

    private static ClaimsPrincipal Principal(string value) =>
        new(
            new ClaimsIdentity(
                new JwtSecurityTokenHandler().ReadJwtToken(value).Claims,
                "jwt",
                ClaimTypes.NameIdentifier,
                ClaimTypes.Role));

    public sealed record LoginRequest(
        string Email,
        string Password);

    public sealed record ChangePasswordRequest(
        string CurrentPassword,
        string NewPassword);

    public sealed record LoginResponse(
        string Token,
        DateTime ExpiresAtUtc,
        bool MustChangePassword = false);
}
