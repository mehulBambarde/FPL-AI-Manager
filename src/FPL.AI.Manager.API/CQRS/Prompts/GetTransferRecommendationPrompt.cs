using FPL.AI.Manager.API.Responses;
using MediatR;

namespace FPL.AI.Manager.API.CQRS.Prompts;

public record GetTransferRecommendationPrompt(
    int TeamId,
    int GameWeek,
    double Budget,
    int TransfersAvailable,
    bool WillingToTakeHit)
    : IRequest<TransferRecommendationResponse>;
