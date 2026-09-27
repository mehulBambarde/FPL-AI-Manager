using MediatR;

namespace FPL.AI.Manager.API.CQRS.Commands;

public record IngestHistoricalDataCommand(string CsvFilePath, string Season)
    : IRequest<int>;
