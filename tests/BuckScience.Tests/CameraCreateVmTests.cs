using BuckScience.Web.ViewModels.Cameras;

namespace BuckScience.Tests
{
    public class CameraCreateVmTests
    {
        [Fact]
        public void CameraCreateVm_ShouldIncludePropertyCoordinates()
        {
            // Arrange & Act
            var vm = new CameraCreateVm
            {
                PropertyId = 1,
                Name = "Test Camera",
                Brand = "Test Brand",
                Latitude = 40.7128,
                Longitude = -74.0060,
                PropertyLatitude = 40.7500,
                PropertyLongitude = -73.9857,
                IsActive = true
            };

            // Assert
            Assert.Equal(1, vm.PropertyId);
            Assert.Equal("Test Camera", vm.Name);
            Assert.Equal("Test Brand", vm.Brand);
            Assert.Equal(40.7128, vm.Latitude);
            Assert.Equal(-74.0060, vm.Longitude);
            Assert.Equal(40.7500, vm.PropertyLatitude);
            Assert.Equal(-73.9857, vm.PropertyLongitude);
            Assert.True(vm.IsActive);
        }

        [Fact]
        public void CameraCreateVm_PropertyCoordinates_ShouldBeInitializedToZero()
        {
            // Arrange & Act
            var vm = new CameraCreateVm();

            // Assert
            Assert.Equal(0, vm.PropertyLatitude);
            Assert.Equal(0, vm.PropertyLongitude);
        }
    }
}