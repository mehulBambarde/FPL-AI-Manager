namespace FPL.AI.Manager.RAG.Models;

public class FplPlayerRecord
{
    public string Name { get; set; } = string.Empty;
    public string Position { get; set; } = string.Empty;
    public string Team { get; set; } = string.Empty;
    public int GoalsScored { get; set; }
    public int Assists { get; set; }
    public int TotalPoints { get; set; }
    public int Minutes { get; set; }
    public int Bonus { get; set; }
    public int Bps { get; set; }
    public int CleanSheets { get; set; }
    public double Creativity { get; set; }
    public double Influence { get; set; }
    public double Threat { get; set; }
    public double IctIndex { get; set; }
    public int TransfersIn { get; set; }
    public int TransfersOut { get; set; }
    public int Selected { get; set; }
    public int Value { get; set; }
    public int Round { get; set; }
    public bool WasHome { get; set; }
    public string OpponentTeam { get; set; } = string.Empty;
    public int TeamHScore { get; set; }
    public int TeamAScore { get; set; }
    public double ExpectedGoals { get; set; }
    public double ExpectedAssists { get; set; }
    public double ExpectedGoalInvolvements { get; set; }
    public double ExpectedGoalsConceded { get; set; }
    public string Season { get; set; } = string.Empty;

    // Convert to natural language for embedding
    public string ToEmbeddingText() =>
        $"Player: {Name}, Season: {Season}, Gameweek: {Round}, " +
        $"Points: {TotalPoints}, Goals: {GoalsScored}, Assists: {Assists}, " +
        $"Minutes: {Minutes}, Was Home: {WasHome}, Opponent: {OpponentTeam}, " +
        $"Price: {Value / 10.0}m, xG: {ExpectedGoals}, xA: {ExpectedAssists}, " +
        $"Bonus: {Bonus}, Clean Sheets: {CleanSheets}, ICT: {IctIndex}";
}