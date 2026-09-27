using Azure.AI.OpenAI;
using Azure.Identity;
using Microsoft.Extensions.Options;
using FPL.AI.Manager.RAG.Configuration;

namespace FPL.AI.Manager.RAG.Services;

public class EmbeddingService
{
    private readonly AzureOpenAIClient _client;
    private readonly string _embeddingModel;

    public EmbeddingService(IOptions<QdrantOptions> options, string endpoint, string embeddingModel)
    {
        _client = new AzureOpenAIClient(new Uri(endpoint), new AzureCliCredential());
        _embeddingModel = embeddingModel;
    }

    public async Task<float[]> GenerateEmbeddingAsync(string text, CancellationToken ct = default)
    {
        var embeddingClient = _client.GetEmbeddingClient(_embeddingModel);
        
        var response = await embeddingClient.GenerateEmbeddingAsync(text, cancellationToken: ct);
        
        return response.Value.ToFloats().ToArray();
    }
}