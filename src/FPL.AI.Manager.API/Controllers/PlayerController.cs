using FPL.AI.Manager.API.CQRS.Queries;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace FPL.AI.Manager.API.Controllers;

[ApiController]
[Route("api/players")]
public class PlayerController : ControllerBase
{
    private readonly IMediator _mediator;

    public PlayerController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpGet("search")]
    public async Task<IActionResult> Search(
        [FromQuery] string query,
        [FromQuery] int topN = 5,
        CancellationToken ct = default)
    {
        var results = await _mediator.Send(new SearchPlayersQuery(query, topN), ct);
        return Ok(results);
    }
}
