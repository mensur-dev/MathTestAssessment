using System.Net;
using System.Text;
using MathTestAssessment.Web.Services;
using Microsoft.Extensions.Logging.Abstractions;

namespace MathTestAssessment.UnitTests.Web;

public sealed class ExamApiClientTests
{
    [Theory]
    [InlineData(
        """{"error":"Student ID already exists."}""",
        "Student ID already exists.")]
    [InlineData(
        """{"errors":{"Password":["A digit is required."]}}""",
        "A digit is required.")]
    [InlineData(
        """{"unexpected":true}""",
        "The request failed with status code 400.")]
    public async Task GetAsync_WhenRequestFails_ReturnsReadableError(
        string responseBody,
        string expectedMessage)
    {
        // Arrange
        using var http = new HttpClient(new ErrorResponseHandler(responseBody))
        {
            BaseAddress = new Uri("https://unit-test.invalid/")
        };

        var client = new ExamApiClient(
            http,
            NullLogger<ExamApiClient>.Instance);

        // Act
        var exception = await Assert.ThrowsAsync<HttpRequestException>(
            () => client.GetAsync<object>("example"));

        // Assert
        Assert.Equal(expectedMessage, exception.Message);
        Assert.Equal(HttpStatusCode.BadRequest, exception.StatusCode);
    }

    private sealed class ErrorResponseHandler(string body) : HttpMessageHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            var response = new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent(
                    body,
                    Encoding.UTF8,
                    "application/json")
            };

            return Task.FromResult(response);
        }
    }
}