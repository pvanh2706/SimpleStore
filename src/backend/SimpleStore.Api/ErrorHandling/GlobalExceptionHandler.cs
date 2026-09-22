using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Errors;
using SimpleStore.Domain;

namespace SimpleStore.Api.ErrorHandling;

public sealed partial class GlobalExceptionHandler(
    ILogger<GlobalExceptionHandler> logger,
    IProblemDetailsService problemDetailsService) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        LogUnhandledException(
            logger,
            exception,
            httpContext.Request.Method,
            httpContext.Request.Path);

        var problem = MapProblem(exception);
        httpContext.Response.StatusCode = problem.Status!.Value;

        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
            Exception = exception
        });
    }

    private static ProblemDetails MapProblem(Exception exception)
    {
        var problem = exception switch
        {
            ApplicationValidationException validation => CreateProblem(
                StatusCodes.Status400BadRequest,
                validation.Code,
                validation.Message),
            DomainRuleException domain => CreateProblem(
                StatusCodes.Status400BadRequest,
                domain.Code,
                domain.Message),
            ApplicationConflictException conflict => CreateProblem(
                StatusCodes.Status409Conflict,
                conflict.Code,
                conflict.Message),
            UniqueConstraintException => CreateProblem(
                StatusCodes.Status409Conflict,
                "unique-constraint-conflict",
                "The requested value already exists."),
            ApplicationNotFoundException notFound => CreateProblem(
                StatusCodes.Status404NotFound,
                notFound.Code,
                notFound.Message),
            InsufficientStockException stock => CreateProblem(
                StatusCodes.Status409Conflict,
                "insufficient-stock",
                stock.Message),
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Type = "https://www.rfc-editor.org/rfc/rfc9110#section-15.6.1"
            }
        };

        if (exception is ApplicationValidationException validationException)
        {
            problem.Extensions["errors"] = validationException.Errors;
        }

        if (exception is InsufficientStockException stockException)
        {
            problem.Extensions["shortages"] = stockException.Shortages;
        }

        if (exception is ApplicationConflictException conflictException)
        {
            foreach (var extension in conflictException.Extensions)
            {
                problem.Extensions[extension.Key] = extension.Value;
            }
        }

        return problem;
    }

    private static ProblemDetails CreateProblem(int status, string code, string title)
    {
        var problem = new ProblemDetails
        {
            Status = status,
            Title = title,
            Type = $"https://simplestore/errors/{code}"
        };
        problem.Extensions["code"] = code;
        return problem;
    }

    [LoggerMessage(
        EventId = 1,
        Level = LogLevel.Error,
        Message = "Unhandled exception while processing {Method} {Path}")]
    private static partial void LogUnhandledException(
        ILogger logger,
        Exception exception,
        string method,
        PathString path);
}
