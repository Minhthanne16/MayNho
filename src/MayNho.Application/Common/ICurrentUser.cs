namespace MayNho.Application.Common;

public interface ICurrentUser
{
    Guid? UserId { get; }
    Guid? SessionId { get; }
    bool IsAuthenticated { get; }
}
