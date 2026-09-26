using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using SimpleStore.Api.Operations;

namespace SimpleStore.Api.Controllers;

[ApiController]
[Authorize]
[Route("api/system")]
public sealed class SystemController(IApplicationBuildInfo buildInfo) : ControllerBase
{
    [HttpGet("version")]
    public ActionResult<SystemVersionResponse> Version() => Ok(new SystemVersionResponse(
        buildInfo.ApplicationVersion,
        buildInfo.CommitSha,
        buildInfo.Environment));
}

public sealed record SystemVersionResponse(
    string ApplicationVersion,
    string CommitSha,
    string Environment);
