namespace BuckScience.Domain.Entities
{
    public class PhotoTag
    {
        protected PhotoTag() { } // EF

        public PhotoTag(int photoId, int tagId)
        {
            if (photoId <= 0) throw new ArgumentOutOfRangeException(nameof(photoId));
            if (tagId <= 0) throw new ArgumentOutOfRangeException(nameof(tagId));

            PhotoId = photoId;
            TagId = tagId;
        }

        public int PhotoId { get; private set; }
        public int TagId { get; private set; }

        // Optional navigations for convenience
        public virtual Photo Photo { get; private set; } = default!;
        public virtual Tag Tag { get; private set; } = default!;
    }
}