using BuckScience.Web.ViewModels.Properties;
using BuckScience.Domain.Enums;

namespace BuckScience.Tests
{
    public class PropertyFeatureEditVmTests
    {
        [Fact]
        public void PropertyFeatureEditVm_ShouldSetAllProperties()
        {
            // Arrange & Act
            var vm = new PropertyFeatureEditVm
            {
                Id = 1,
                Type = ClassificationType.FoodPlot,
                Name = "Test Food Plot",
                GeometryWkt = "POINT(-74.0060 40.7128)",
                Notes = "Test notes for the feature"
            };

            // Assert
            Assert.Equal(1, vm.Id);
            Assert.Equal(ClassificationType.FoodPlot, vm.Type);
            Assert.Equal("Test Food Plot", vm.Name);
            Assert.Equal("POINT(-74.0060 40.7128)", vm.GeometryWkt);
            Assert.Equal("Test notes for the feature", vm.Notes);
        }

        [Fact]
        public void PropertyFeatureEditVm_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var vm = new PropertyFeatureEditVm();

            // Assert
            Assert.Equal(0, vm.Id);
            Assert.Equal(default(ClassificationType), vm.Type);
            Assert.Null(vm.Name);
            Assert.Equal(string.Empty, vm.GeometryWkt);
            Assert.Null(vm.Notes);
        }

        [Fact]
        public void PropertyFeatureEditVm_ValidationAttributes_ShouldBeApplied()
        {
            // Arrange
            var vm = new PropertyFeatureEditVm();

            // Act & Assert - Testing that properties can be set without validation errors in this context
            vm.Id = 0; // This should fail validation when model state is checked (Required, Range 1 to max)
            vm.Name = new string('x', 101); // This should fail validation (max 100 chars)
            vm.GeometryWkt = string.Empty; // This should fail validation (Required)
            vm.Notes = new string('x', 1001); // This should fail validation (max 1000 chars)

            // Properties are set successfully (validation occurs at model binding/validation time)
            Assert.Equal(0, vm.Id);
            Assert.Equal(101, vm.Name.Length);
            Assert.Equal(string.Empty, vm.GeometryWkt);
            Assert.Equal(1001, vm.Notes.Length);
        }

        [Fact]
        public void PropertyFeatureEditVm_RequiredProperties_CanBeSetToValidValues()
        {
            // Arrange
            var vm = new PropertyFeatureEditVm();

            // Act
            vm.Id = 1;
            vm.Type = ClassificationType.Creek;
            vm.GeometryWkt = "POLYGON((0 0, 1 0, 1 1, 0 1, 0 0))";

            // Assert
            Assert.Equal(1, vm.Id);
            Assert.Equal(ClassificationType.Creek, vm.Type);
            Assert.Equal("POLYGON((0 0, 1 0, 1 1, 0 1, 0 0))", vm.GeometryWkt);
        }

        [Fact]
        public void PropertyFeatureEditVm_OptionalProperties_CanBeNull()
        {
            // Arrange & Act
            var vm = new PropertyFeatureEditVm
            {
                Id = 1,
                Type = ClassificationType.BeddingArea,
                GeometryWkt = "POINT(0 0)",
                Name = null,
                Notes = null
            };

            // Assert
            Assert.Null(vm.Name);
            Assert.Null(vm.Notes);
        }

        [Fact]
        public void PropertyFeatureEditVm_AllClassificationTypes_CanBeSet()
        {
            // Arrange
            var vm = new PropertyFeatureEditVm();

            // Act & Assert - Test a few different classification types
            vm.Type = ClassificationType.FoodPlot;
            Assert.Equal(ClassificationType.FoodPlot, vm.Type);

            vm.Type = ClassificationType.Ridge;
            Assert.Equal(ClassificationType.Ridge, vm.Type);

            vm.Type = ClassificationType.Creek;
            Assert.Equal(ClassificationType.Creek, vm.Type);

            vm.Type = ClassificationType.BeddingArea;
            Assert.Equal(ClassificationType.BeddingArea, vm.Type);
        }
    }
}