// Add this temporarily to your console app Program.cs
using FPL.AI.Manager.RAG.Services;
using FPL.AI.Manager.RAG.Configuration;
using Microsoft.Extensions.Options;

var qdrantOptions = Options.Create(new QdrantOptions
{
    Host = "localhost",
    Port = 6334,
    CollectionName = "fpl_historical",
    VectorSize = 1536
});

var embeddingService = new EmbeddingService(
    qdrantOptions,
    "https://fpl-ai-manager-resource.cognitiveservices.azure.com/",
    "text-embedding-3-small");

var qdrantService = new QdrantService(qdrantOptions);
var ingestionService = new IngestionService(embeddingService, qdrantService);

Console.WriteLine("Starting ingestion...");
var count = await ingestionService.IngestAsync(
    "merged_gw_2024-25.csv",
    "2024-25",
    CancellationToken.None);

Console.WriteLine($"Done. Total: {count}");