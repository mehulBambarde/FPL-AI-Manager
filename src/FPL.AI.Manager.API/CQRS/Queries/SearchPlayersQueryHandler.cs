using FPL.AI.Manager.RAG.Models;
using FPL.AI.Manager.RAG.Repositories;
using MediatR;

namespace FPL.AI.Manager.API.CQRS.Queries;

public class SearchPlayersQueryHandler
    : IRequestHandler<SearchPlayersQuery, List<SearchResult>>
{
    private readonly FplVectorRepository _repository;

    public SearchPlayersQueryHandler(FplVectorRepository repository)
    {
        _repository = repository;
    }

    public async Task<List<SearchResult>> Handle(
        SearchPlayersQuery request, CancellationToken ct)
    {
        return await _repository.SearchAsync(request.Query, request.TopN, ct);
    }
}
