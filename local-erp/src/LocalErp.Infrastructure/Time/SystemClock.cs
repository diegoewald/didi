using LocalErp.Application.Abstractions.Time;

namespace LocalErp.Infrastructure.Time;

public sealed class SystemClock : IClock
{
    public DateTime UtcNow => DateTime.UtcNow;
}
