using System.Text;
using LocalErp.Domain.Common;

namespace LocalErp.Domain.Security;

public sealed class Role : Entity
{
    public const int NameMaxLength = 100;

    private Role()
    {
        Name = string.Empty;
        NormalizedName = string.Empty;
    }

    public Role(string name)
    {
        SetName(name);
        IsActive = true;
    }

    public string Name { get; private set; } = string.Empty;

    public string NormalizedName { get; private set; } = string.Empty;

    public bool IsActive { get; private set; }

    public void Rename(string name)
    {
        SetName(name);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static string NormalizeName(string name)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        var normalized = name.Trim().Normalize(NormalizationForm.FormKC);
        if (normalized.Length > NameMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(name),
                name,
                $"Role name cannot exceed {NameMaxLength} characters.");
        }

        return normalized.ToUpperInvariant();
    }

    private void SetName(string name)
    {
        NormalizedName = NormalizeName(name);
        Name = name.Trim().Normalize(NormalizationForm.FormKC);
    }
}
