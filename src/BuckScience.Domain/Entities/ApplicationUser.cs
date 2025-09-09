using System;

namespace BuckScience.Domain.Entities;

public class ApplicationUser
{
    public int Id { get; set; }
    public string AzureEntraB2CId { get; set; } = null!; // B2C oid
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string DisplayName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime? TrialStartDate { get; set; }

    // Navigation properties
    public virtual ICollection<FeatureWeight> FeatureWeights { get; set; } = new List<FeatureWeight>();
}