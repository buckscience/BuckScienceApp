namespace BuckScience.Domain.Entities
{
    public class Photo
    {
        public int Id { get; set; }
        public DateTime DateTaken { get; set; }
        public DateTime DateUploaded { get; set; }
        public string PhotoUrl { get; set; }
        public int CameraId { get; set; }
        public Camera Camera { get; set; }
        public ICollection<Tag> Tags { get; set; }
        public int? WeatherId { get; set; }
        public Weather Weather { get; set; }
        public ICollection<PhotoTag> PhotoTags { get; set; }
    }
}
