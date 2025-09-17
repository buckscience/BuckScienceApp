namespace BuckScience.Domain.Enums
{
    /// <summary>
    /// Defines hunting seasons with default month mappings.
    /// Default mappings can be overridden per property using PropertySeasonMonthsOverride.
    /// </summary>
    public enum Season
    {
        /// <summary>
        /// Early season hunting (September-October)
        /// </summary>
        [Months(9, 10)]
        EarlySeason = 1,

        /// <summary>
        /// Pre-rutting season (October)
        /// </summary>
        [Months(10)]
        PreRut = 2,

        /// <summary>
        /// Peak rutting season (November)
        /// </summary>
        [Months(11)]
        Rut = 3,

        /// <summary>
        /// Post-rutting season (December)
        /// </summary>
        [Months(12)]
        PostRut = 4,

        /// <summary>
        /// Late season hunting (December-January)
        /// </summary>
        [Months(12, 1)]
        LateSeason = 5,

        /// <summary>
        /// Year-round season (all months)
        /// </summary>
        [Months(1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12)]
        YearRound = 6
    }
}