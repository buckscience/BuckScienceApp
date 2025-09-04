using BuckScience.Domain.Enums;
using BuckScience.Domain.Helpers;
using Xunit;

namespace BuckScience.Tests
{
    public class FeatureHelperTests
    {
        [Fact]
        public void GetCategory_ShouldReturnCorrectCategoryForTopographicalFeatures()
        {
            // Arrange & Act & Assert
            Assert.Equal(FeatureCategory.Topographical, FeatureHelper.GetCategory(ClassificationType.Ridge));
            Assert.Equal(FeatureCategory.Topographical, FeatureHelper.GetCategory(ClassificationType.PinchPointFunnel));
            Assert.Equal(FeatureCategory.Topographical, FeatureHelper.GetCategory(ClassificationType.TravelCorridor));
            Assert.Equal(FeatureCategory.Topographical, FeatureHelper.GetCategory(ClassificationType.Saddle));
        }

        [Fact]
        public void GetCategory_ShouldReturnCorrectCategoryForFoodResources()
        {
            // Arrange & Act & Assert
            Assert.Equal(FeatureCategory.ResourceFood, FeatureHelper.GetCategory(ClassificationType.AgCropField));
            Assert.Equal(FeatureCategory.ResourceFood, FeatureHelper.GetCategory(ClassificationType.FoodPlot));
            Assert.Equal(FeatureCategory.ResourceFood, FeatureHelper.GetCategory(ClassificationType.MastTreePatch));
            Assert.Equal(FeatureCategory.ResourceFood, FeatureHelper.GetCategory(ClassificationType.BrowsePatch));
        }

        [Fact]
        public void GetCategory_ShouldReturnCorrectCategoryForWaterResources()
        {
            // Arrange & Act & Assert
            Assert.Equal(FeatureCategory.ResourceWater, FeatureHelper.GetCategory(ClassificationType.Creek));
            Assert.Equal(FeatureCategory.ResourceWater, FeatureHelper.GetCategory(ClassificationType.Pond));
            Assert.Equal(FeatureCategory.ResourceWater, FeatureHelper.GetCategory(ClassificationType.Lake));
            Assert.Equal(FeatureCategory.ResourceWater, FeatureHelper.GetCategory(ClassificationType.Spring));
        }

        [Fact]
        public void GetCategory_ShouldReturnCorrectCategoryForBeddingResources()
        {
            // Arrange & Act & Assert
            Assert.Equal(FeatureCategory.ResourceBedding, FeatureHelper.GetCategory(ClassificationType.BeddingArea));
            Assert.Equal(FeatureCategory.ResourceBedding, FeatureHelper.GetCategory(ClassificationType.ThickBrush));
            Assert.Equal(FeatureCategory.ResourceBedding, FeatureHelper.GetCategory(ClassificationType.CedarThicket));
            Assert.Equal(FeatureCategory.ResourceBedding, FeatureHelper.GetCategory(ClassificationType.EdgeCover));
        }

        [Fact]
        public void GetCategory_ShouldReturnOtherForOtherType()
        {
            // Arrange & Act & Assert
            Assert.Equal(FeatureCategory.Other, FeatureHelper.GetCategory(ClassificationType.Other));
        }

        [Fact]
        public void GetFeatureName_ShouldReturnCorrectDisplayNames()
        {
            // Arrange & Act & Assert
            Assert.Equal("Ridge", FeatureHelper.GetFeatureName(ClassificationType.Ridge));
            Assert.Equal("Pinch Point/Funnel", FeatureHelper.GetFeatureName(ClassificationType.PinchPointFunnel));
            Assert.Equal("Agricultural Crop Field", FeatureHelper.GetFeatureName(ClassificationType.AgCropField));
            Assert.Equal("Bedding Area", FeatureHelper.GetFeatureName(ClassificationType.BeddingArea));
        }

        [Fact]
        public void GetFeatureDescription_ShouldReturnCorrectDescriptions()
        {
            // Arrange & Act & Assert
            Assert.Equal("High ground that directs deer movement", FeatureHelper.GetFeatureDescription(ClassificationType.Ridge));
            Assert.Equal("Areas where deer rest during the day", FeatureHelper.GetFeatureDescription(ClassificationType.BeddingArea));
            Assert.Equal("Natural flowing water source", FeatureHelper.GetFeatureDescription(ClassificationType.Creek));
        }

        [Fact]
        public void GetFeatureIcon_ShouldReturnCorrectIcons()
        {
            // Arrange & Act & Assert
            Assert.Equal("fas fa-mountain", FeatureHelper.GetFeatureIcon(ClassificationType.Ridge));
            Assert.Equal("fas fa-bed", FeatureHelper.GetFeatureIcon(ClassificationType.BeddingArea));
            Assert.Equal("fas fa-seedling", FeatureHelper.GetFeatureIcon(ClassificationType.FoodPlot));
            Assert.Equal("fas fa-tint", FeatureHelper.GetFeatureIcon(ClassificationType.Pond));
        }

        [Fact]
        public void GetCategoryName_ShouldReturnCorrectCategoryNames()
        {
            // Arrange & Act & Assert
            Assert.Equal("Topographical", FeatureHelper.GetCategoryName(FeatureCategory.Topographical));
            Assert.Equal("Food Resources", FeatureHelper.GetCategoryName(FeatureCategory.ResourceFood));
            Assert.Equal("Water Resources", FeatureHelper.GetCategoryName(FeatureCategory.ResourceWater));
            Assert.Equal("Bedding & Cover", FeatureHelper.GetCategoryName(FeatureCategory.ResourceBedding));
        }

        [Fact]
        public void CategoryMapping_ShouldContainAllClassificationTypes()
        {
            // Arrange
            var allClassificationTypes = Enum.GetValues<ClassificationType>();

            // Act & Assert
            foreach (var type in allClassificationTypes)
            {
                Assert.True(FeatureHelper.CategoryMapping.ContainsKey(type), 
                    $"CategoryMapping missing entry for {type}");
            }
        }

        [Fact]
        public void FeatureNames_ShouldContainAllClassificationTypes()
        {
            // Arrange
            var allClassificationTypes = Enum.GetValues<ClassificationType>();

            // Act & Assert
            foreach (var type in allClassificationTypes)
            {
                Assert.True(FeatureHelper.FeatureNames.ContainsKey(type), 
                    $"FeatureNames missing entry for {type}");
                Assert.False(string.IsNullOrWhiteSpace(FeatureHelper.FeatureNames[type]), 
                    $"FeatureNames contains empty value for {type}");
            }
        }

        [Fact]
        public void FeatureDescriptions_ShouldContainAllClassificationTypes()
        {
            // Arrange
            var allClassificationTypes = Enum.GetValues<ClassificationType>();

            // Act & Assert
            foreach (var type in allClassificationTypes)
            {
                Assert.True(FeatureHelper.FeatureDescriptions.ContainsKey(type), 
                    $"FeatureDescriptions missing entry for {type}");
                Assert.False(string.IsNullOrWhiteSpace(FeatureHelper.FeatureDescriptions[type]), 
                    $"FeatureDescriptions contains empty value for {type}");
            }
        }

        [Fact]
        public void FeatureIcons_ShouldContainAllClassificationTypes()
        {
            // Arrange
            var allClassificationTypes = Enum.GetValues<ClassificationType>();

            // Act & Assert
            foreach (var type in allClassificationTypes)
            {
                Assert.True(FeatureHelper.FeatureIcons.ContainsKey(type), 
                    $"FeatureIcons missing entry for {type}");
                Assert.False(string.IsNullOrWhiteSpace(FeatureHelper.FeatureIcons[type]), 
                    $"FeatureIcons contains empty value for {type}");
            }
        }
    }
}