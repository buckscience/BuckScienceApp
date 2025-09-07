using BuckScience.Domain.Entities;

namespace BuckScience.Tests
{
    public class PhotoPlacementHistoryTests
    {
        [Fact]
        public void Photo_Constructor_ShouldAcceptCameraPlacementHistoryId()
        {
            // Arrange
            var cameraId = 1;
            var photoUrl = "https://example.com/photo.jpg";
            var dateTaken = DateTime.UtcNow;
            var placementHistoryId = 5;

            // Act
            var photo = new Photo(cameraId, photoUrl, dateTaken, cameraPlacementHistoryId: placementHistoryId);

            // Assert
            Assert.Equal(cameraId, photo.CameraId);
            Assert.Equal(photoUrl, photo.PhotoUrl);
            Assert.Equal(dateTaken, photo.DateTaken);
            Assert.Equal(placementHistoryId, photo.CameraPlacementHistoryId);
        }

        [Fact]
        public void Photo_Constructor_ShouldAllowNullCameraPlacementHistoryId()
        {
            // Arrange
            var cameraId = 1;
            var photoUrl = "https://example.com/photo.jpg";
            var dateTaken = DateTime.UtcNow;

            // Act
            var photo = new Photo(cameraId, photoUrl, dateTaken);

            // Assert
            Assert.Equal(cameraId, photo.CameraId);
            Assert.Equal(photoUrl, photo.PhotoUrl);
            Assert.Equal(dateTaken, photo.DateTaken);
            Assert.Null(photo.CameraPlacementHistoryId);
        }

        [Fact]
        public void Photo_SetCameraPlacementHistory_ShouldSetValue()
        {
            // Arrange
            var photo = new Photo(1, "https://example.com/photo.jpg", DateTime.UtcNow);
            var placementHistoryId = 10;

            // Act
            photo.SetCameraPlacementHistory(placementHistoryId);

            // Assert
            Assert.Equal(placementHistoryId, photo.CameraPlacementHistoryId);
        }

        [Fact]
        public void Photo_SetCameraPlacementHistory_ShouldAllowNull()
        {
            // Arrange
            var photo = new Photo(1, "https://example.com/photo.jpg", DateTime.UtcNow);
            photo.SetCameraPlacementHistory(5); // Set initially

            // Act
            photo.SetCameraPlacementHistory(null);

            // Assert
            Assert.Null(photo.CameraPlacementHistoryId);
        }

        [Fact]
        public void Photo_SetCameraPlacementHistory_ShouldThrowForInvalidId()
        {
            // Arrange
            var photo = new Photo(1, "https://example.com/photo.jpg", DateTime.UtcNow);

            // Act & Assert
            Assert.Throws<ArgumentOutOfRangeException>(() => photo.SetCameraPlacementHistory(0));
            Assert.Throws<ArgumentOutOfRangeException>(() => photo.SetCameraPlacementHistory(-1));
        }

        [Fact]
        public void Photo_WithPlacementHistory_ShouldAssociateCorrectly()
        {
            // Arrange
            var validCameraId = 1;
            var validPlacementHistoryId = 5;

            // Act
            var photo = new Photo(
                validCameraId, 
                "https://example.com/photo.jpg", 
                DateTime.UtcNow, 
                cameraPlacementHistoryId: validPlacementHistoryId);

            // Assert
            Assert.Equal(validPlacementHistoryId, photo.CameraPlacementHistoryId);
            Assert.Equal(validCameraId, photo.CameraId);
        }
    }
}