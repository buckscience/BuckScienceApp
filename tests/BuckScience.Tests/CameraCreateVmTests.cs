using BuckScience.Web.ViewModels.Cameras;
using BuckScience.Web.Helpers;

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
                LocationName = "Test Location",
                Brand = "Test Brand",
                Latitude = 40.7128,
                Longitude = -74.0060,
                PropertyLatitude = 40.7500,
                PropertyLongitude = -73.9857,
                IsActive = true
            };

            // Assert
            Assert.Equal(1, vm.PropertyId);
            Assert.Equal("Test Location", vm.LocationName);
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

        [Fact]
        public void CameraCreateVm_SyncDirectionFromSelection_ShouldUpdateDirectionDegrees()
        {
            // Arrange
            var vm = new CameraCreateVm
            {
                DirectionSelection = DirectionHelper.CompassDirection.S // South = 180°
            };

            // Act
            vm.SyncDirectionFromSelection();

            // Assert
            Assert.Equal(180f, vm.DirectionDegrees);
        }

        [Fact]
        public void CameraCreateVm_SyncSelectionFromDirection_ShouldUpdateDirectionSelection()
        {
            // Arrange
            var vm = new CameraCreateVm
            {
                DirectionDegrees = 90f // East
            };

            // Act
            vm.SyncSelectionFromDirection();

            // Assert
            Assert.Equal(DirectionHelper.CompassDirection.E, vm.DirectionSelection);
        }

        [Theory]
        [InlineData(DirectionHelper.CompassDirection.N, 0f)]
        [InlineData(DirectionHelper.CompassDirection.NE, 45f)]
        [InlineData(DirectionHelper.CompassDirection.E, 90f)]
        [InlineData(DirectionHelper.CompassDirection.SE, 135f)]
        [InlineData(DirectionHelper.CompassDirection.S, 180f)]
        [InlineData(DirectionHelper.CompassDirection.SW, 225f)]
        [InlineData(DirectionHelper.CompassDirection.W, 270f)]
        [InlineData(DirectionHelper.CompassDirection.NW, 315f)]
        public void CameraCreateVm_DirectionSync_ShouldWorkForAllDirections(DirectionHelper.CompassDirection direction, float expectedDegrees)
        {
            // Arrange
            var vm = new CameraCreateVm
            {
                DirectionSelection = direction
            };

            // Act
            vm.SyncDirectionFromSelection();

            // Assert
            Assert.Equal(expectedDegrees, vm.DirectionDegrees);
        }
    }
}