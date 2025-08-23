using System;

namespace BuckScience.Domain.Entities
{
    public class PropertyTag
    {
        protected PropertyTag() { } // EF

        public PropertyTag(int propertyId, int tagId, bool isFastTag = false)
        {
            if (propertyId <= 0) throw new ArgumentOutOfRangeException(nameof(propertyId));
            if (tagId <= 0) throw new ArgumentOutOfRangeException(nameof(tagId));

            PropertyId = propertyId;
            TagId = tagId;
            IsFastTag = isFastTag;
        }

        public int PropertyId { get; private set; }
        public int TagId { get; private set; }
        public bool IsFastTag { get; private set; }

        public void SetFastTag(bool isFast) => IsFastTag = isFast;

        // Optional navigations for convenience
        public virtual Property Property { get; private set; } = default!;
        public virtual Tag Tag { get; private set; } = default!;
    }
}