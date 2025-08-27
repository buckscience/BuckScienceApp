using BuckScience.Web.ViewModels.Cameras;

namespace BuckScience.Tests
{
    public class CameraDetailsVmTests
    {
        [Fact]
        public void CameraDetailsVm_ShouldHaveAllRequiredProperties()
        {
            // Arrange & Act
            var vm = new CameraDetailsVm
            {
                Id = 1,
                Name = "Test Camera",
                Brand = "Test Brand",
                Model = "Test Model",
                Latitude = 40.7128,
                Longitude = -74.0060,
                IsActive = true,
                PhotoCount = 15,
                CreatedDate = new DateTime(2024, 1, 1),
                PropertyId = 2,
                PropertyName = "Test Property"
            };

            // Assert
            Assert.Equal(1, vm.Id);
            Assert.Equal("Test Camera", vm.Name);
            Assert.Equal("Test Brand", vm.Brand);
            Assert.Equal("Test Model", vm.Model);
            Assert.Equal(40.7128, vm.Latitude);
            Assert.Equal(-74.0060, vm.Longitude);
            Assert.True(vm.IsActive);
            Assert.Equal(15, vm.PhotoCount);
            Assert.Equal(new DateTime(2024, 1, 1), vm.CreatedDate);
            Assert.Equal(2, vm.PropertyId);
            Assert.Equal("Test Property", vm.PropertyName);
        }

        [Fact]
        public void CameraDetailsVm_ShouldAllowNullModel()
        {
            // Arrange & Act
            var vm = new CameraDetailsVm
            {
                Name = "Camera without model",
                Brand = "Brand",
                Model = null
            };

            // Assert
            Assert.Null(vm.Model);
        }
    }
}