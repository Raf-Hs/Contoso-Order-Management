using System.Security.Claims;
using Orders.Application.Security;

namespace Orders.Api.Security;

public sealed class HttpCurrentUser(IHttpContextAccessor httpContextAccessor)
    : ICurrentUser
{
    public Guid UserId
    {
        get
        {
            var subject = httpContextAccessor.HttpContext?.User
                .FindFirstValue("sub");

            if (Guid.TryParse(subject, out var userId))
                return userId;

            throw new UnauthorizedAccessException(
                "The authenticated subject must be a GUID.");
        }
    }
}
