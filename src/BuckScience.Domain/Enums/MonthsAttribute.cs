using System;

namespace BuckScience.Domain.Enums
{
    /// <summary>
    /// Attribute for defining default month mappings for Season enum values.
    /// Months are represented as integers (1-12, where 1=January, 12=December).
    /// </summary>
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false)]
    public class MonthsAttribute : Attribute
    {
        /// <summary>
        /// Gets the default months for this season.
        /// </summary>
        public int[] Months { get; }

        /// <summary>
        /// Initializes a new instance of the MonthsAttribute class.
        /// </summary>
        /// <param name="months">Array of month integers (1-12) representing the default months for the season.</param>
        /// <exception cref="ArgumentNullException">Thrown when months is null.</exception>
        /// <exception cref="ArgumentException">Thrown when months is empty or contains invalid month values.</exception>
        public MonthsAttribute(params int[] months)
        {
            if (months == null)
                throw new ArgumentNullException(nameof(months));
            
            if (months.Length == 0)
                throw new ArgumentException("At least one month must be specified.", nameof(months));

            foreach (var month in months)
            {
                if (month < 1 || month > 12)
                    throw new ArgumentOutOfRangeException(nameof(months), $"Month value {month} is invalid. Must be between 1 and 12.");
            }

            Months = months;
        }
    }
}