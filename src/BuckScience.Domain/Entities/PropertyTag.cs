using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuckScience.Domain.Entities
{
    public class PropertyTag
    {
        [Key]
        public int PropertyId { get; set; }
        [NotMapped]
        public Property Property { get; set; }

        [Key]
        public int TagId { get; set; }
        public bool IsFastTag { get; set; }
        [NotMapped]
        public Tag Tag { get; set; }
        [NotMapped]
        public ICollection<Profile>? Profiles { get; set; }
    }

}
