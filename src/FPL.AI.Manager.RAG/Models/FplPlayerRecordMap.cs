using CsvHelper.Configuration;

namespace FPL.AI.Manager.RAG.Models;

public class FplPlayerRecordMap : ClassMap<FplPlayerRecord>
{
    public FplPlayerRecordMap()
    {
        Map(m => m.Name).Name("name");
        Map(m => m.Position).Name("position");
        Map(m => m.Team).Name("team");
        Map(m => m.GoalsScored).Name("goals_scored");
        Map(m => m.Assists).Name("assists");
        Map(m => m.TotalPoints).Name("total_points");
        Map(m => m.Minutes).Name("minutes");
        Map(m => m.Bonus).Name("bonus");
        Map(m => m.Bps).Name("bps");
        Map(m => m.CleanSheets).Name("clean_sheets");
        Map(m => m.Creativity).Name("creativity");
        Map(m => m.Influence).Name("influence");
        Map(m => m.Threat).Name("threat");
        Map(m => m.IctIndex).Name("ict_index");
        Map(m => m.TransfersIn).Name("transfers_in");
        Map(m => m.TransfersOut).Name("transfers_out");
        Map(m => m.Selected).Name("selected");
        Map(m => m.Value).Name("value");
        Map(m => m.Round).Name("round");
        Map(m => m.WasHome).Name("was_home");
        Map(m => m.OpponentTeam).Name("opponent_team");
        Map(m => m.TeamHScore).Name("team_h_score");
        Map(m => m.TeamAScore).Name("team_a_score");
        Map(m => m.ExpectedGoals).Name("expected_goals");
        Map(m => m.ExpectedAssists).Name("expected_assists");
        Map(m => m.ExpectedGoalInvolvements).Name("expected_goal_involvements");
        Map(m => m.ExpectedGoalsConceded).Name("expected_goals_conceded");
    }
}