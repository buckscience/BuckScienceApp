namespace BuckScience.Web.ViewModels;

public sealed class PropertyListItemVm
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public string TimeZone { get; init; } = "UTC";
    public int DayHour { get; init; }
    public int NightHour { get; init; }
}