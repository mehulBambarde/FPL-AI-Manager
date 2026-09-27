using FPL.AI.Manager.RAG.Configuration;
using FPL.AI.Manager.RAG.Repositories;
using FPL.AI.Manager.RAG.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace FPL.AI.Manager.RAG;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddFplRag(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.Configure<QdrantOptions>(options => 
            configuration.GetSection("Qdrant"));
        services.AddScoped<QdrantService>();

        services.AddScoped<EmbeddingService>(sp =>
        {
            var qdrantOptions = sp.GetRequiredService<IOptions<QdrantOptions>>();
            var endpoint = configuration["AzureOpenAI:BaseEndpoint"]!;
            var embeddingModel = configuration["AzureOpenAI:EmbeddingDeploymentName"]!;
            return new EmbeddingService(qdrantOptions, endpoint, embeddingModel);
        });

        services.AddScoped<IngestionService>();
        services.AddScoped<FplVectorRepository>();

        return services;
    }
}