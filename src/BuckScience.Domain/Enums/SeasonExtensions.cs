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
        /// Gets the months for a season for a specific property using default mapping only.
        /// This extension method does not check for property-specific overrides.
        /// </summary>
        /// <param name="season">The season to get months for.</param>
        /// <param name="property">The property (parameter kept for API compatibility but not used).</param>
        /// <returns>Array of month integers (1-12) representing the default months for the season.</returns>
        /// <remarks>
        /// <para>
        /// This extension method provides default month mappings only and does not access the database.
        /// For hybrid logic that includes property-specific overrides, use the SeasonMonthMappingService
        /// which implements the complete hybrid mapping logic:
        /// 1. Check for property-specific overrides in PropertySeasonMonthsOverride table
        /// 2. Fall back to default months from MonthsAttribute if no override exists
        /// </para>
        /// <para>
        /// <strong>Usage Recommendations:</strong><br/>
        /// - Use this extension method when you only need default season mappings<br/>
        /// - Use SeasonMonthMappingService.GetMonthsForPropertyAsync() for hybrid logic with overrides<br/>
        /// - The service approach is recommended for application logic that needs override support
        /// </para>
        /// </remarks>
        public static int[] GetMonthsForProperty(this Season season, Property property)
        {
            // This extension method intentionally returns only default months
            // Property-specific override logic is implemented in SeasonMonthMappingService
            // to maintain proper separation of concerns (extensions shouldn't have DB dependencies)
            return season.GetDefaultMonths();
        }
    }
}