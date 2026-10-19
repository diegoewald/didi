using LocalErp.Domain.Common;

namespace LocalErp.Domain.Products;

public sealed class Product : Entity
{
    public const int NameMaxLength = 200;

    private Product()
    {
        Name = string.Empty;
    }

    public Product(string name)
    {
        Name = NormalizeName(name);
        IsActive = true;
    }

    public string Name { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        Name = NormalizeName(name);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    private static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.Trim();
        if (normalized.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                name,
                $"Product name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }
}
