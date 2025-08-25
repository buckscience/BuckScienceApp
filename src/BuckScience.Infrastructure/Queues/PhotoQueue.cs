using Azure.Storage.Queues;
using BuckScience.Application.Photos.Messages;
using BuckScience.Infrastructure.Options;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace BuckScience.Infrastructure.Queues;

/// <summary>
/// Service for sending photo ingest messages to Azure Storage Queue
/// </summary>
public interface IPhotoQueue
{
    Task SendPhotoIngestMessageAsync(PhotoIngestMessage message, CancellationToken cancellationToken = default);
}

public class PhotoQueue : IPhotoQueue
{
    private readonly QueueClient _queueClient;

    public PhotoQueue(IOptions<QueueOptions> options)
    {
        var queueOptions = options.Value;
        _queueClient = new QueueClient(queueOptions.ConnectionString, queueOptions.Name);
    }

    public async Task SendPhotoIngestMessageAsync(PhotoIngestMessage message, CancellationToken cancellationToken = default)
    {
        // Ensure queue exists
        await _queueClient.CreateIfNotExistsAsync(cancellationToken: cancellationToken);

        // Serialize message to JSON
        var messageJson = JsonSerializer.Serialize(message);

        // Send message to queue
        await _queueClient.SendMessageAsync(messageJson, cancellationToken);
    }
}