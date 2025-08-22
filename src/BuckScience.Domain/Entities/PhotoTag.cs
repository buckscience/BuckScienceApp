using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BuckScience.Domain.Entities
{
    public class PhotoTag
    {
        public int PhotoId { get; set; }
        [NotMapped]
        public Photo Photo { get; set; }

        public int TagId { get; set; }
        [NotMapped]
        public Tag Tag { get; set; }
    }

}
