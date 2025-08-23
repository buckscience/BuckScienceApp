using System;
using System.Collections.Generic;

namespace BuckScience.Domain.Entities
{
    public class Tag
    {
        protected Tag() { } // EF

        public Tag(string tagName)
        {
            Rename(tagName);
        }

        public int Id { get; private set; }
        public string TagName { get; private set; } = string.Empty;
        public bool isDefaultTag { get; private set; } = false;

        // Use explicit join entities (keeps mapping unambiguous)
        public virtual ICollection<PropertyTag> PropertyTags { get; private set; } = new HashSet<PropertyTag>();
        public virtual ICollection<PhotoTag> PhotoTags { get; private set; } = new HashSet<PhotoTag>();

        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Tag name is required.", nameof(newName));
            TagName = newName.Trim();
        }
    }
}