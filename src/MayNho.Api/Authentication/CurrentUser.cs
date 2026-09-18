using System.Security.Claims;
using MayNho.Application.Common;

namespace MayNho.Api.Authentication;

public class CurrentUser : ICurrentUser
{
    private readonly IHttpContextAccessor _httpContextAccessor;

    public CurrentUser(IHttpContextAccessor httpContextAccessor)
    {
        _httpContextAccessor = httpContextAccessor;
    }

    public Guid? UserId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var sub = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value;

            return Guid.TryParse(sub, out var id) ? id : null;
        }
    }

    public Guid? SessionId
    {
        get
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var sid = user?.FindFirst("sid")?.Value;

            return Guid.TryParse(sid, out var id) ? id : null;
        }
    }

    public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
