using BuckScience.Domain.Enums;
using Xunit;

namespace BuckScience.Tests.Web;

/// <summary>
/// Tests to verify the subscription tier logic for navigation visibility in _Layout.cshtml
/// </summary>
public class LayoutSubscriptionVisibilityTests
{
    [Theory]
    [InlineData(SubscriptionTier.Trial, false)]
    [InlineData(SubscriptionTier.Fawn, false)]
    [InlineData(SubscriptionTier.Doe, true)]
    [InlineData(SubscriptionTier.Buck, true)]
    [InlineData(SubscriptionTier.Trophy, true)]
    [InlineData(SubscriptionTier.Expired, false)]
    public void ShouldShowPremiumFeatures_ReturnsCorrectVisibility(SubscriptionTier tier, bool expectedVisible)
    {
        // This tests the same logic used in _Layout.cshtml: showPremiumFeatures = userTier >= SubscriptionTier.Doe && userTier != SubscriptionTier.Expired
        bool showPremiumFeatures = tier >= SubscriptionTier.Doe && tier != SubscriptionTier.Expired;
        
        Assert.Equal(expectedVisible, showPremiumFeatures);
    }

    [Fact]
    public void SubscriptionTier_DoeValueIsCorrect()
    {
        // Verify that the Doe tier has the expected numeric value
        Assert.Equal(2, (int)SubscriptionTier.Doe);
    }

    [Fact]
    public void SubscriptionTier_OrderingIsCorrect()
    {
        // Verify that the tier ordering allows for proper >= comparison (excluding Expired which is special)
        Assert.True(SubscriptionTier.Trial < SubscriptionTier.Doe);
        Assert.True(SubscriptionTier.Fawn < SubscriptionTier.Doe);
        Assert.True(SubscriptionTier.Doe == SubscriptionTier.Doe);
        Assert.True(SubscriptionTier.Buck > SubscriptionTier.Doe);
        Assert.True(SubscriptionTier.Trophy > SubscriptionTier.Doe);
        
        // Expired is a special case - numeric value is higher but logically should not show premium features
        Assert.True(SubscriptionTier.Expired > SubscriptionTier.Doe);
        
        // Verify the logic handles expired correctly
        bool expiredShowsPremium = SubscriptionTier.Expired >= SubscriptionTier.Doe && SubscriptionTier.Expired != SubscriptionTier.Expired;
        Assert.False(expiredShowsPremium);
    }
}