namespace FPL.AI.Manager.Agents.Configuration;

/// <summary>
/// Settings the agents need to talk to Azure AI Foundry.
/// </summary>
/// <remarks>
/// These are usually bound from the <c>AzureOpenAI</c> section of <c>appsettings.json</c>,
/// the same section the console app reads. There are no keys in here on purpose:
/// authentication always goes through <see cref="Azure.Identity.AzureCliCredential"/>,
/// so make sure you've run <c>az login</c> before starting anything that uses these agents.
/// </remarks>
public sealed class FplAgentOptions
{
    /// <summary>
    /// The configuration section these options are read from.
    /// </summary>
    public const string SectionName = "AzureOpenAI";

    /// <summary>
    /// The Azure AI Foundry project endpoint, for example
    /// <c>https://my-resource.services.ai.azure.com/api/projects/my-project</c>.
    /// </summary>
    public string Endpoint { get; set; } = string.Empty;

    public string BaseEndpoint { get; set; }
    /// <summary>
    /// The name of the model deployment every agent runs on, such as <c>gpt-4.1-mini</c>.
    /// </summary>
    public string DeploymentName { get; set; } = string.Empty;
}
