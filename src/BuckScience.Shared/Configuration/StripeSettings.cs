using System.ComponentModel.DataAnnotations;

namespace BuckScience.Shared.Configuration;

public class StripeSettings
{
    public const string SectionName = "StripeSettings";

    [Required]
    public string PublishableKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;

    public Dictionary<string, string> PriceIds { get; set; } = new();
}