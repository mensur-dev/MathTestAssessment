using MathTestAssessment.Web.Services;
using MathTestAssessment.Web.Components;
using Microsoft.AspNetCore.Components.Authorization;
using Radzen;

var builder = WebApplication.CreateBuilder(args);

// Blazor
builder.Services
    .AddRazorComponents()
    .AddInteractiveServerComponents();

// Radzen
builder.Services.AddRadzenComponents();

// API client
builder.Services
    .AddHttpClient<ExamApiClient>(client =>
    {
        var baseUrl = builder.Configuration["Api:BaseUrl"];

        if (string.IsNullOrWhiteSpace(baseUrl))
        {
            throw new InvalidOperationException(
                "API BaseUrl is not configured.");
        }

        client.BaseAddress = new Uri(baseUrl);
        client.Timeout = TimeSpan.FromSeconds(30);
    })
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
    {
        // Prevent redirects from dropping the bearer token.
        AllowAutoRedirect = false
    });

// Authentication and authorization
builder.Services.AddAuthorizationCore();

builder.Services.AddScoped<AuthService>();

builder.Services.AddScoped<
    Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage.ProtectedSessionStorage>();

builder.Services.AddScoped<AuthenticationStateProvider>(
    sp => sp.GetRequiredService<AuthService>());

var app = builder.Build();

// Error handling
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

// Middleware
app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

// Static assets
app.MapStaticAssets();

// Blazor routing
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    // Blazor handles UI authentication; the API enforces JWT authorization.
    .AllowAnonymous();

app.Run();
