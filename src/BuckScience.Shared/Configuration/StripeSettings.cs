using System.ComponentModel.DataAnnotations;

namespace BuckScience.Shared.Configuration;

public class StripeSettings
{
    public const string SectionName = "Stripe";

    [Required]
    public string PublishableKey { get; set; } = string.Empty;

    [Required]
    public string SecretKey { get; set; } = string.Empty;

    [Required]
    public string WebhookSecret { get; set; } = string.Empty;

    public string PriceFawn { get; set; } = string.Empty;
    public string PriceDoe { get; set; } = string.Empty;
    public string PriceBuck { get; set; } = string.Empty;
    public string PriceTrophy { get; set; } = string.Empty;

    public Dictionary<string, string> GetPriceIds()
    {
        return new Dictionary<string, string>
        {
            { "fawn", PriceFawn },
            { "doe", PriceDoe },
            { "buck", PriceBuck },
            { "trophy", PriceTrophy }
        };
    }
}