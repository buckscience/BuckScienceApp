using BuckScience.Domain.Entities;
using BuckScience.Web.ViewModels.Cameras;

namespace BuckScience.Tests
{
    public class CameraPlacementHistoryTests
    {
        [Fact]
        public void Camera_PlaceAt_ShouldCreateNewPlacementHistory()
        {
            // Arrange
            var camera = new Camera("Test Brand", "Test Model");
            camera.PlaceInProperty(1);

            // Act
            camera.PlaceAt(40.7128, -74.0060, 180f, DateTime.UtcNow);

            // Assert
            Assert.Single(camera.PlacementHistories);
            var placement = camera.GetCurrentPlacement();
            Assert.NotNull(placement);
            Assert.Equal(40.7128, placement.Latitude);
            Assert.Equal(-74.0060, placement.Longitude);
            Assert.Equal(180f, placement.DirectionDegrees);
            Assert.True(placement.IsCurrentPlacement);
        }

        [Fact]
        public void Camera_Move_ShouldEndCurrentPlacementAndCreateNew()
        {
            // Arrange
            var camera = new Camera("Test Brand", "Test Model");
            camera.PlaceInProperty(1);
            var initialTime = DateTime.UtcNow.AddHours(-1);
            camera.PlaceAt(40.7128, -74.0060, 180f, initialTime);

            // Act
            var moveTime = DateTime.UtcNow;
            camera.Move(41.7128, -75.0060, 90f);

            // Assert
            Assert.Equal(2, camera.PlacementHistories.Count);
            
            var previousPlacement = camera.PlacementHistories.First();
            Assert.False(previousPlacement.IsCurrentPlacement);
            Assert.NotNull(previousPlacement.EndDateTime);
            
            var currentPlacement = camera.GetCurrentPlacement();
            Assert.NotNull(currentPlacement);
            Assert.Equal(41.7128, currentPlacement.Latitude);
            Assert.Equal(-75.0060, currentPlacement.Longitude);
            Assert.Equal(90f, currentPlacement.DirectionDegrees);
            Assert.True(currentPlacement.IsCurrentPlacement);
        }

        [Fact]
        public void Camera_CurrentLocationProperties_ShouldReturnFromPlacementHistory()
        {
            // Arrange
            var camera = new Camera("Test Brand", "Test Model");
            camera.PlaceInProperty(1);
            camera.PlaceAt(40.7128, -74.0060, 270f, DateTime.UtcNow);

            // Act & Assert
            Assert.Equal(40.7128, camera.Latitude);
            Assert.Equal(-74.0060, camera.Longitude);
            Assert.Equal(270f, camera.DirectionDegrees);
            Assert.NotNull(camera.Location);
        }

        [Fact]
        public void CameraCreateVm_ShouldIncludeDirectionDegrees()
        {
            // Arrange & Act
            var vm = new CameraCreateVm
            {
                PropertyId = 1,
                LocationName = "Test Location",
                Brand = "Test Brand",
                Latitude = 40.7128,
                Longitude = -74.0060,
                DirectionDegrees = 45f,
                IsActive = true
            };

            // Assert
            Assert.Equal(45f, vm.DirectionDegrees);
        }

        [Fact]
        public void CameraEditVm_ShouldIncludeDirectionDegrees()
        {
            // Arrange & Act
            var vm = new CameraEditVm
            {
                PropertyId = 1,
                Id = 1,
                LocationName = "Test Location",
                Brand = "Test Brand",
                Latitude = 40.7128,
                Longitude = -74.0060,
                DirectionDegrees = 135f,
                IsActive = true
            };

            // Assert
            Assert.Equal(135f, vm.DirectionDegrees);
        }

        [Fact]
        public void CameraPlacementHistory_EndPlacement_ShouldSetEndDateTime()
        {
            // Arrange
            var placement = new CameraPlacementHistory(1, 40.7128, -74.0060, 180f, DateTime.UtcNow.AddHours(-1));
            var endTime = DateTime.UtcNow;

            // Act
            placement.EndPlacement(endTime);

            // Assert
            Assert.Equal(endTime, placement.EndDateTime);
            Assert.False(placement.IsCurrentPlacement);
        }

        [Fact]
        public void CameraPlacementHistory_EndPlacement_ShouldThrowIfAlreadyEnded()
        {
            // Arrange
            var placement = new CameraPlacementHistory(1, 40.7128, -74.0060, 180f, DateTime.UtcNow.AddHours(-2));
            placement.EndPlacement(DateTime.UtcNow.AddHours(-1));

            // Act & Assert
            Assert.Throws<InvalidOperationException>(() => placement.EndPlacement(DateTime.UtcNow));
        }

        [Fact]
        public void CameraPlacementHistory_Duration_ShouldCalculateCorrectly()
        {
            // Arrange
            var startTime = DateTime.UtcNow.AddHours(-2);
            var endTime = DateTime.UtcNow;
            var placement = new CameraPlacementHistory(1, 40.7128, -74.0060, 180f, startTime);
            placement.EndPlacement(endTime);

            // Act
            var duration = placement.Duration;

            // Assert
            Assert.NotNull(duration);
            Assert.True(duration.Value.TotalHours >= 1.9 && duration.Value.TotalHours <= 2.1);
        }
    }
}