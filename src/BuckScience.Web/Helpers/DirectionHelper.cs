using System.ComponentModel.DataAnnotations;

namespace BuckScience.Web.Helpers;

public static class DirectionHelper
{
    public enum CompassDirection
    {
        [Display(Name = "North")]
        N,
        [Display(Name = "Northeast")]
        NE,
        [Display(Name = "East")]
        E,
        [Display(Name = "Southeast")]
        SE,
        [Display(Name = "South")]
        S,
        [Display(Name = "Southwest")]
        SW,
        [Display(Name = "West")]
        W,
        [Display(Name = "Northwest")]
        NW
    }

    private static readonly Dictionary<CompassDirection, float> DirectionToDegrees = new()
    {
        { CompassDirection.N, 0f },
        { CompassDirection.NE, 45f },
        { CompassDirection.E, 90f },
        { CompassDirection.SE, 135f },
        { CompassDirection.S, 180f },
        { CompassDirection.SW, 225f },
        { CompassDirection.W, 270f },
        { CompassDirection.NW, 315f }
    };

    private static readonly Dictionary<float, CompassDirection> DegreesToDirection = 
        DirectionToDegrees.ToDictionary(kvp => kvp.Value, kvp => kvp.Key);

    public static float ToFloat(CompassDirection direction)
    {
        return DirectionToDegrees[direction];
    }

    public static CompassDirection FromFloat(float degrees)
    {
        // Normalize the degrees to 0-360 range
        degrees = degrees % 360;
        if (degrees < 0) degrees += 360;

        // Find the closest compass direction
        var closest = DirectionToDegrees
            .OrderBy(kvp => Math.Abs(kvp.Value - degrees))
            .First();

        return closest.Key;
    }

    public static string GetDisplayName(CompassDirection direction)
    {
        var field = direction.GetType().GetField(direction.ToString());
        var attribute = field?.GetCustomAttributes(typeof(DisplayAttribute), false)
                              .FirstOrDefault() as DisplayAttribute;
        return attribute?.Name ?? direction.ToString();
    }

    public static IEnumerable<(CompassDirection Direction, string DisplayName, float Degrees)> GetAllDirections()
    {
        return Enum.GetValues<CompassDirection>()
                   .Select(d => (d, GetDisplayName(d), ToFloat(d)));
    }
}