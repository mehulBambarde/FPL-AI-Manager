using FPL.AI.Manager.Agents.DependencyInjection;
using FPL.AI.Manager.RAG;
using FPL.AI.Manager.RAG.Configuration;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader());
});

// RAG: QdrantService, EmbeddingService, IngestionService, FplVectorRepository
builder.Services.Configure<QdrantOptions>(builder.Configuration.GetSection("Qdrant"));
builder.Services.AddFplRag(builder.Configuration);

// Agents: FplService, AIProjectClient, FplAgentNodes, TransferWorkflow
builder.Services.AddFplAgents(builder.Configuration);

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors();
app.UseAuthorization();
app.MapControllers();

app.Run();
