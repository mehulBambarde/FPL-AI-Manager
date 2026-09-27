using Qdrant.Client;
using Qdrant.Client.Grpc;
using FPL.AI.Manager.RAG.Configuration;
using FPL.AI.Manager.RAG.Models;
using Microsoft.Extensions.Options;

namespace FPL.AI.Manager.RAG.Services;

public class QdrantService
{
    private readonly QdrantClient _client;
    private readonly QdrantOptions _options;

    public QdrantService(IOptions<QdrantOptions> options)
    {
        _options = options.Value;
        _client = new QdrantClient(_options.Host, _options.Port);
    }

    public async Task EnsureCollectionExistsAsync(CancellationToken ct = default)
    {
        var collections = await _client.ListCollectionsAsync(ct);
        
        if (collections.Any(c => c == _options.CollectionName))
        {
            Console.WriteLine($"Collection '{_options.CollectionName}' already exists.");
            return;
        }

        await _client.CreateCollectionAsync(
            _options.CollectionName,
            new VectorParams
            {
                Size = (ulong)_options.VectorSize,
                Distance = Distance.Cosine
            },
            cancellationToken: ct);
        Console.WriteLine($"Collection '{_options.CollectionName}' created.");
    }

    public async Task UpsertPointsAsync(
        List<(Guid Id, float[] Vector, Dictionary<string, object> Payload)> points,
        CancellationToken ct = default)
    {
        var qdrantPoints = points.Select(p => new PointStruct
        {
            Id = new PointId { Uuid = p.Id.ToString() },
            Vectors = p.Vector,
            Payload =
            {
                p.Payload.ToDictionary(
                    kvp => kvp.Key,
                    kvp => new Value { StringValue = kvp.Value.ToString() ?? string.Empty })
            }
        }).ToList();

        await _client.UpsertAsync(_options.CollectionName, qdrantPoints, cancellationToken: ct);
    }

    public async Task<List<SearchResult>> SearchAsync(
        float[] queryVector,
        int topN = 5,
        CancellationToken ct = default)
    {
        var results = await _client.SearchAsync(
            _options.CollectionName,
            queryVector,
            limit: (ulong)topN,
            cancellationToken: ct);

        return results.Select(r => new SearchResult
        {
            Text = r.Payload.TryGetValue("text", out var text) 
                ? text.StringValue 
                : string.Empty,
            Score = r.Score
        }).ToList();
    }
}