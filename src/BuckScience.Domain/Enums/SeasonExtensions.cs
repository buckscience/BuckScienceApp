using System;
using System.Linq;
using System.Reflection;
using BuckScience.Domain.Entities;

namespace BuckScience.Domain.Enums
{
    /// <summary>
    /// Extension methods for Season enum providing month mapping functionality.
    /// Supports both default mappings (via MonthsAttribute) and property-specific overrides.
    /// </summary>
    public static class SeasonExtensions
    {
        /// <summary>
        /// Gets the default months for a season from its MonthsAttribute.
        /// </summary>
        /// <param name="season">The season to get months for.</param>
        /// <returns>Array of month integers (1-12) representing the default months for the season.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the season does not have a MonthsAttribute defined.</exception>
        public static int[] GetDefaultMonths(this Season season)
        {
            var seasonField = typeof(Season).GetField(season.ToString());
            
            if (seasonField == null)
                throw new InvalidOperationException($"Field for season {season} not found.");

            var monthsAttribute = seasonField.GetCustomAttribute<MonthsAttribute>();
            
            if (monthsAttribute == null)
                throw new InvalidOperationException($"Season {season} does not have a MonthsAttribute defined.");

            return monthsAttribute.Months.ToArray(); // Return a copy to prevent modification
        }

        /// <summary>
        /// Gets the months for a season for a specific property.
        /// Checks for property-specific overrides first, then falls back to default months.
        /// </summary>
        /// <param name="season">The season to get months for.</param>
        /// <param name="property">The property to check for custom overrides.</param>
        /// <returns>Array of month integers (1-12) representing the months for the season and property.</returns>
        /// <remarks>
        /// This method requires a database context to check for overrides. In practice, this would be
        /// called from a service or repository that has access to the database context.
        /// For now, this is a placeholder that returns default months only.
        /// The actual override logic should be implemented in a service class with database access.
        /// </remarks>
        public static int[] GetMonthsForProperty(this Season season, Property property)
        {
            // TODO: Implement database lookup for PropertySeasonMonthsOverride
            // For now, return default months as this extension method doesn't have database access
            // The actual implementation should be in a service class with IAppDbContext dependency
            return season.GetDefaultMonths();
        }
    }
}