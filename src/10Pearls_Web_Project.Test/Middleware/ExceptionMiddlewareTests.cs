using System;
using System.IO;
using System.Net;
using System.Text.Json;
using System.Threading.Tasks;
using _10Pearls_Web_Project.Server.Middleware;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace _10Pearls_Web_Project.Test.Middleware
{
    /// <summary>
    /// Unit tests for verifying the ExceptionMiddleware global error handling.
    /// </summary>
    public class ExceptionMiddlewareTests
    {
        #region InvokeAsync Tests

        /// <summary>
        /// Verifies that when an unhandled exception occurs in the request pipeline:
        /// 1. The exception is caught and logged as an error.
        /// 2. The response content type is set to application/json.
        /// 3. The response status code is set to 500 (Internal Server Error).
        /// 4. The JSON response contains the appropriate message and exception details conditional on compile settings.
        /// </summary>
        [Fact]
        public async Task InvokeAsync_ShouldCatchExceptionAndReturn500_WhenExceptionIsThrown()
        {
            // Arrange
            var exceptionMessage = "Database connection failed!";
            var expectedException = new Exception(exceptionMessage);
            
            RequestDelegate next = (ctx) => throw expectedException;
            var loggerMock = new Mock<ILogger<ExceptionMiddleware>>();
            var middleware = new ExceptionMiddleware(next, loggerMock.Object);

            var context = new DefaultHttpContext();
            var responseBodyStream = new MemoryStream();
            context.Response.Body = responseBodyStream;

            // Act
            await middleware.InvokeAsync(context);

            // Assert
            // 1. Verify exception was logged as error
            loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => true),
                    expectedException,
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);

            // 2. Verify response headers
            context.Response.ContentType.Should().Be("application/json");
            context.Response.StatusCode.Should().Be((int)HttpStatusCode.InternalServerError);

            // 3. Verify response body JSON
            responseBodyStream.Seek(0, SeekOrigin.Begin);
            using var reader = new StreamReader(responseBodyStream);
            var jsonResponseString = await reader.ReadToEndAsync();
            jsonResponseString.Should().NotBeNullOrWhiteSpace();

            using var jsonDocument = JsonDocument.Parse(jsonResponseString);
            var root = jsonDocument.RootElement;

            root.TryGetProperty("message", out var messageProp).Should().BeTrue();
            messageProp.GetString().Should().Be("An unexpected error occurred. Please try again later.");

#if DEBUG
            // In debug mode, the 'detail' property should be present and equal to the exception message
            root.TryGetProperty("detail", out var detailProp).Should().BeTrue();
            detailProp.GetString().Should().Be(exceptionMessage);
#else
            // In release mode, the 'detail' property should not be included
            root.TryGetProperty("detail", out _).Should().BeFalse();
#endif
        }

        #endregion
    }
}
