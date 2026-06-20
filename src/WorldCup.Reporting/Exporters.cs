using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using WorldCup.Engine.Stats;
using WorldCup.Engine.Tournament;

namespace WorldCup.Reporting;

/// <summary>Exports reports to CSV and JSON.</summary>
public static class Exporters
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Serialize any report object to indented JSON.</summary>
    public static void ToJson(object data, string path)
    {
        File.WriteAllText(path, JsonSerializer.Serialize(data, data.GetType(), JsonOptions));
    }

    public static void TournamentOddsToCsv(TournamentMonteCarloReport r, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("# iterations,parameterSet,seed,fidelity");
        sb.AppendLine($"# {r.Iterations},{Esc(r.ParameterLabel)},{r.Seed},{r.Fidelity}");
        sb.AppendLine("team,group,champion,reachedFinal,reachedSemi,reachedQuarter,reachedR16,reachedR32,topGroup,expectedPoints");
        foreach (var t in r.Teams)
        {
            sb.Append(Esc(t.Name)).Append(',').Append(t.Group).Append(',')
                .Append(F(t.Champion)).Append(',').Append(F(t.ReachedFinal)).Append(',').Append(F(t.ReachedSemi)).Append(',')
                .Append(F(t.ReachedQuarter)).Append(',').Append(F(t.ReachedR16)).Append(',').Append(F(t.ReachedR32)).Append(',')
                .Append(F(t.TopGroup)).Append(',').Append(F(t.ExpectedGroupPoints)).AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    public static void MatchMonteCarloToCsv(MatchMonteCarloReport r, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine("metric,value");
        sb.AppendLine($"{Esc(r.HomeName)} win,{F(r.HomeWin)}");
        sb.AppendLine($"draw,{F(r.Draw)}");
        sb.AppendLine($"{Esc(r.AwayName)} win,{F(r.AwayWin)}");
        sb.AppendLine($"avg home goals,{F(r.AvgHomeGoals)}");
        sb.AppendLine($"avg away goals,{F(r.AvgAwayGoals)}");
        sb.AppendLine();
        sb.AppendLine("homeGoals,awayGoals,count,probability");
        foreach (var s in r.TopScorelines)
        {
            sb.AppendLine($"{s.HomeGoals},{s.AwayGoals},{s.Count},{F(s.Probability)}");
        }

        File.WriteAllText(path, sb.ToString());
    }

    public static void MatchAggregateToCsv(MatchAggregateReport r, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {Esc(r.HomeName)} vs {Esc(r.AwayName)} — aggregated over {r.Iterations} simulations (seed {r.Seed}, {Esc(r.ParameterLabel)})");
        sb.AppendLine("outcome,probability");
        sb.AppendLine($"{Esc(r.HomeName)} win,{F(r.HomeWin)}");
        sb.AppendLine($"draw,{F(r.Draw)}");
        sb.AppendLine($"{Esc(r.AwayName)} win,{F(r.AwayWin)}");
        sb.AppendLine();
        sb.AppendLine("stat,home,away");
        sb.AppendLine($"goals,{F(r.Home.Goals)},{F(r.Away.Goals)}");
        sb.AppendLine($"possession,{F(r.Home.Possession)},{F(r.Away.Possession)}");
        sb.AppendLine($"shots,{F(r.Home.Shots)},{F(r.Away.Shots)}");
        sb.AppendLine($"shotsOnTarget,{F(r.Home.ShotsOnTarget)},{F(r.Away.ShotsOnTarget)}");
        sb.AppendLine($"corners,{F(r.Home.Corners)},{F(r.Away.Corners)}");
        sb.AppendLine($"throwIns,{F(r.Home.ThrowIns)},{F(r.Away.ThrowIns)}");
        sb.AppendLine($"goalKicks,{F(r.Home.GoalKicks)},{F(r.Away.GoalKicks)}");
        sb.AppendLine($"fouls,{F(r.Home.Fouls)},{F(r.Away.Fouls)}");
        sb.AppendLine($"offsides,{F(r.Home.Offsides)},{F(r.Away.Offsides)}");
        sb.AppendLine($"yellows,{F(r.Home.Yellows)},{F(r.Away.Yellows)}");
        sb.AppendLine($"reds,{F(r.Home.Reds)},{F(r.Away.Reds)}");
        sb.AppendLine($"penalties,{F(r.Home.Penalties)},{F(r.Away.Penalties)}");
        sb.AppendLine($"injuries,{F(r.Home.Injuries)},{F(r.Away.Injuries)}");
        sb.AppendLine($"saves,{F(r.Home.Saves)},{F(r.Away.Saves)}");
        sb.AppendLine();
        sb.AppendLine("market,probability");
        sb.AppendLine($"bttsScore,{F(r.BttsPercent / 100)}");
        sb.AppendLine($"over2.5,{F(r.Over25Percent / 100)}");
        sb.AppendLine($"under2.5,{F(1 - r.Over25Percent / 100)}");
        sb.AppendLine($"homeCleanSheet,{F(r.HomeCleanSheetPercent / 100)}");
        sb.AppendLine($"awayCleanSheet,{F(r.AwayCleanSheetPercent / 100)}");
        sb.AppendLine();
        sb.AppendLine("# avg vergazo, worldie %");
        sb.AppendLine($"{F(r.AverageVergazo)},{F(r.WorldiePercent)}");
        sb.AppendLine();
        sb.AppendLine("rank,player,team,goalsPerMatch");
        int scorerRank = 1;
        foreach (var s in r.TopScorers.Take(10))
        {
            sb.AppendLine($"{scorerRank++},{Esc(s.Name)},{s.TeamCode},{F(s.GoalsPerMatch)}");
        }

        sb.AppendLine();
        sb.AppendLine("# mistakes & officiating (per game, both teams)");
        sb.AppendLine("metric,perGame");
        var c = r.Controversy;
        sb.AppendLine($"keeperErrorGoals,{F(c.KeeperErrorGoals)}");
        sb.AppendLine($"defensiveErrorGoals,{F(c.DefensiveErrorGoals)}");
        sb.AppendLine($"unpunishedErrors,{F(c.UnpunishedErrors)}");
        sb.AppendLine($"controversialPenalties,{F(c.ControversialPenalties)}");
        sb.AppendLine($"controversialCards,{F(c.ControversialCards)}");
        sb.AppendLine($"refereeMistakes,{F(c.RefereeMistakes)}");
        sb.AppendLine();
        sb.AppendLine("homeGoals,awayGoals,count,probability");
        foreach (var s in r.TopScorelines)
        {
            sb.AppendLine($"{s.HomeGoals},{s.AwayGoals},{s.Count},{F(s.Probability)}");
        }

        File.WriteAllText(path, sb.ToString());
    }

    public static void ScheduledForecastsToCsv(ScheduledForecastReport r, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# {r.Games.Count} remaining fixtures · {r.IterationsPerGame} simulations each · {Esc(r.ParameterLabel)} · seed {r.Seed}");
        sb.AppendLine("group,matchday,kickoffUtc,home,away,forecastScore,homeWin,draw,awayWin,xgHome,xgAway,favourite");
        foreach (var f in r.Games.OrderBy(g => g.KickoffUtc).ThenBy(g => g.Group).ThenBy(g => g.Matchday))
        {
            var m = f.Report;
            string score = f.ModalScore is { } s ? $"{s.HomeGoals}-{s.AwayGoals}" : "";
            string fav = f.Favourite == 1 ? m.HomeName : f.Favourite == -1 ? m.AwayName : "draw";
            sb.Append(f.Group).Append(',').Append(f.Matchday).Append(',')
                .Append(f.KickoffUtc.ToString("yyyy-MM-ddTHH:mm:ssZ", CultureInfo.InvariantCulture)).Append(',')
                .Append(Esc(m.HomeName)).Append(',').Append(Esc(m.AwayName)).Append(',').Append(score).Append(',')
                .Append(F(m.HomeWin)).Append(',').Append(F(m.Draw)).Append(',').Append(F(m.AwayWin)).Append(',')
                .Append(F(m.AvgHomeGoals)).Append(',').Append(F(m.AvgAwayGoals)).Append(',').Append(Esc(fav)).AppendLine();
        }

        File.WriteAllText(path, sb.ToString());
    }

    public static void StatsToCsv(StatsReport r, string path)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"# Detailed stats across {r.Tournaments} tournament(s)");
        sb.AppendLine();
        sb.AppendLine("GOLDEN BOOT");
        sb.AppendLine("rank,player,team,goals,assists,minutes");
        int rank = 1;
        foreach (var p in r.GoldenBoot)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{p.Goals},{p.Assists},{p.Minutes}");
        }

        sb.AppendLine();
        sb.AppendLine("MVP");
        sb.AppendLine("rank,player,team,score,goals,assists,cleanSheets,minutes,reached");
        rank = 1;
        foreach (var p in r.Mvp)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{F(p.Score)},{p.Goals},{p.Assists},{p.CleanSheets},{p.Minutes},{Esc(p.FurthestStage)}");
        }

        sb.AppendLine();
        sb.AppendLine("GOLDEN GLOVE");
        sb.AppendLine("rank,goalkeeper,team,cleanSheets,saves,amazingSaves,conceded");
        rank = 1;
        foreach (var p in r.GoldenGlove)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{p.CleanSheets},{p.Saves},{p.AmazingSaves},{p.GoalsConceded}");
        }

        sb.AppendLine();
        sb.AppendLine("TEAM STATS");
        sb.AppendLine("team,gf,ga,cleanSheets,shots,shotsOnTarget,corners,yellows,reds,possession");
        foreach (var t in r.Teams)
        {
            sb.AppendLine($"{Esc(t.Name)},{t.GoalsFor},{t.GoalsAgainst},{t.CleanSheets},{t.Shots},{t.ShotsOnTarget},{t.Corners},{t.Yellows},{t.Reds},{F(t.PossessionAvg)}");
        }

        sb.AppendLine();
        sb.AppendLine("CRAZY STATS / RECORDS");
        sb.AppendLine("category,detail,value");
        foreach (var rec in r.CrazyStats)
        {
            sb.AppendLine($"{Esc(rec.Category)},{Esc(rec.Description)},{F(rec.Value)}");
        }

        sb.AppendLine();
        sb.AppendLine("TOP ASSISTS");
        sb.AppendLine("rank,player,team,assists,goals");
        rank = 1;
        foreach (var p in r.TopAssists)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{p.Assists},{p.Goals}");
        }

        sb.AppendLine();
        sb.AppendLine("MOST YELLOWS");
        sb.AppendLine("rank,player,team,yellows,reds");
        rank = 1;
        foreach (var p in r.MostYellows)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{p.Yellows},{p.Reds}");
        }

        sb.AppendLine();
        sb.AppendLine("MOST REDS");
        sb.AppendLine("rank,player,team,reds,yellows");
        rank = 1;
        foreach (var p in r.MostReds)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{p.Reds},{p.Yellows}");
        }

        sb.AppendLine();
        sb.AppendLine("PENALTY TAKERS");
        sb.AppendLine("rank,player,team,scored,missed");
        rank = 1;
        foreach (var p in r.PenaltyTakers)
        {
            sb.AppendLine($"{rank++},{Esc(p.Name)},{p.TeamCode},{p.Scored},{p.Missed}");
        }

        sb.AppendLine();
        sb.AppendLine("GOALS BY TYPE");
        sb.AppendLine("type,count,percent");
        foreach (var g in r.GoalsByType)
        {
            sb.AppendLine($"{Esc(g.Type)},{g.Count},{F(g.Percent)}");
        }

        sb.AppendLine();
        sb.AppendLine($"# total injuries: {r.TotalInjuries}");
        sb.AppendLine("INJURIES");
        sb.AppendLine("minute,player,team,bodyPart,diagnosis,severity,recoveryDays,replaced");
        foreach (var inj in r.Injuries)
        {
            sb.AppendLine($"{inj.Minute},{Esc(inj.PlayerName)},{inj.TeamCode},{Esc(inj.BodyPart)},{Esc(inj.Diagnosis)},{inj.Severity},{inj.RecoveryDays},{(inj.Replaced ? "yes" : "no")}");
        }

        if (r.Tournaments > 1)
        {
            AppendAwardFrequency(sb, "GOLDEN BOOT FREQUENCY", r.GoldenBootFrequency);
            AppendAwardFrequency(sb, "MVP FREQUENCY", r.MvpFrequency);
            AppendAwardFrequency(sb, "GOLDEN GLOVE FREQUENCY", r.GoldenGloveFrequency);
        }

        File.WriteAllText(path, sb.ToString());
    }

    private static void AppendAwardFrequency(StringBuilder sb, string header, IReadOnlyList<AwardFrequencyRow> rows)
    {
        sb.AppendLine();
        sb.AppendLine(header);
        sb.AppendLine("rank,player,team,wins,frequency,avgTally");
        int rank = 1;
        foreach (var row in rows)
        {
            sb.AppendLine($"{rank++},{Esc(row.Name)},{row.TeamCode},{row.Wins},{F(row.Frequency)},{F(row.AverageTally)}");
        }
    }

    private static string F(double v) => v.ToString("0.#####", CultureInfo.InvariantCulture);

    private static string Esc(string field)
    {
        if (field.Contains(',') || field.Contains('"') || field.Contains('\n'))
        {
            return "\"" + field.Replace("\"", "\"\"") + "\"";
        }

        return field;
    }
}
