using System.ComponentModel;
using System.Text.Json;

namespace FPL.AI.Manager.Console;

public static class FplService
{
    private static readonly HttpClient _httpClient = new HttpClient();
    

    static FplService()
    {
        _httpClient.DefaultRequestHeaders.Add("User-Agent", "FPL-AI-Manager/1.0");
    }

    [Description("Gets all FPL players, teams, and current season statistics including points, price, form, and ownership")]
    public static async Task<string> GetAllPlayersAsync()
    {
        return await _httpClient.GetStringAsync(
            "https://fantasy.premierleague.com/api/bootstrap-static/");
    }

    [Description("Gets all Premier League fixtures including dates, home and away teams, and difficulty ratings")]
    public static async Task<string> GetFixturesAsync()
    {
        return await _httpClient.GetStringAsync(
            "https://fantasy.premierleague.com/api/fixtures/");
    }

    [Description("Gets detailed stats and fixture history for a specific player by their FPL player ID")]
    public static async Task<string> GetPlayerSummaryAsync(
        [Description("The unique FPL player ID found in the bootstrap-static response")] 
        int playerId)
    {
        return await _httpClient.GetStringAsync(
            $"https://fantasy.premierleague.com/api/element-summary/{playerId}/");
    }

    [Description("Gets live scores and stats for all players in a specific gameweek")]
    public static async Task<string> GetLiveGameweekDataAsync(
        [Description("The gameweek number between 1 and 38")] 
        int gameweekId)
    {
        return await _httpClient.GetStringAsync(
            $"https://fantasy.premierleague.com/api/event/{gameweekId}/live/");
    }

    [Description("Gets the top FPL players by ownership percentage")]
    public static async Task<string> GetTopOwnedPlayersAsync(
        [Description("Number of players to return")] int count = 10)
    {
        var json = await _httpClient.GetStringAsync(
            "https://fantasy.premierleague.com/api/bootstrap-static/");

        var data = JsonSerializer.Deserialize<JsonElement>(json);
        var players = data.GetProperty("elements")
            .EnumerateArray()
            .OrderByDescending(p => double.Parse(
                p.GetProperty("selected_by_percent").GetString() ?? "0"))
            .Take(count)
            .Select(p => new {
                name = p.GetProperty("web_name").GetString(),
                team = p.GetProperty("team").GetInt32(),
                price = p.GetProperty("now_cost").GetInt32() / 10.0,
                owned = p.GetProperty("selected_by_percent").GetString(),
                points = p.GetProperty("total_points").GetInt32()
            });

        return JsonSerializer.Serialize(players);
    }
    
    [Description("Gets the current squad for analysis - returns top players by position for demonstration")]
    public static async Task<string> GetSquadAsync(
        [Description("The FPL team ID")] int teamId,
        [Description("The current gameweek number")] int gameWeek)
    {
        // Use public bootstrap data to build a representative squad
        var json = await _httpClient.GetStringAsync(
            "https://fantasy.premierleague.com/api/bootstrap-static/");

        var data = JsonSerializer.Deserialize<JsonElement>(json);
    
        // Get top player by points from each position as a sample squad
        var players = data.GetProperty("elements")
            .EnumerateArray()
            .GroupBy(p => p.GetProperty("element_type").GetInt32())
            .SelectMany(g => g.OrderByDescending(p => 
                p.GetProperty("total_points").GetInt32()).Take(4))
            .Select(p => new {
                name = p.GetProperty("web_name").GetString(),
                position = p.GetProperty("element_type").GetInt32(),
                price = p.GetProperty("now_cost").GetInt32() / 10.0,
                points = p.GetProperty("total_points").GetInt32(),
                form = p.GetProperty("form").GetString()
            });

        return JsonSerializer.Serialize(players);
    }
    
    
}