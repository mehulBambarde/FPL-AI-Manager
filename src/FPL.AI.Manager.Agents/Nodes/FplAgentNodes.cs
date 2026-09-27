using Azure.AI.Projects;
using Azure.Identity;
using FPL.AI.Manager.Agents.Configuration;
using FPL.AI.Manager.Agents.Tools;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Options;

namespace FPL.AI.Manager.Agents.Nodes;

/// <summary>
/// Builds the individual agents ("nodes") that make up the transfer workflow.
/// </summary>
/// <remarks>
/// <para>
/// Each agent has one narrow job and only gets the FPL tools it needs for that job. Keeping
/// them focused like this makes their answers sharper and stops them wandering off to call
/// endpoints that have nothing to do with their step.
/// </para>
/// <para>
/// All agents share a single <see cref="AIProjectClient"/> signed in with
/// <see cref="AzureCliCredential"/>, so whatever account you're logged into with <c>az login</c>
/// is the one that talks to Foundry.
/// </para>
/// </remarks>
public sealed class FplAgentNodes
{
    private readonly AIProjectClient _projectClient;
    private readonly FplService _fplService;
    private readonly string _model;

    /// <summary>
    /// Sets up the factory with the Foundry connection details and the FPL tools.
    /// </summary>
    /// <param name="projectClient">
    /// The Foundry project client, signed in with <see cref="AzureCliCredential"/>. Share one
    /// instance across the app; use <see cref="CreateProjectClient"/> to build it.
    /// </param>
    /// <param name="options">Which model deployment the agents should run on.</param>
    /// <param name="fplService">The FPL API wrapper whose methods are handed to agents as tools.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the deployment name hasn't been configured.
    /// </exception>
    public FplAgentNodes(AIProjectClient projectClient, IOptions<FplAgentOptions> options, FplService fplService)
    {
        var deploymentName = options.Value.DeploymentName;

        if (string.IsNullOrWhiteSpace(deploymentName))
            throw new InvalidOperationException(
                $"{FplAgentOptions.SectionName}:{nameof(FplAgentOptions.DeploymentName)} is not configured.");

        _projectClient = projectClient;
        _fplService = fplService;
        _model = deploymentName;
    }

    /// <summary>
    /// Creates the Foundry project client every agent talks through, signed in with
    /// <see cref="AzureCliCredential"/>.
    /// </summary>
    /// <remarks>
    /// Build this once and reuse it. Each new credential has to ask the Azure CLI for a fresh
    /// token, which takes a noticeable moment.
    /// </remarks>
    /// <param name="options">Where the Foundry project lives.</param>
    /// <returns>A ready-to-use project client.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the endpoint hasn't been configured.
    /// </exception>
    public static AIProjectClient CreateProjectClient(FplAgentOptions options)
    {
        if (string.IsNullOrWhiteSpace(options.Endpoint))
            throw new InvalidOperationException(
                $"{FplAgentOptions.SectionName}:{nameof(FplAgentOptions.Endpoint)} is not configured.");

        return new AIProjectClient(new Uri(options.Endpoint), new AzureCliCredential());
    }

    /// <summary>
    /// Step 1: looks over the manager's squad and picks out the weakest player to sell,
    /// weighing up form, fixtures and value for money.
    /// </summary>
    /// <returns>The squad analyser agent.</returns>
    public AIAgent CreateSquadAnalyserNode() =>
        CreateAgent(
            name: "SquadAnalyser",
            instructions: """
                          You are an FPL squad analyst. Given a team ID and gameweek,
                          analyse the squad and identify the weakest performing player
                          who should be sold. Consider form, upcoming fixtures, and value for money.
                          Be concise and specific in your recommendation.
                          """,
            AIFunctionFactory.Create(_fplService.GetSquadAsync),
            AIFunctionFactory.Create(_fplService.GetTopOwnedPlayersAsync));

    /// <summary>
    /// Step 2: works out exactly how much money is available for a replacement once the
    /// outgoing player has been sold.
    /// </summary>
    /// <returns>The budget calculator agent.</returns>
    public AIAgent CreateBudgetCalculatorNode() =>
        CreateAgent(
            name: "BudgetCalculator",
            instructions: """
                          You are an FPL budget analyst. Given information about a player
                          to sell and the current bank balance, calculate the exact budget
                          available for a replacement. State the available budget clearly.
                          """,
            AIFunctionFactory.Create(_fplService.GetPlayersAsync));

    /// <summary>
    /// Step 3: comes up with three realistic replacements that fit the position and the budget.
    /// </summary>
    /// <returns>The replacement finder agent.</returns>
    public AIAgent CreateReplacementFinderNode() =>
        CreateAgent(
            name: "ReplacementFinder",
            instructions: """
                          You are an FPL transfer specialist. Given a position and budget from the previous analysis,
                          find the top 3 replacement candidates. Use GetTopOwnedPlayersAsync to find
                          popular options. Consider current form, total points, and price.
                          Return exactly 3 candidates with their key stats.
                          """,
            AIFunctionFactory.Create(_fplService.GetTopOwnedPlayersAsync));

    /// <summary>
    /// Step 4: scores each candidate's next five fixtures out of 10, where 10 is the kindest run.
    /// </summary>
    /// <returns>The fixture analyst agent.</returns>
    public AIAgent CreateFixtureAnalystNode() =>
        CreateAgent(
            name: "FixtureAnalyst",
            instructions: """
                          You are an FPL fixture analyst. Given the replacement candidates from the previous analysis,
                          analyse their next 5 fixtures using GetFixturesAsync and score each player's
                          fixture difficulty from 1 to 10 where 10 is the easiest run.
                          Return a clear score and reasoning for each candidate.
                          """,
            AIFunctionFactory.Create(_fplService.GetFixturesAsync));

    /// <summary>
    /// Step 5: pulls everything together and makes the final call on who goes out and who
    /// comes in, with three reasons to back it up.
    /// </summary>
    /// <returns>The decision maker agent.</returns>
    public AIAgent CreateDecisionNode() =>
        CreateAgent(
            name: "DecisionMaker",
            instructions: """
                          You are an FPL transfer decision expert. Given squad analysis,
                          budget, replacement candidates, and fixture scores, make a clear
                          final transfer recommendation. State who to sell, who to bring in,
                          and give 3 bullet point reasons for the decision. Be decisive.
                          """,
            AIFunctionFactory.Create(_fplService.GetPlayersAsync),
            AIFunctionFactory.Create(_fplService.GetFixturesAsync));

    private AIAgent CreateAgent(string name, string instructions, params AIFunction[] tools) =>
        _projectClient.AsAIAgent(
            model: _model,
            name: name,
            instructions: instructions,
            tools: tools);
}
