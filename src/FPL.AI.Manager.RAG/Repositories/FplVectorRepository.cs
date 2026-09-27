using FPL.AI.Manager.RAG.Models;
using FPL.AI.Manager.RAG.Services;

namespace FPL.AI.Manager.RAG.Repositories;

public class FplVectorRepository
{
    private readonly EmbeddingService _embeddingService;
    private readonly QdrantService _qdrantService;
    private readonly IngestionService _ingestionService;

    public FplVectorRepository(
        EmbeddingService embeddingService,
        QdrantService qdrantService,
        IngestionService ingestionService)
    {
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
        _ingestionService = ingestionService;
    }

    public async Task<int> IngestAsync(
        string csvFilePath,
        string season,
        CancellationToken ct = default)
    {
        return await _ingestionService.IngestAsync(csvFilePath, season, ct);
    }

    public async Task<List<SearchResult>> SearchAsync(
        string query,
        int topN = 5,
        CancellationToken ct = default)
    {
        var queryVector = await _embeddingService.GenerateEmbeddingAsync(query, ct);
        return await _qdrantService.SearchAsync(queryVector, topN, ct);
    }
}