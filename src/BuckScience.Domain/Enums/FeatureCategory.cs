namespace BuckScience.Domain.Enums
{
    /// <summary>
    /// Categorizes property features into high-level groups for organization and filtering.
    /// </summary>
    public enum FeatureCategory
    {
        /// <summary>
        /// Topographical and terrain features that shape deer movement patterns
        /// </summary>
        Topographical = 1,

        /// <summary>
        /// Food-related resource features
        /// </summary>
        ResourceFood = 2,

        /// <summary>
        /// Water-related resource features
        /// </summary>
        ResourceWater = 3,

        /// <summary>
        /// Bedding and cover-related resource features
        /// </summary>
        ResourceBedding = 4,

        /// <summary>
        /// Other miscellaneous features
        /// </summary>
        Other = 99
    }
}