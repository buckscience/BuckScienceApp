namespace BuckScience.Infrastructure.Options;

/// <summary>
/// Configuration options for Azure Storage Queue
/// </summary>
public class QueueOptions
{
    /// <summary>
    /// Configuration section name
    /// </summary>
    public const string SectionName = "Queue";

    /// <summary>
    /// Azure Storage connection string
    /// </summary>
    public string ConnectionString { get; set; } = string.Empty;

    /// <summary>
    /// Name of the queue for photo ingest messages
    /// </summary>
    public string Name { get; set; } = "photo-ingest";
}