using NodaTime;

namespace MayNho.Application.Common;

public interface IClock
{
    Instant GetCurrentInstant();
}
