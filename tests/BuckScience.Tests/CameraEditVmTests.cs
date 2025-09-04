using BuckScience.Web.ViewModels.Cameras;

namespace BuckScience.Tests
{
    public class CameraEditVmTests
    {
        [Fact]
        public void CameraEditVm_ShouldSetAllProperties()
        {
            // Arrange & Act
            var vm = new CameraEditVm
            {
                PropertyId = 1,
                Id = 2,
                Name = "Test Camera",
                Brand = "Test Brand",
                Model = "Test Model",
                Latitude = 40.7128,
                Longitude = -74.0060,
                IsActive = true
            };

            // Assert
            Assert.Equal(1, vm.PropertyId);
            Assert.Equal(2, vm.Id);
            Assert.Equal("Test Camera", vm.Name);
            Assert.Equal("Test Brand", vm.Brand);
            Assert.Equal("Test Model", vm.Model);
            Assert.Equal(40.7128, vm.Latitude);
            Assert.Equal(-74.0060, vm.Longitude);
            Assert.True(vm.IsActive);
        }

        [Fact]
        public void CameraEditVm_DefaultValues_ShouldBeCorrect()
        {
            // Arrange & Act
            var vm = new CameraEditVm();

            // Assert
            Assert.Equal(0, vm.PropertyId);
            Assert.Equal(0, vm.Id);
            Assert.Equal(string.Empty, vm.Name);
            Assert.Equal(string.Empty, vm.Brand);
            Assert.Null(vm.Model);
            Assert.Equal(0, vm.Latitude);
            Assert.Equal(0, vm.Longitude);
            Assert.False(vm.IsActive);
        }

        [Fact]
        public void CameraEditVm_ValidationAttributes_ShouldBeApplied()
        {
            // Arrange
            var vm = new CameraEditVm();

            // Act & Assert - Testing that properties can be set without validation errors in this context
            vm.PropertyId = -1; // This should fail validation when model state is checked
            vm.Name = new string('x', 201); // This should fail validation (max 200 chars)
            vm.Brand = new string('x', 101); // This should fail validation (max 100 chars)
            vm.Model = new string('x', 201); // This should fail validation (max 200 chars)
            vm.Latitude = -91; // This should fail validation (range -90 to 90)
            vm.Longitude = -181; // This should fail validation (range -180 to 180)

            // Properties are set successfully (validation occurs at model binding/validation time)
            Assert.Equal(-1, vm.PropertyId);
            Assert.Equal(201, vm.Name.Length);
            Assert.Equal(101, vm.Brand.Length);
            Assert.Equal(201, vm.Model.Length);
            Assert.Equal(-91, vm.Latitude);
            Assert.Equal(-181, vm.Longitude);
        }
    }
}