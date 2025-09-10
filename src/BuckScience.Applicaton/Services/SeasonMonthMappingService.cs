using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Services
{
    /// <summary>
    /// Service for managing season-to-month mappings with property-specific overrides.
    /// Provides access to both default mappings (from MonthsAttribute) and custom overrides (from database).
    /// </summary>
    public class SeasonMonthMappingService
    {
        private readonly IAppDbContext _dbContext;

        public SeasonMonthMappingService(IAppDbContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Gets the months for a season for a specific property.
        /// Checks for property-specific overrides first, then falls back to default months.
        /// </summary>
        /// <param name="season">The season to get months for.</param>
        /// <param name="property">The property to check for custom overrides.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Array of month integers (1-12) representing the months for the season and property.</returns>
        public async Task<int[]> GetMonthsForPropertyAsync(Season season, Property property, CancellationToken cancellationToken = default)
        {
            if (property == null)
                return season.GetDefaultMonths();

            // Check for property-specific override
            var override_ = await _dbContext.PropertySeasonMonthsOverrides
                .FirstOrDefaultAsync(o => o.PropertyId == property.Id && o.Season == season, cancellationToken);

            if (override_ != null)
            {
                var overrideMonths = override_.GetMonths();
                if (overrideMonths != null && overrideMonths.Length > 0)
                {
                    return overrideMonths;
                }
            }

            // Fall back to default months
            return season.GetDefaultMonths();
        }

        /// <summary>
        /// Sets a custom month mapping override for a specific property and season.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="season">The season to override.</param>
        /// <param name="months">Array of month integers (1-12) for the custom mapping.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>The created or updated override entity.</returns>
        public async Task<PropertySeasonMonthsOverride> SetPropertySeasonOverrideAsync(int propertyId, Season season, int[] months, CancellationToken cancellationToken = default)
        {
            // Check if override already exists
            var existingOverride = await _dbContext.PropertySeasonMonthsOverrides
                .FirstOrDefaultAsync(o => o.PropertyId == propertyId && o.Season == season, cancellationToken);

            if (existingOverride != null)
            {
                // Update existing override
                existingOverride.SetMonths(months);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return existingOverride;
            }
            else
            {
                // Create new override
                var newOverride = new PropertySeasonMonthsOverride(propertyId, season, months);
                _dbContext.PropertySeasonMonthsOverrides.Add(newOverride);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return newOverride;
            }
        }

        /// <summary>
        /// Removes a custom month mapping override for a specific property and season.
        /// The property will then use the default month mapping for the season.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="season">The season to remove the override for.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>True if an override was removed, false if no override existed.</returns>
        public async Task<bool> RemovePropertySeasonOverrideAsync(int propertyId, Season season, CancellationToken cancellationToken = default)
        {
            var existingOverride = await _dbContext.PropertySeasonMonthsOverrides
                .FirstOrDefaultAsync(o => o.PropertyId == propertyId && o.Season == season, cancellationToken);

            if (existingOverride != null)
            {
                _dbContext.PropertySeasonMonthsOverrides.Remove(existingOverride);
                await _dbContext.SaveChangesAsync(cancellationToken);
                return true;
            }

            return false;
        }

        /// <summary>
        /// Gets all custom season month overrides for a specific property.
        /// </summary>
        /// <param name="propertyId">The ID of the property.</param>
        /// <param name="cancellationToken">Cancellation token for async operation.</param>
        /// <returns>Dictionary mapping seasons to their custom month arrays.</returns>
        public async Task<Dictionary<Season, int[]>> GetAllPropertyOverridesAsync(int propertyId, CancellationToken cancellationToken = default)
        {
            var overrides = await _dbContext.PropertySeasonMonthsOverrides
                .Where(o => o.PropertyId == propertyId)
                .ToListAsync(cancellationToken);

            var result = new Dictionary<Season, int[]>();
            
            foreach (var override_ in overrides)
            {
                var months = override_.GetMonths();
                if (months != null && months.Length > 0)
                {
                    result[override_.Season] = months;
                }
            }

            return result;
        }
    }
}