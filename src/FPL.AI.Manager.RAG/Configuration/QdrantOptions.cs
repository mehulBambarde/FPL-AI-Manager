namespace FPL.AI.Manager.RAG.Configuration;

public class QdrantOptions
{
    public string Host { get; set; } = "localhost";
    public int Port { get; set; } = 6333;
    public string CollectionName { get; set; } = "fpl_historical";
    public int VectorSize { get; set; } = 1536;
}