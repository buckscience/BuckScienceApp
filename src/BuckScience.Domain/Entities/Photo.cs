using System;
using System.Collections.Generic;

namespace BuckScience.Domain.Entities
{
    public class Photo
    {
        protected Photo() { } // EF

        public Photo(int cameraId, string photoUrl, DateTime dateTaken, DateTime? dateUploaded = null)
        {
            SetCamera(cameraId);
            SetPhotoUrl(photoUrl);
            SetDateTaken(dateTaken);
            DateUploaded = dateUploaded ?? DateTime.UtcNow;
        }

        public Photo(int cameraId, string displayUrl, string? thumbnailUrl, DateTime dateTaken, long? displaySizeBytes = null, long? thumbnailSizeBytes = null, DateTime? dateUploaded = null)
        {
            SetCamera(cameraId);
            SetPhotoUrl(displayUrl);
            SetThumbnailUrl(thumbnailUrl);
            SetFileSizes(displaySizeBytes, thumbnailSizeBytes);
            SetDateTaken(dateTaken);
            DateUploaded = dateUploaded ?? DateTime.UtcNow;
        }

        public int Id { get; private set; }

        public DateTime DateTaken { get; private set; }
        public DateTime DateUploaded { get; private set; }
        public string PhotoUrl { get; private set; } = string.Empty;
        public string? ThumbnailUrl { get; private set; }
        public long? FileSizeBytes { get; private set; }
        public long? ThumbnailSizeBytes { get; private set; }

        // Required relationship to Camera
        public int CameraId { get; private set; }
        public virtual Camera Camera { get; private set; } = default!;

        // Optional relationship to Weather
        public int? WeatherId { get; private set; }
        public virtual Weather? Weather { get; private set; }

        // Many-to-many via explicit join entity
        public virtual ICollection<PhotoTag> PhotoTags { get; private set; } = new HashSet<PhotoTag>();

        // Behavior
        public void SetPhotoUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) throw new ArgumentException("PhotoUrl is required.", nameof(url));
            PhotoUrl = url.Trim();
        }

        public void SetThumbnailUrl(string? url)
        {
            ThumbnailUrl = url?.Trim();
        }

        public void SetFileSizes(long? displaySizeBytes, long? thumbnailSizeBytes)
        {
            FileSizeBytes = displaySizeBytes;
            ThumbnailSizeBytes = thumbnailSizeBytes;
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
    }
}