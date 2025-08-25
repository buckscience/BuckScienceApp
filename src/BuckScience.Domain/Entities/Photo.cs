using System;
using System.Collections.Generic;

namespace BuckScience.Domain.Entities
{
    public class Photo
    {
        protected Photo() { } // EF

        // Traditional constructor for existing workflow
        public Photo(int cameraId, string photoUrl, DateTime dateTaken, DateTime? dateUploaded = null)
        {
            SetCamera(cameraId);
            SetPhotoUrl(photoUrl);
            SetDateTaken(dateTaken);
            DateUploaded = dateUploaded ?? DateTime.UtcNow;
        }

        // Azure pipeline constructor
        public Photo(
            string userId,
            int cameraId,
            string contentHash,
            string thumbBlobName,
            string displayBlobName,
            DateTime? takenAtUtc = null,
            decimal? latitude = null,
            decimal? longitude = null)
        {
            SetUserId(userId);
            SetCamera(cameraId);
            SetContentHash(contentHash);
            SetBlobNames(thumbBlobName, displayBlobName);
            TakenAtUtc = takenAtUtc;
            Latitude = latitude;
            Longitude = longitude;
            Status = "processing";
            DateTaken = takenAtUtc ?? DateTime.UtcNow;
            DateUploaded = DateTime.UtcNow;
        }

        public int Id { get; private set; }

        public DateTime DateTaken { get; private set; }
        public DateTime DateUploaded { get; private set; }
        public string PhotoUrl { get; private set; } = string.Empty;

        // Required relationship to Camera
        public int CameraId { get; private set; }
        public virtual Camera Camera { get; private set; } = default!;

        // Optional relationship to Weather
        public int? WeatherId { get; private set; }
        public virtual Weather? Weather { get; private set; }

        // Many-to-many via explicit join entity
        public virtual ICollection<PhotoTag> PhotoTags { get; private set; } = new HashSet<PhotoTag>();

        // Azure pipeline properties
        public string? UserId { get; private set; }
        public string? ContentHash { get; private set; }
        public string? ThumbBlobName { get; private set; }
        public string? DisplayBlobName { get; private set; }
        public DateTime? TakenAtUtc { get; private set; }
        public decimal? Latitude { get; private set; }
        public decimal? Longitude { get; private set; }
        public string? WeatherJson { get; private set; }
        public string? Status { get; private set; }

        // Behavior methods for traditional workflow
        public void SetPhotoUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("PhotoUrl is required.", nameof(url));
            PhotoUrl = url.Trim();
        }

        public void SetDateTaken(DateTime dateTaken)
        {
            DateTaken = dateTaken;
        }

        public void MarkUploaded(DateTime uploadedUtc)
        {
            DateUploaded = uploadedUtc;
        }

        public void SetCamera(int cameraId)
        {
            if (cameraId <= 0) throw new ArgumentOutOfRangeException(nameof(cameraId));
            CameraId = cameraId;
        }

        public void SetWeather(int? weatherId)
        {
            if (weatherId.HasValue && weatherId.Value <= 0)
                throw new ArgumentOutOfRangeException(nameof(weatherId));

            WeatherId = weatherId;
        }

        // Azure pipeline behavior methods
        public void SetUserId(string? userId)
        {
            if (!string.IsNullOrWhiteSpace(userId))
                UserId = userId.Trim();
        }

        public void SetContentHash(string? contentHash)
        {
            if (!string.IsNullOrWhiteSpace(contentHash))
                ContentHash = contentHash.Trim();
        }

        public void SetBlobNames(string? thumbBlobName, string? displayBlobName)
        {
            if (!string.IsNullOrWhiteSpace(thumbBlobName))
                ThumbBlobName = thumbBlobName.Trim();
            if (!string.IsNullOrWhiteSpace(displayBlobName))
                DisplayBlobName = displayBlobName.Trim();
        }

        public void SetWeatherData(string? weatherJson)
        {
            WeatherJson = weatherJson;
        }

        public void SetStatus(string? status)
        {
            if (!string.IsNullOrWhiteSpace(status))
                Status = status.Trim();
        }

        public void MarkReady(string? weatherJson = null)
        {
            WeatherJson = weatherJson;
            Status = "ready";
        }

        public void MarkFailed()
        {
            Status = "failed";
        }
    }
}