using System;
using System.Text.Json;
using BuckScience.Domain.Enums;

namespace BuckScience.Domain.Entities
{
    /// <summary>
    /// Represents a custom season-to-month mapping override for a specific property.
    /// Allows properties to define custom month ranges for seasons, overriding the default mappings.
    /// </summary>
    public class PropertySeasonMonthsOverride
    {
        protected PropertySeasonMonthsOverride() { } // EF constructor

        /// <summary>
        /// Initializes a new instance of the PropertySeasonMonthsOverride class.
        /// </summary>
        /// <param name="propertyId">The ID of the property this override applies to.</param>
        /// <param name="season">The season being overridden.</param>
        /// <param name="months">Array of month integers (1-12) representing the custom months for this season.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when propertyId is invalid.</exception>
        /// <exception cref="ArgumentNullException">Thrown when months is null.</exception>
        /// <exception cref="ArgumentException">Thrown when months is empty or contains invalid values.</exception>
        public PropertySeasonMonthsOverride(int propertyId, Season season, int[] months)
        {
            if (propertyId <= 0)
                throw new ArgumentOutOfRangeException(nameof(propertyId), "Property ID must be greater than 0.");

            if (months == null)
                throw new ArgumentNullException(nameof(months));

            if (months.Length == 0)
                throw new ArgumentException("At least one month must be specified.", nameof(months));

            SetPropertyId(propertyId);
            Season = season;
            SetMonthsInternal(months); // Use internal method that doesn't allow null/empty
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Gets the unique identifier for this override.
        /// </summary>
        public int Id { get; private set; }

        /// <summary>
        /// Gets the ID of the property this override applies to.
        /// </summary>
        public int PropertyId { get; private set; }

        /// <summary>
        /// Gets the season being overridden.
        /// </summary>
        public Season Season { get; private set; }

        /// <summary>
        /// Gets the custom months as a JSON-serialized string for database storage.
        /// </summary>
        public string MonthsJson { get; private set; } = string.Empty;

        /// <summary>
        /// Gets the date and time when this override was created.
        /// </summary>
        public DateTime CreatedAt { get; private set; }

        /// <summary>
        /// Gets the date and time when this override was last updated.
        /// </summary>
        public DateTime UpdatedAt { get; private set; }

        /// <summary>
        /// Navigation property to the associated property.
        /// </summary>
        public virtual Property Property { get; private set; } = default!;

        /// <summary>
        /// Gets the custom months for this season override.
        /// </summary>
        /// <returns>Array of month integers (1-12), or null if no months are set.</returns>
        public int[]? GetMonths()
        {
            if (string.IsNullOrWhiteSpace(MonthsJson))
                return null;

            try
            {
                return JsonSerializer.Deserialize<int[]>(MonthsJson);
            }
            catch (JsonException)
            {
                return null;
            }
        }

        /// <summary>
        /// Sets the custom months for this season override.
        /// </summary>
        /// <param name="months">Array of month integers (1-12), or null to clear.</param>
        /// <exception cref="ArgumentException">Thrown when months contains invalid values.</exception>
        public void SetMonths(int[]? months)
        {
            if (months == null || months.Length == 0)
            {
                MonthsJson = string.Empty;
            }
            else
            {
                SetMonthsInternal(months);
            }

            UpdatedAt = DateTime.UtcNow;
        }

        /// <summary>
        /// Internal method to set months with validation. Does not allow null or empty arrays.
        /// </summary>
        /// <param name="months">Array of month integers (1-12).</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when months contains invalid values.</exception>
        private void SetMonthsInternal(int[] months)
        {
            // Validate month values
            foreach (var month in months)
            {
                if (month < 1 || month > 12)
                    throw new ArgumentOutOfRangeException(nameof(months), $"Month value {month} is invalid. Must be between 1 and 12.");
            }

            MonthsJson = JsonSerializer.Serialize(months);
        }

        /// <summary>
        /// Sets the property ID for this override.
        /// </summary>
        /// <param name="propertyId">The property ID.</param>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when propertyId is invalid.</exception>
        private void SetPropertyId(int propertyId)
        {
            if (propertyId <= 0)
                throw new ArgumentOutOfRangeException(nameof(propertyId), "Property ID must be greater than 0.");

            PropertyId = propertyId;
        }
    }
}