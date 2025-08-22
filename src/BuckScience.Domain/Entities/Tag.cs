namespace BuckScience.Domain.Entities
    public class Tag
    {
        public int Id { get; set; }
        public string TagName { get; set; }
        public ICollection<Property> Properties { get; set; }
        public ICollection<Photo> Photos { get; set; }
        public ICollection<PropertyTag> PropertyTags { get; set; }
        public ICollection<PhotoTag> PhotoTags { get; set; }
    }
}
