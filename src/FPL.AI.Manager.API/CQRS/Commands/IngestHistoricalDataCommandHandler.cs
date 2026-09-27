using FPL.AI.Manager.RAG.Services;
using MediatR;

namespace FPL.AI.Manager.API.CQRS.Commands;

public class IngestHistoricalDataCommandHandler
    : IRequestHandler<IngestHistoricalDataCommand, int>
{
    private readonly IngestionService _ingestionService;

    public IngestHistoricalDataCommandHandler(IngestionService ingestionService)
    {
        _ingestionService = ingestionService;
    }

    public async Task<int> Handle(
        IngestHistoricalDataCommand request, CancellationToken ct)
    {
        return await _ingestionService.IngestAsync(request.CsvFilePath, request.Season, ct);
    }
}
