using System.Runtime.CompilerServices;
using FPL.AI.Manager.Agents.Models;
using FPL.AI.Manager.Agents.Nodes;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

namespace FPL.AI.Manager.Agents.Workflows;

/// <summary>
/// Runs the five transfer agents one after another to turn a manager's situation into a
/// single "sell X, buy Y" recommendation.
/// </summary>
/// <remarks>
/// <para>The agents run in this order, each one reading everything the previous ones said:</para>
/// <list type="number">
///   <item><description><c>SquadAnalyser</c> picks the player to sell.</description></item>
///   <item><description><c>BudgetCalculator</c> works out the money available.</description></item>
///   <item><description><c>ReplacementFinder</c> shortlists three replacements.</description></item>
///   <item><description><c>FixtureAnalyst</c> scores their upcoming fixtures.</description></item>
///   <item><description><c>DecisionMaker</c> makes the final call.</description></item>
/// </list>
/// <para>
/// A fresh workflow is built for every run, so it's safe to call <see cref="RunStreamingAsync"/>
/// for several managers at the same time.
/// </para>
/// </remarks>
public sealed class TransferWorkflow
{
    private readonly FplAgentNodes _nodes;

    /// <summary>
    /// Creates the workflow on top of the given agent factory.
    /// </summary>
    /// <param name="nodes">Where the five agents come from.</param>
    public TransferWorkflow(FplAgentNodes nodes)
    {
        _nodes = nodes;
    }

    /// <summary>
    /// Wires the five agents together into a sequential workflow.
    /// </summary>
    /// <remarks>
    /// You only need this if you want to drive the workflow yourself. Most callers should
    /// just use <see cref="RunStreamingAsync"/>.
    /// </remarks>
    /// <returns>A workflow ready to be handed to <see cref="InProcessExecution"/>.</returns>
    public Workflow Build() =>
        AgentWorkflowBuilder.BuildSequential(
            _nodes.CreateSquadAnalyserNode(),
            _nodes.CreateBudgetCalculatorNode(),
            _nodes.CreateReplacementFinderNode(),
            _nodes.CreateFixtureAnalystNode(),
            _nodes.CreateDecisionNode());

    /// <summary>
    /// Kicks off the workflow for a manager and streams back everything that happens as it
    /// happens.
    /// </summary>
    /// <remarks>
    /// <para>The events you'll usually care about are:</para>
    /// <list type="bullet">
    ///   <item><description>
    ///     <see cref="AgentResponseUpdateEvent"/>: a chunk of text from whichever agent is
    ///     talking. Check <c>ExecutorId</c> to see which one.
    ///   </description></item>
    ///   <item><description><see cref="ExecutorCompletedEvent"/>: an agent has finished its step.</description></item>
    ///   <item><description><see cref="WorkflowOutputEvent"/>: the workflow's final output.</description></item>
    ///   <item><description><see cref="WorkflowErrorEvent"/>: something went wrong along the way.</description></item>
    /// </list>
    /// <para>
    /// Stop enumerating, or cancel the token, and the run is shut down and cleaned up for you.
    /// </para>
    /// </remarks>
    /// <param name="input">The manager's team, gameweek, budget and transfer situation.</param>
    /// <param name="cancellationToken">Cancels the run, including any agent or tool call in flight.</param>
    /// <returns>The workflow events, in the order they were raised.</returns>
    public async IAsyncEnumerable<WorkflowEvent> RunStreamingAsync(
        TransferWorkflowInput input,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        List<ChatMessage> messages = [new(ChatRole.User, BuildPrompt(input))];

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
            Build(), messages, cancellationToken: cancellationToken);

        // Agents in a workflow sit and wait until they're handed a turn token, so nothing
        // actually happens until this is sent. emitEvents: true is what gets us the
        // token-by-token AgentResponseUpdateEvents.
        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        await foreach (WorkflowEvent evt in run.WatchStreamAsync(cancellationToken))
        {
            yield return evt;
        }
    }

    /// <summary>
    /// Runs the whole workflow for a manager and hands back just the final recommendation.
    /// </summary>
    /// <remarks>
    /// Use this when you only care about the answer, like a plain request/response API
    /// endpoint. If you want to show the agents thinking as they go, use
    /// <see cref="RunStreamingAsync"/> instead.
    /// </remarks>
    /// <param name="input">The manager's team, gameweek, budget and transfer situation.</param>
    /// <param name="cancellationToken">Cancels the run, including any agent or tool call in flight.</param>
    /// <returns>What the last agent in the chain (the <c>DecisionMaker</c>) said.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the workflow reports an error, or finishes without any agent saying anything.
    /// </exception>
    public async Task<string> RunAsync(TransferWorkflowInput input, CancellationToken cancellationToken = default)
    {
        // The agents stream their replies in small chunks, so we stitch together the chunks
        // for whichever agent spoke most recently. Once the run ends, that's the decision maker.
        var lastAgentReply = new System.Text.StringBuilder();
        string? lastExecutorId = null;

        await foreach (WorkflowEvent evt in RunStreamingAsync(input, cancellationToken))
        {
            switch (evt)
            {
                case AgentResponseUpdateEvent update:
                    if (update.ExecutorId != lastExecutorId)
                    {
                        lastExecutorId = update.ExecutorId;
                        lastAgentReply.Clear();
                    }
                    lastAgentReply.Append(update.Update.Text);
                    break;

                case WorkflowErrorEvent error:
                    throw new InvalidOperationException(
                        "The transfer workflow failed before it could make a recommendation.",
                        error.Exception);
            }
        }

        if (lastAgentReply.Length == 0)
            throw new InvalidOperationException("The transfer workflow finished without producing a recommendation.");

        return lastAgentReply.ToString();
    }

    /// <summary>
    /// Turns the manager's situation into the opening message the first agent sees.
    /// </summary>
    /// <param name="input">The manager's team, gameweek, budget and transfer situation.</param>
    /// <returns>A plain-English request describing the transfer the manager wants help with.</returns>
    public static string BuildPrompt(TransferWorkflowInput input) =>
        $"Help me make a transfer decision. " +
        $"Team ID: {input.TeamId}, " +
        $"Gameweek: {input.GameWeek}, " +
        $"Budget: £{input.Budget}m, " +
        $"Free transfers: {input.TransfersAvailable}, " +
        $"Willing to take hit: {input.WillingToTakeHit}";
}
