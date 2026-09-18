namespace MayNho.Infrastructure.Services;

public sealed class SystemClockAdapter : MayNho.Application.Common.IClock
{
    public NodaTime.Instant GetCurrentInstant() => NodaTime.SystemClock.Instance.GetCurrentInstant();
}

