using Microsoft.AspNetCore.Components;
using Radzen;

namespace MathTestAssessment.Web.Services;

public abstract class ApiPage : ComponentBase
{
    [Inject]
    protected ExamApiClient Api { get; set; } = default!;

    [Inject]
    protected AuthService Auth { get; set; } = default!;

    [Inject]
    protected NotificationService Notifications { get; set; } = default!;

    [Inject]
    protected ILogger<ApiPage> Logger { get; set; } = default!;

    [Inject]
    protected NavigationManager Navigation { get; set; } = default!;

    protected bool Busy { get; private set; }

    protected string? Error { get; private set; }

    protected async Task Run(Func<Task> action)
    {
        if (Busy)
        {
            return;
        }

        Busy = true;
        Error = null;

        try
        {
            await action();
        }
        catch (HttpRequestException ex)
        {
            Logger.LogWarning(ex, "API operation failed");

            if (ex.StatusCode == System.Net.HttpStatusCode.Unauthorized)
            {
                await Auth.ClearSessionAsync();

                Navigation.NavigateTo(
                    "/login?sessionExpired=true",
                    replace: true);

                return;
            }

            Error = ex.StatusCode switch
            {
                System.Net.HttpStatusCode.Unauthorized =>
                    "Session expired. Please sign in again.",

                System.Net.HttpStatusCode.Forbidden =>
                    "Your role cannot perform this operation.",

                System.Net.HttpStatusCode.NotFound =>
                    "The requested record was not found.",

                System.Net.HttpStatusCode.BadRequest or System.Net.HttpStatusCode.Conflict =>
                    ex.Message,

                _ =>
                    "The API is unavailable. Check that MathTestAssessment.Api is running and retry."
            };
        }
        catch (TaskCanceledException)
        {
            Error = "The request timed out. Please retry.";
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "Page operation failed");

            Error = "The operation could not be completed. Please retry.";
        }
        finally
        {
            Busy = false;
        }
    }

    protected void Saved(string message = "Saved successfully.") =>
        Notifications.Notify(
            NotificationSeverity.Success,
            "Success",
            message);
}
