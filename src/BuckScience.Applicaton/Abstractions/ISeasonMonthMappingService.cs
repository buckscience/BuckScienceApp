using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions;

/// <summary>
/// Interface for managing season-to-month mappings with property-specific overrides.
/// Provides access to both default mappings (from MonthsAttribute) and custom overrides (from database).
/// </summary>
public interface ISeasonMonthMappingService
{
    /// <summary>
    /// Gets the months for a season for a specific property.
    /// Checks for property-specific overrides first, then falls back to default months.
    /// </summary>
    /// <param name="season">The season to get months for.</param>
    /// <param name="property">The property to check for custom overrides.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Array of month integers (1-12) representing the months for the season and property.</returns>
    Task<int[]> GetMonthsForPropertyAsync(Season season, Property property, CancellationToken cancellationToken = default);

    /// <summary>
    /// Sets a custom month mapping override for a specific property and season.
    /// </summary>
    /// <param name="propertyId">The ID of the property.</param>
    /// <param name="season">The season to override.</param>
    /// <param name="months">Array of month integers (1-12) for the custom mapping.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The created or updated override entity.</returns>
    Task<PropertySeasonMonthsOverride> SetPropertySeasonOverrideAsync(int propertyId, Season season, int[] months, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a custom month mapping override for a specific property and season.
    /// The property will then use the default month mapping for the season.
    /// </summary>
    /// <param name="propertyId">The ID of the property.</param>
    /// <param name="season">The season to remove the override for.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>True if an override was removed, false if no override existed.</returns>
    Task<bool> RemovePropertySeasonOverrideAsync(int propertyId, Season season, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets all custom season month overrides for a specific property.
    /// </summary>
    /// <param name="propertyId">The ID of the property.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>Dictionary mapping seasons to their custom month arrays.</returns>
    Task<Dictionary<Season, int[]>> GetAllPropertyOverridesAsync(int propertyId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Determines the active season(s) for a given date and property using hybrid season-month mapping.
    /// Checks property-specific overrides first, then falls back to default season mappings.
    /// </summary>
    /// <param name="date">The date to determine the season for.</param>
    /// <param name="property">The property to check for custom season overrides.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>List of seasons that are active for the given date and property (ordered by season enum value).</returns>
    Task<IReadOnlyList<Season>> GetActiveSeasonsForDateAsync(DateTime date, Property property, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the primary active season for a given date and property using hybrid season-month mapping.
    /// If multiple seasons are active, returns the first one by enum ordering.
    /// </summary>
    /// <param name="date">The date to determine the season for.</param>
    /// <param name="property">The property to check for custom season overrides.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The primary season for the given date and property, or null if no season matches.</returns>
    Task<Season?> GetPrimarySeasonForDateAsync(DateTime date, Property property, CancellationToken cancellationToken = default);
}