using FPL.AI.Manager.API.CQRS.Commands;
using FPL.AI.Manager.API.CQRS.Prompts;
using FPL.AI.Manager.API.Requests;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;

namespace FPL.AI.Manager.API.Controllers;

[ApiController]
[Route("api/transfer")]
public class TransferController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IServiceProvider _serviceProvider;


    public TransferController(IMediator mediator, IServiceProvider serviceProvider)
    {
        _mediator = mediator;
        _serviceProvider = serviceProvider;
    }
    [HttpPost("recommend")]
    public async Task<IActionResult> Recommend(
        [FromBody] TransferRecommendationRequest request,
        CancellationToken ct)
    {
        var result = await _mediator.Send(new GetTransferRecommendationPrompt(
            request.TeamId,
            request.GameWeek,
            request.Budget,
            request.TransfersAvailable,
            request.WillingToTakeHit), ct);

        return Ok(result);
    }

    [HttpPost("ingest")]
    public IActionResult Ingest([FromBody] IngestRequest request)
    {
        RunIngestionInBackground(request);
        return Accepted(new { Message = "Ingestion started in background" });
    }

    private void RunIngestionInBackground(IngestRequest request)
    {
        var thread = new Thread(async () =>
        {
            using var scope = _serviceProvider.CreateScope();
            var mediator = scope.ServiceProvider.GetRequiredService<IMediator>();
            await mediator.Send(
                new IngestHistoricalDataCommand(request.CsvFilePath, request.Season),
                CancellationToken.None);
        });
        thread.IsBackground = true;
        thread.Start();
    }
}
