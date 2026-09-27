namespace FPL.AI.Manager.Console;

public record TransferWorkflowInput(
    int TeamId,
    int GameWeek,
    double Budget,
    int TransfersAvailable,    // 1 or 2 free transfers
    bool WillingToTakeHit      // -4 points for extra transfer
);

public record TransferWorkflowPayload(
    TransferWorkflowInput Input,
    string? SquadAnalysis,
    string? BudgetAnalysis,
    string? Candidates,
    string? FixtureAnalysis,
    string? FinalRecommendation
);