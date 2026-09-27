using CsvHelper;
using CsvHelper.Configuration;
using FPL.AI.Manager.RAG.Models;
using System.Globalization;

namespace FPL.AI.Manager.RAG.Services;

public class IngestionService
{
    private readonly EmbeddingService _embeddingService;
    private readonly QdrantService _qdrantService;
    private const int BatchSize = 100;

    public IngestionService(EmbeddingService embeddingService, QdrantService qdrantService)
    {
        _embeddingService = embeddingService;
        _qdrantService = qdrantService;
    }

    public async Task<int> IngestAsync(string csvFilePath, string season, CancellationToken ct = default)
    {
        await _qdrantService.EnsureCollectionExistsAsync(ct);

        var records = ReadCsv(csvFilePath, season);
        var totalIngested = 0;
        var batch = new List<(Guid, float[], Dictionary<string, object>)>();

        foreach (var record in records)
        {
            if (ct.IsCancellationRequested) break;

            var text = record.ToEmbeddingText();
            var vector = await _embeddingService.GenerateEmbeddingAsync(text, ct);

            batch.Add((
                GenerateDeterministicId(record.Name, record.Season, record.Round),
                vector,
                new Dictionary<string, object>
                {
                    { "text", text },
                    { "player", record.Name },
                    { "season", record.Season },
                    { "round", record.Round.ToString() },
                    { "points", record.TotalPoints.ToString() },
                    { "goals", record.GoalsScored.ToString() },
                    { "assists", record.Assists.ToString() }
                }
            ));

            if (batch.Count >= BatchSize)
            {
                await _qdrantService.UpsertPointsAsync(batch, ct);
                totalIngested += batch.Count;
                Console.WriteLine($"Ingested {totalIngested} records...");
                batch.Clear();

                // Small delay to avoid embedding API rate limits
                await Task.Delay(500, ct);
            }
        }

        // Ingest remaining records
        if (batch.Count > 0)
        {
            await _qdrantService.UpsertPointsAsync(batch, ct);
            totalIngested += batch.Count;
        }

        Console.WriteLine($"Ingestion complete. Total records: {totalIngested}");
        return totalIngested;
    }

    private List<FplPlayerRecord> ReadCsv(string csvFilePath, string season)
    {
        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            MissingFieldFound = null,
            HeaderValidated = null
        };

        using var reader = new StreamReader(csvFilePath);
        using var csv = new CsvReader(reader, config);

        csv.Context.RegisterClassMap<FplPlayerRecordMap>();

        var records = csv.GetRecords<FplPlayerRecord>().ToList();

        // Set season on each record since it is not in the CSV
        records.ForEach(r => r.Season = season);

        return records;
    }
    
    private static Guid GenerateDeterministicId(string playerName, string season, int round)
    {
        var key = $"{playerName}-{season}-{round}".ToLowerInvariant();
        var hash = System.Security.Cryptography.MD5.HashData(
            System.Text.Encoding.UTF8.GetBytes(key));
        return new Guid(hash);
    }
}