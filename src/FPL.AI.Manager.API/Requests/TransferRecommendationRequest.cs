namespace FPL.AI.Manager.API.Requests;

public record TransferRecommendationRequest(
    int TeamId,
    int GameWeek,
    double Budget,
    int TransfersAvailable,
    bool WillingToTakeHit);
