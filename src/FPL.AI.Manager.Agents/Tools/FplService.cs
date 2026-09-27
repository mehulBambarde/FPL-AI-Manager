using System.ComponentModel;
using System.Globalization;
using System.Text.Json;

namespace FPL.AI.Manager.Agents.Tools;

/// <summary>
/// A thin wrapper around the public Fantasy Premier League API that the agents call as tools.
/// </summary>
/// <remarks>
/// <para>
/// The <see cref="DescriptionAttribute"/> text on each method and parameter isn't just for us:
/// it's what the model reads when deciding which tool to call and what to pass in, so keep
/// it clear and accurate if you change anything.
/// </para>
/// <para>
/// Every method takes a <see cref="CancellationToken"/>. The agent framework fills that in
/// automatically when it invokes a tool and hides it from the model, so the model never sees it
/// as a parameter.
/// </para>
/// <para>
/// Register this as a typed <see cref="HttpClient"/> (see
/// <c>ServiceCollectionExtensions.AddFplAgents</c>) rather than creating it by hand, so the
/// underlying connections are pooled properly.
/// </para>
/// </remarks>
public sealed class FplService
{
    /// <summary>
    /// The root of the public FPL API. Every endpoint we use hangs off this.
    /// </summary>
    public const string BaseAddress = "https://fantasy.premierleague.com/api/";

    private readonly HttpClient _httpClient;

    /// <summary>
    /// Creates the service around an <see cref="HttpClient"/> whose base address points at
    /// <see cref="BaseAddress"/>.
    /// </summary>
    /// <param name="httpClient">The HTTP client to use for every request.</param>
    public FplService(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// Returns a short, filtered list of players with the stats that matter for a transfer:
    /// team, position, price, points, form and ownership.
    /// </summary>
    /// <remarks>
    /// The raw bootstrap payload this is built from is almost 2 MB of JSON, far too much to
    /// hand to a model (it blows straight through the deployment's token-per-minute limit).
    /// So we filter and trim it here and only pass back the best few matches, sorted by
    /// total points.
    /// </remarks>
    /// <param name="position">Optional position filter: GKP, DEF, MID or FWD.</param>
    /// <param name="maxPrice">Optional price ceiling in millions, for example <c>9.3</c>.</param>
    /// <param name="count">How many players to return. Defaults to 20.</param>
    /// <param name="cancellationToken">Lets the caller give up on the request.</param>
    /// <returns>A JSON array of the matching players, best scorers first.</returns>
    [Description("Gets FPL players with team, position, price, total points, form and ownership. Can filter by position and maximum price. Results are sorted by total points, highest first.")]
    public async Task<string> GetPlayersAsync(
        [Description("Optional position filter: GKP, DEF, MID or FWD")] string? position = null,
        [Description("Optional maximum price in millions, e.g. 9.3")] double? maxPrice = null,
        [Description("Number of players to return")] int count = 20,
        CancellationToken cancellationToken = default)
    {
        var data = await GetBootstrapAsync(cancellationToken);
        var teams = GetTeamNames(data);
        var positions = data.GetProperty("element_types")
            .EnumerateArray()
            .ToDictionary(t => t.GetProperty("id").GetInt32(), t => t.GetProperty("singular_name_short").GetString());

        var players = data.GetProperty("elements")
            .EnumerateArray()
            .Select(p => new
            {
                name = p.GetProperty("web_name").GetString(),
                team = teams.GetValueOrDefault(p.GetProperty("team").GetInt32()),
                position = positions.GetValueOrDefault(p.GetProperty("element_type").GetInt32()),
                price = p.GetProperty("now_cost").GetInt32() / 10.0,
                points = p.GetProperty("total_points").GetInt32(),
                form = p.GetProperty("form").GetString(),
                owned = p.GetProperty("selected_by_percent").GetString()
            })
            .Where(p => position is null || string.Equals(p.position, position, StringComparison.OrdinalIgnoreCase))
            .Where(p => maxPrice is null || p.price <= maxPrice)
            .OrderByDescending(p => p.points)
            .Take(count);

        return JsonSerializer.Serialize(players);
    }

    /// <summary>
    /// Returns the Premier League fixtures for the next few gameweeks, with FPL's difficulty
    /// rating for each side.
    /// </summary>
    /// <remarks>
    /// The full season's fixture list is around 230 KB of JSON, which is enough on its own to
    /// hit the model's rate limit. Agents only ever need to look a few weeks ahead, so we
    /// start from the next unfinished gameweek and swap team IDs for team names while we're at it.
    /// </remarks>
    /// <param name="gameweeks">How many gameweeks ahead to include. Defaults to 5.</param>
    /// <param name="cancellationToken">Lets the caller give up on the request.</param>
    /// <returns>
    /// A JSON array of fixtures with the gameweek, kickoff time, home and away team names,
    /// and the difficulty (1 = easiest, 5 = hardest) for each side.
    /// </returns>
    [Description("Gets upcoming Premier League fixtures for the next few gameweeks, with home and away team names and FPL difficulty ratings (1 = easiest, 5 = hardest) for each side")]
    public async Task<string> GetFixturesAsync(
        [Description("How many gameweeks ahead to include")] int gameweeks = 5,
        CancellationToken cancellationToken = default)
    {
        var teams = GetTeamNames(await GetBootstrapAsync(cancellationToken));

        await using var stream = await _httpClient.GetStreamAsync("fixtures/", cancellationToken);
        var fixtures = (await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken))
            .EnumerateArray()
            .Where(f => f.GetProperty("event").ValueKind == JsonValueKind.Number
                        && !f.GetProperty("finished").GetBoolean())
            .ToList();

        if (fixtures.Count == 0)
            return "[]";

        var firstGameweek = fixtures.Min(f => f.GetProperty("event").GetInt32());

        var upcoming = fixtures
            .Where(f => f.GetProperty("event").GetInt32() < firstGameweek + gameweeks)
            .OrderBy(f => f.GetProperty("event").GetInt32())
            .Select(f => new
            {
                gameweek = f.GetProperty("event").GetInt32(),
                kickoff = f.GetProperty("kickoff_time").GetString(),
                home = teams.GetValueOrDefault(f.GetProperty("team_h").GetInt32()),
                away = teams.GetValueOrDefault(f.GetProperty("team_a").GetInt32()),
                homeDifficulty = f.GetProperty("team_h_difficulty").GetInt32(),
                awayDifficulty = f.GetProperty("team_a_difficulty").GetInt32()
            });

        return JsonSerializer.Serialize(upcoming);
    }

    /// <summary>
    /// Gets one player's match-by-match history and their upcoming fixtures.
    /// </summary>
    /// <param name="playerId">The player's FPL ID (the <c>id</c> field in the bootstrap data).</param>
    /// <param name="cancellationToken">Lets the caller give up on the request.</param>
    /// <returns>The raw player summary JSON.</returns>
    [Description("Gets detailed stats and fixture history for a specific player by their FPL player ID")]
    public Task<string> GetPlayerSummaryAsync(
        [Description("The unique FPL player ID found in the bootstrap-static response")]
        int playerId,
        CancellationToken cancellationToken = default) =>
        _httpClient.GetStringAsync($"element-summary/{playerId}/", cancellationToken);

    /// <summary>
    /// Gets live points and stats for every player in a given gameweek.
    /// </summary>
    /// <param name="gameweekId">The gameweek number, from 1 to 38.</param>
    /// <param name="cancellationToken">Lets the caller give up on the request.</param>
    /// <returns>The raw live gameweek JSON.</returns>
    [Description("Gets live scores and stats for all players in a specific gameweek")]
    public Task<string> GetLiveGameweekDataAsync(
        [Description("The gameweek number between 1 and 38")]
        int gameweekId,
        CancellationToken cancellationToken = default) =>
        _httpClient.GetStringAsync($"event/{gameweekId}/live/", cancellationToken);

    /// <summary>
    /// Returns the most-owned players in the game, trimmed down to the handful of fields
    /// the agents actually care about.
    /// </summary>
    /// <remarks>
    /// This is much friendlier to the model than the full bootstrap payload, which is several
    /// megabytes of JSON.
    /// </remarks>
    /// <param name="count">How many players to return. Defaults to 10.</param>
    /// <param name="cancellationToken">Lets the caller give up on the request.</param>
    /// <returns>
    /// A JSON array of players with their name, team ID, price in millions, ownership
    /// percentage and total points.
    /// </returns>
    [Description("Gets the top FPL players by ownership percentage")]
    public async Task<string> GetTopOwnedPlayersAsync(
        [Description("Number of players to return")] int count = 10,
        CancellationToken cancellationToken = default)
    {
        var data = await GetBootstrapAsync(cancellationToken);

        var players = data.GetProperty("elements")
            .EnumerateArray()
            .OrderByDescending(p => double.Parse(
                p.GetProperty("selected_by_percent").GetString() ?? "0",
                CultureInfo.InvariantCulture))
            .Take(count)
            .Select(p => new
            {
                name = p.GetProperty("web_name").GetString(),
                team = p.GetProperty("team").GetInt32(),
                price = p.GetProperty("now_cost").GetInt32() / 10.0,
                owned = p.GetProperty("selected_by_percent").GetString(),
                points = p.GetProperty("total_points").GetInt32()
            });

        return JsonSerializer.Serialize(players);
    }

    /// <summary>
    /// Returns a squad for the squad analyser to pick apart.
    /// </summary>
    /// <remarks>
    /// Heads up: this doesn't fetch the manager's real team yet. It builds a stand-in squad
    /// from the four highest scorers in each position, so <paramref name="teamId"/> and
    /// <paramref name="gameWeek"/> are currently ignored. Swapping this for the
    /// <c>entry/{teamId}/event/{gameWeek}/picks/</c> endpoint is the obvious next step.
    /// </remarks>
    /// <param name="teamId">The manager's FPL team ID.</param>
    /// <param name="gameWeek">The current gameweek.</param>
    /// <param name="cancellationToken">Lets the caller give up on the request.</param>
    /// <returns>
    /// A JSON array of players with their name, position ID, price in millions, total points and form.
    /// </returns>
    [Description("Gets the current squad for analysis - returns top players by position for demonstration")]
    public async Task<string> GetSquadAsync(
        [Description("The FPL team ID")] int teamId,
        [Description("The current gameweek number")] int gameWeek,
        CancellationToken cancellationToken = default)
    {
        var data = await GetBootstrapAsync(cancellationToken);

        var players = data.GetProperty("elements")
            .EnumerateArray()
            .GroupBy(p => p.GetProperty("element_type").GetInt32())
            .SelectMany(g => g.OrderByDescending(p =>
                p.GetProperty("total_points").GetInt32()).Take(4))
            .Select(p => new
            {
                name = p.GetProperty("web_name").GetString(),
                position = p.GetProperty("element_type").GetInt32(),
                price = p.GetProperty("now_cost").GetInt32() / 10.0,
                points = p.GetProperty("total_points").GetInt32(),
                form = p.GetProperty("form").GetString()
            });

        return JsonSerializer.Serialize(players);
    }

    private static Dictionary<int, string?> GetTeamNames(JsonElement bootstrap) =>
        bootstrap.GetProperty("teams")
            .EnumerateArray()
            .ToDictionary(t => t.GetProperty("id").GetInt32(), t => t.GetProperty("name").GetString());

    private async Task<JsonElement> GetBootstrapAsync(CancellationToken cancellationToken)
    {
        await using var stream = await _httpClient.GetStreamAsync("bootstrap-static/", cancellationToken);
        return await JsonSerializer.DeserializeAsync<JsonElement>(stream, cancellationToken: cancellationToken);
    }
}
