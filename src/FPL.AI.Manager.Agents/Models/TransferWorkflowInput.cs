namespace FPL.AI.Manager.Agents.Models;

/// <summary>
/// Everything the transfer workflow needs to know about a manager's situation
/// before it starts recommending anything.
/// </summary>
/// <param name="TeamId">The manager's FPL team ID (the number in the URL of their team page).</param>
/// <param name="GameWeek">The gameweek the transfer is being planned for, between 1 and 38.</param>
/// <param name="Budget">Money in the bank, in millions (so <c>1.6</c> means £1.6m).</param>
/// <param name="TransfersAvailable">How many free transfers the manager has, normally 1 or 2.</param>
/// <param name="WillingToTakeHit">
/// Whether the manager is happy to make an extra transfer at the usual cost of 4 points.
/// </param>
public record TransferWorkflowInput(
    int TeamId,
    int GameWeek,
    double Budget,
    int TransfersAvailable,
    bool WillingToTakeHit
);

/// <summary>
/// The running state of a transfer decision as it passes through each agent in the workflow.
/// </summary>
/// <remarks>
/// Each agent fills in its own piece and leaves the rest alone, so by the end you have the
/// full paper trail: why a player was picked to sell, how much money there was to spend,
/// who the alternatives were, and how their fixtures looked. Any field can still be
/// <see langword="null"/> if the workflow hasn't reached that step yet.
/// </remarks>
/// <param name="Input">The manager's original request.</param>
/// <param name="SquadAnalysis">The squad analyser's verdict on who should be sold.</param>
/// <param name="BudgetAnalysis">How much money is available for the replacement.</param>
/// <param name="Candidates">The shortlist of possible replacements.</param>
/// <param name="FixtureAnalysis">How kind or tough each candidate's upcoming fixtures are.</param>
/// <param name="FinalRecommendation">The final sell-and-buy call.</param>
public record TransferWorkflowPayload(
    TransferWorkflowInput Input,
    string? SquadAnalysis,
    string? BudgetAnalysis,
    string? Candidates,
    string? FixtureAnalysis,
    string? FinalRecommendation
);
