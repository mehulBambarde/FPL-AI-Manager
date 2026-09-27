using Azure.AI.Projects;
using FPL.AI.Manager.Agents.Configuration;
using FPL.AI.Manager.Agents.Nodes;
using FPL.AI.Manager.Agents.Tools;
using FPL.AI.Manager.Agents.Workflows;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FPL.AI.Manager.Agents.DependencyInjection;

/// <summary>
/// One-line setup for hosts (the Web API, the Function App) that want to use the FPL agents.
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers the FPL API client, the agent factory and the transfer workflow.
    /// </summary>
    /// <remarks>
    /// Settings are read from the <c>AzureOpenAI</c> section of configuration, which needs an
    /// <c>Endpoint</c> and a <c>DeploymentName</c>. After calling this you can inject
    /// <see cref="TransferWorkflow"/> wherever you need it.
    /// </remarks>
    /// <example>
    /// <code>
    /// builder.Services.AddFplAgents(builder.Configuration);
    /// </code>
    /// </example>
    /// <param name="services">The service collection to add to.</param>
    /// <param name="configuration">The app configuration holding the <c>AzureOpenAI</c> section.</param>
    /// <returns>The same service collection, so calls can be chained.</returns>
    public static IServiceCollection AddFplAgents(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<FplAgentOptions>(configuration.GetSection(FplAgentOptions.SectionName));

        services.AddHttpClient<FplService>(client =>
        {
            client.BaseAddress = new Uri(FplService.BaseAddress);
            client.DefaultRequestHeaders.Add("User-Agent", "FPL-AI-Manager/1.0");
        });

        // One project client (and so one AzureCliCredential) for the whole app, so the
        // Azure CLI is only asked for a token when the cached one runs out.
        services.AddSingleton<AIProjectClient>(sp =>
            FplAgentNodes.CreateProjectClient(sp.GetRequiredService<IOptions<FplAgentOptions>>().Value));

        // FplAgentNodes holds on to an FplService for its lifetime, so it has to be scoped
        // (or shorter) to avoid pinning the typed HttpClient forever.
        services.AddScoped<FplAgentNodes>();
        services.AddScoped<TransferWorkflow>();

        return services;
    }
}
