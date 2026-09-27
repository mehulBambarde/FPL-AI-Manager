using FPL.AI.Manager.Agents.Models;
using FPL.AI.Manager.Agents.Workflows;
using FPL.AI.Manager.API.Responses;
using MediatR;

namespace FPL.AI.Manager.API.CQRS.Prompts;

public class GetTransferRecommendationPromptHandler
    : IRequestHandler<GetTransferRecommendationPrompt, TransferRecommendationResponse>
{
    private readonly TransferWorkflow _workflow;

    public GetTransferRecommendationPromptHandler(TransferWorkflow workflow)
    {
        _workflow = workflow;
    }

    public async Task<TransferRecommendationResponse> Handle(
        GetTransferRecommendationPrompt request, CancellationToken ct)
    {
        var input = new TransferWorkflowInput(
            request.TeamId,
            request.GameWeek,
            request.Budget,
            request.TransfersAvailable,
            request.WillingToTakeHit);

        var recommendation = await _workflow.RunAsync(input, ct);

        return new TransferRecommendationResponse(
            recommendation,
            DateTime.UtcNow);
    }
}
