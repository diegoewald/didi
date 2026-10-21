using Microsoft.EntityFrameworkCore.Storage.ValueConversion;

namespace LocalErp.Persistence.ValueConverters;

public sealed class ScaledDecimalToInt64Converter : ValueConverter<decimal, long>
{
    public const decimal Scale = 1_000_000m;

    public ScaledDecimalToInt64Converter()
        : base(
            value => checked((long)(value * Scale)),
            value => value / Scale)
    {
    }
}
