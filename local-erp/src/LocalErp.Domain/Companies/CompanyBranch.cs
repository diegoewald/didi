using LocalErp.Domain.Common;

namespace LocalErp.Domain.Companies;

public sealed class CompanyBranch : Entity
{
    public const int NameMaxLength = 200;

    private CompanyBranch()
    {
        CompanyId = Guid.Empty;
        Name = string.Empty;
    }

    public CompanyBranch(Guid companyId, string name)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company identifier cannot be empty.", nameof(companyId));
        }

        CompanyId = companyId;
        Name = NormalizeName(name);
        IsActive = true;
    }

    public Guid CompanyId { get; private set; }

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
                $"Company branch name cannot exceed {NameMaxLength} characters.");
        }

        return normalized;
    }
}
