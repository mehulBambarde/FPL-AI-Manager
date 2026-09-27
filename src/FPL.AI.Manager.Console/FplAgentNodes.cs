using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

namespace FPL.AI.Manager.Console;

public static class FplAgentNodes
{
    private static AIAgent CreateAgent(string name, string instructions, string model, string baseUrl,
        params AIFunction[] tools)
    {
        return new AIProjectClient(
                new Uri(baseUrl),
                new AzureCliCredential())
            .AsAIAgent(
                model: model,
                name: name,
                instructions: instructions,
                tools: tools);
    }
  //
    // Node 1 — Analyses the user's squad and finds the weakest player to sell
    public static AIAgent CreateSquadAnalyserNode(string model, string baseUrl) =>
        CreateAgent(
            name: "SquadAnalyser",
            instructions: """
                          You are an FPL squad analyst. Given a team ID and gameweek, 
                          analyse the squad and identify the weakest performing player 
                          who should be sold. Consider form, upcoming fixtures, and value for money.
                          Be concise and specific in your recommendation.
                          """,
            model: model,
            baseUrl: baseUrl,
            [AIFunctionFactory.Create(FplService.GetSquadAsync),
                AIFunctionFactory.Create(FplService.GetTopOwnedPlayersAsync)]
        );

    // Node 2 — Calculates the available transfer budget
    public static AIAgent CreateBudgetCalculatorNode(string model, string baseUrl) =>
        CreateAgent(
            name: "BudgetCalculator",
            instructions: """
                          You are an FPL budget analyst. Given information about a player 
                          to sell and the current bank balance, calculate the exact budget 
                          available for a replacement. State the available budget clearly.
                          """,
            model: model,
            baseUrl: baseUrl,
            AIFunctionFactory.Create(FplService.GetAllPlayersAsync)
        );

    // Node 3 — Finds the best replacement candidates within budget
    public static AIAgent CreateReplacementFinderNode(string model, string baseUrl) =>
        CreateAgent(
            name: "ReplacementFinder",
            instructions: """
                          You are an FPL transfer specialist. Given a position and budget from the previous analysis, 
                          find the top 3 replacement candidates. Use GetTopOwnedPlayersAsync to find 
                          popular options. Consider current form, total points, and price. 
                          Return exactly 3 candidates with their key stats.
                          """,
            model: model,
            baseUrl: baseUrl,
            AIFunctionFactory.Create(FplService.GetTopOwnedPlayersAsync)
        );
    // Node 4 — Analyses upcoming fixtures for each candidate
    public static AIAgent CreateFixtureAnalystNode(string model, string baseUrl) =>
        CreateAgent(
            name: "FixtureAnalyst",
            instructions: """
                          You are an FPL fixture analyst. Given the replacement candidates from the previous analysis, 
                          analyse their next 5 fixtures using GetFixturesAsync and score each player's 
                          fixture difficulty from 1 to 10 where 10 is the easiest run. 
                          Return a clear score and reasoning for each candidate.
                          """,
            model: model,
            baseUrl: baseUrl,
            AIFunctionFactory.Create(FplService.GetFixturesAsync)
        );

    // Node 5 — Makes the final transfer decision
    public static AIAgent CreateDecisionNode(string model, string baseUrl) =>
        CreateAgent(
            name: "DecisionMaker",
            instructions: """
                          You are an FPL transfer decision expert. Given squad analysis, 
                          budget, replacement candidates, and fixture scores, make a clear 
                          final transfer recommendation. State who to sell, who to bring in, 
                          and give 3 bullet point reasons for the decision. Be decisive.
                          """,
            model: model,
            baseUrl: baseUrl,
            AIFunctionFactory.Create(FplService.GetAllPlayersAsync),
            AIFunctionFactory.Create(FplService.GetFixturesAsync)
        );
}