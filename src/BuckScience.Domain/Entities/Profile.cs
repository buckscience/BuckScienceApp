using System;
using BuckScience.Domain.Enums;

namespace BuckScience.Domain.Entities
{
    public class Profile
    {
        protected Profile() { } // EF

        public Profile(string name, int propertyId, int tagId, ProfileStatus profileStatus)
        {
            Rename(name);
            AssignProperty(propertyId);
            AssignTag(tagId);
            SetStatus(profileStatus);
        }

        public int Id { get; private set; }
        public string Name { get; private set; } = string.Empty;

        public ProfileStatus ProfileStatus { get; private set; }

        public int PropertyId { get; private set; }
        public virtual Property Property { get; private set; } = default!;

        public int TagId { get; private set; }
        public virtual Tag Tag { get; private set; } = default!;

        public string? CoverPhotoUrl { get; private set; }

        // Behavior
        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
            Name = newName.Trim();
        }

        public void SetStatus(ProfileStatus status)
        {
            ProfileStatus = status;
        }

        public void AssignProperty(int propertyId)
        {
            if (propertyId <= 0) throw new ArgumentOutOfRangeException(nameof(propertyId));
            PropertyId = propertyId;
        }

        public void AssignTag(int tagId)
        {
            if (tagId <= 0) throw new ArgumentOutOfRangeException(nameof(tagId));
            TagId = tagId;
        }

        public void SetCoverPhoto(string? coverPhotoUrl)
        {
            CoverPhotoUrl = coverPhotoUrl;
        }
    }
}