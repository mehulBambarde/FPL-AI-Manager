using FPL.AI.Manager.RAG.Models;
using MediatR;

namespace FPL.AI.Manager.API.CQRS.Queries;

public record SearchPlayersQuery(string Query, int TopN = 5)
    : IRequest<List<SearchResult>>;
