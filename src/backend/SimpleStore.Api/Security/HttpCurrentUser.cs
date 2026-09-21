using System.Security.Claims;
using SimpleStore.Application.Abstractions;

namespace SimpleStore.Api.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor) : ICurrentUser
{
    private ClaimsPrincipal? Principal => httpContextAccessor.HttpContext?.User;

    public bool IsAuthenticated => Principal?.Identity?.IsAuthenticated == true;

    public Guid UserId => Guid.TryParse(
        Principal?.FindFirstValue(ClaimTypes.NameIdentifier),
        out var userId)
        ? userId
        : Guid.Empty;
}
