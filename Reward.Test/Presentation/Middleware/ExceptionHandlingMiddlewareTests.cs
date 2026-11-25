using System.Text.Json;
using FluentAssertions;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Moq;
using Reward.Domain.Exceptions;
using Reward.Presentation.Middleware;
using Serilog.Core;

namespace Reward.Test.Presentation.Middleware;

public class ExceptionHandlingMiddlewareTests
{
    [Fact]
    public async Task InvokeAsync_DomainException_ReturnsConflictProblem()
    {
        // Act
        (HttpContext context, JsonElement problem) = await InvokeAsync(
            new ItemNotEquippableException("The hero level is too low to equip this item.")
        );

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        context.Response.ContentType.Should().StartWith("application/problem+json");
        problem.GetProperty("title").GetString().Should().Be("Conflict");
        problem
            .GetProperty("detail")
            .GetString()
            .Should()
            .Be("The hero level is too low to equip this item.");
    }

    [Fact]
    public async Task InvokeAsync_InventoryCapacityExceeded_ReturnsConflictWithItsCode()
    {
        // Act
        (HttpContext context, JsonElement problem) = await InvokeAsync(
            new InventoryCapacityExceededException("item", 40)
        );

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        problem
            .GetProperty("code")
            .GetString()
            .Should()
            .Be(ProblemErrorCodes.InventoryCapacityInsufficient);
    }

    [Fact]
    public async Task InvokeAsync_InvalidOperationException_ReturnsServerErrorInsteadOfConflict()
    {
        // Act: a configuration failure is not a business refusal the client can resolve.
        (HttpContext context, JsonElement problem) = await InvokeAsync(
            new InvalidOperationException("The reward source 'CHEST' is not configured.")
        );

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status500InternalServerError);
        problem.GetProperty("title").GetString().Should().Be("Internal server error");
    }

    [Fact]
    public async Task InvokeAsync_ServerErrorOutsideDevelopment_HidesTheExceptionMessage()
    {
        // Act
        (_, JsonElement problem) = await InvokeAsync(
            new InvalidOperationException("Connection string 'secret' is invalid."),
            isDevelopment: false
        );

        // Assert
        problem.ToString().Should().NotContain("secret");
    }

    [Fact]
    public async Task InvokeAsync_ConcurrentUpdate_ReturnsConflictProblem()
    {
        // Act
        (HttpContext context, JsonElement problem) = await InvokeAsync(
            new DbUpdateConcurrencyException("Concurrency token mismatch.")
        );

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status409Conflict);
        problem.GetProperty("title").GetString().Should().Be("Concurrent update");
    }

    [Fact]
    public async Task InvokeAsync_ValidationException_ReturnsUnprocessableEntityWithFieldErrors()
    {
        // Act
        (HttpContext context, JsonElement problem) = await InvokeAsync(
            new ValidationException([new ValidationFailure("HeroId", "Hero id is required.")])
        );

        // Assert
        context.Response.StatusCode.Should().Be(StatusCodes.Status422UnprocessableEntity);
        problem
            .GetProperty("errors")
            .GetProperty("heroId")[0]
            .GetString()
            .Should()
            .Be("Hero id is required.");
    }

    private static async Task<(HttpContext Context, JsonElement Problem)> InvokeAsync(
        Exception exception,
        bool isDevelopment = true
    )
    {
        // Arrange a request whose next delegate fails with the given exception.
        var environment = new Mock<IHostEnvironment>();
        environment
            .SetupGet(value => value.EnvironmentName)
            .Returns(isDevelopment ? Environments.Development : Environments.Production);
        var middleware = new ExceptionHandlingMiddleware(Logger.None, environment.Object);
        var context = new DefaultHttpContext();
        context.Request.Path = "/api/rewards/v1/test";
        context.Response.Body = new MemoryStream();

        await middleware.InvokeAsync(context, _ => throw exception);

        context.Response.Body.Position = 0;
        using JsonDocument document = await JsonDocument.ParseAsync(
            context.Response.Body,
            cancellationToken: TestContext.Current.CancellationToken
        );
        return (context, document.RootElement.Clone());
    }
}
