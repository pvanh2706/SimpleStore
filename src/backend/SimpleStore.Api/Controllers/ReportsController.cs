using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Application.Errors;
using SimpleStore.Application.Reports;
using SimpleStore.Infrastructure.Identity;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize(Roles = ApplicationRoles.Owner)]
[Route("api/reports")]
public sealed class ReportsController(GetEndOfDayReportUseCase getEndOfDay) : ControllerBase
{
    [HttpGet("end-of-day")]
    public async Task<ActionResult<EndOfDayReportResult>> EndOfDay(
        [FromQuery] string? date,
        CancellationToken cancellationToken)
    {
        if (!DateOnly.TryParseExact(
                date,
                "yyyy-MM-dd",
                CultureInfo.InvariantCulture,
                DateTimeStyles.None,
                out var businessDate))
        {
            throw new ApplicationValidationException(
                "invalid-business-date",
                "Date must use the YYYY-MM-DD format.",
                [new ValidationError(null, "date", "invalid-business-date", "Date must use the YYYY-MM-DD format.")]);
        }

        return Ok(await getEndOfDay.ExecuteAsync(businessDate, cancellationToken));
    }
}
