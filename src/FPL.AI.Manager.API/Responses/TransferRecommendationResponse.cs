namespace FPL.AI.Manager.API.Responses;

public record TransferRecommendationResponse(
    string Recommendation,
    DateTime GeneratedAt);
