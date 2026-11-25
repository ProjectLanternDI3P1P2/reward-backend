using FluentValidation;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Reward.Domain.Exceptions;
using ILogger = Serilog.ILogger;

namespace Reward.Presentation.Middleware;

public sealed class ExceptionHandlingMiddleware(ILogger logger, IHostEnvironment environment)
    : IMiddleware
{
    public async Task InvokeAsync(HttpContext context, RequestDelegate next)
    {
        try
        {
            await next(context);
        }
        catch (KeyNotFoundException exception)
        {
            logger.Warning(exception, "Resource not found");
            await WriteProblemAsync(
                context,
                new ProblemDetails { Title = "Resource not found", Detail = exception.Message },
                StatusCodes.Status404NotFound
            );
        }
        catch (LootTableNotFoundException exception)
        {
            logger.Warning(exception, "Chest loot table not found");
            await WriteProblemAsync(
                context,
                new ProblemDetails { Title = "Resource not found", Detail = exception.Message },
                StatusCodes.Status404NotFound
            );
        }
        catch (ValidationException exception)
        {
            logger.Warning(exception, "Validation error occurred");
            await WriteProblemAsync(
                context,
                new ValidationProblemDetails(ToErrors(exception))
                {
                    Title = "Validation error",
                    Detail = "One or more validation errors occurred.",
                },
                StatusCodes.Status422UnprocessableEntity
            );
        }
        // Must stay before DomainException: this refusal carries its own client code.
        catch (InventoryCapacityExceededException exception)
        {
            logger.Warning(exception, "Inventory capacity refused the operation");
            // Every command runs in one transaction, so a capacity refusal never leaves a
            // partial transfer behind: the client only has to free space and retry.
            var problemDetails = new ProblemDetails
            {
                Title = "Insufficient inventory capacity",
                Detail =
                    $"{exception.Message} Free up inventory space and try again. No items were transferred.",
            };
            problemDetails.Extensions["code"] = ProblemErrorCodes.InventoryCapacityInsufficient;
            await WriteProblemAsync(context, problemDetails, StatusCodes.Status409Conflict);
        }
        catch (DomainException exception)
        {
            logger.Warning(exception, "Business rule refused the operation");
            await WriteProblemAsync(
                context,
                new ProblemDetails { Title = "Conflict", Detail = exception.Message },
                StatusCodes.Status409Conflict
            );
        }
        // A server-side loot table that cannot produce loot: kept as a conflict, as the
        // chest generation clients already handle it.
        catch (InvalidLootTableException exception)
        {
            logger.Warning(exception, "Chest loot table is invalid");
            await WriteProblemAsync(
                context,
                new ProblemDetails { Title = "Conflict", Detail = exception.Message },
                StatusCodes.Status409Conflict
            );
        }
        catch (DbUpdateConcurrencyException exception)
        {
            logger.Warning(exception, "Concurrent update detected");
            await WriteProblemAsync(
                context,
                new ProblemDetails
                {
                    Title = "Concurrent update",
                    Detail =
                        "The resource was modified by another request. Reload it and try again.",
                },
                StatusCodes.Status409Conflict
            );
        }
        catch (Exception exception)
        {
            logger.Error(exception, "Unhandled exception occurred");
            await WriteProblemAsync(
                context,
                new ProblemDetails
                {
                    Title = "Internal server error",
                    Detail = environment.IsDevelopment() ? exception.Message : null,
                },
                StatusCodes.Status500InternalServerError
            );
        }
    }

    /// <summary>
    /// Writes an RFC 9457 problem. The content type must be passed to WriteAsJsonAsync:
    /// setting Response.ContentType beforehand is overwritten with application/json.
    /// </summary>
    private static Task WriteProblemAsync<TProblem>(
        HttpContext context,
        TProblem problemDetails,
        int statusCode
    )
        where TProblem : ProblemDetails
    {
        problemDetails.Type = $"https://httpstatuses.com/{statusCode}";
        problemDetails.Status = statusCode;
        problemDetails.Instance = context.Request.Path;
        context.Response.StatusCode = statusCode;

        return context.Response.WriteAsJsonAsync(
            problemDetails,
            options: null,
            contentType: "application/problem+json",
            context.RequestAborted
        );
    }

    private static Dictionary<string, string[]> ToErrors(ValidationException exception) =>
        exception
            .Errors.GroupBy(error => error.PropertyName)
            .ToDictionary(
                group => ToCamelCase(group.Key),
                group => group.Select(error => error.ErrorMessage).ToArray()
            );

    private static string ToCamelCase(string propertyName)
    {
        if (string.IsNullOrEmpty(propertyName) || char.IsLower(propertyName[0]))
        {
            return propertyName;
        }

        return char.ToLowerInvariant(propertyName[0]) + propertyName[1..];
    }
}
