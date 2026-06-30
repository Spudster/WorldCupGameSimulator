using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Tournament;
using Xunit;

namespace WorldCup.Tests;

public class KnockoutScheduleResolverTests
{
    private static TournamentData LoadData() => new SeedTeamDataProvider().GetTournamentData();

    [Fact]
    public void Every_Knockout_Match_Has_A_2026_Utc_Kickoff()
    {
        var bracket = OfficialBracket2026.Build();
        Assert.All(bracket.Matches, m =>
        {
            Assert.NotEqual(default, m.KickoffUtc);
            Assert.Equal(2026, m.KickoffUtc.Year);
            Assert.Equal(DateTimeKind.Utc, m.KickoffUtc.Kind);
        });
    }

    [Fact]
    public void Round_Of_32_Has_Three_Matches_On_June_29()
    {
        var bracket = OfficialBracket2026.Build();
        var june29 = bracket.Matches
            .Where(m => m.Stage == Stage.RoundOf32 && m.KickoffUtc.Date == new DateTime(2026, 6, 29))
            .Select(m => m.Id)
            .OrderBy(id => id)
            .ToList();
        Assert.Equal(new[] { 74, 75, 76 }, june29);
    }

    [Fact]
    public void Knockout_Schedule_Spans_June28_Opener_To_July19_Final()
    {
        var byId = OfficialBracket2026.Build().Matches.ToDictionary(m => m.Id);
        Assert.Equal(new DateTime(2026, 6, 28), byId[73].KickoffUtc.Date);  // first knockout match
        Assert.Equal(new DateTime(2026, 7, 19), byId[104].KickoffUtc.Date); // final
        Assert.All(byId.Values, m => Assert.True(m.KickoffUtc <= byId[104].KickoffUtc));
    }

    [Fact]
    public void With_No_Results_R32_Is_Projected_And_Later_Rounds_Are_Pending()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();

        var schedule = KnockoutScheduleResolver.Resolve(data, Array.Empty<PlayedResult>(), p, seed: 12345);

        Assert.False(schedule.AllGroupsComplete);
        Assert.Equal(12, schedule.IncompleteGroups.Count);

        var r32 = schedule.Fixtures.Where(f => f.Stage == Stage.RoundOf32).ToList();
        Assert.Equal(16, r32.Count);
        Assert.All(r32, f =>
        {
            Assert.True(f.IsResolved);  // teams projected from current form, so always concrete
            Assert.True(f.Projected);
            Assert.False(f.Played);
        });

        // Later rounds depend on R32 winners, which have not been played -> bracket placeholders.
        Assert.All(schedule.Fixtures.Where(f => f.Stage == Stage.RoundOf16), f => Assert.False(f.IsResolved));
    }

    [Fact]
    public void With_A_Complete_Group_Stage_R32_Resolves_To_32_Distinct_Real_Teams()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var results = CompleteGroupStage(data);

        var schedule = KnockoutScheduleResolver.Resolve(data, results, p, seed: 999);

        Assert.True(schedule.AllGroupsComplete);
        Assert.Empty(schedule.IncompleteGroups);

        var r32 = schedule.Fixtures.Where(f => f.Stage == Stage.RoundOf32).ToList();
        Assert.All(r32, f =>
        {
            Assert.True(f.IsResolved);
            Assert.False(f.Projected);  // groups are settled, so nothing is projected
        });

        var teams = r32.SelectMany(f => new[] { f.HomeCode!, f.AwayCode! }).ToList();
        Assert.Equal(32, teams.Count);
        Assert.Equal(32, teams.Distinct(StringComparer.OrdinalIgnoreCase).Count());
        Assert.All(teams, c => Assert.True(data.TryGetTeam(c, out _)));
    }

    [Fact]
    public void A_Played_R32_Result_Feeds_The_Round_Of_16()
    {
        var data = LoadData();
        var p = SimulationParameters.CreateStarting();
        var results = CompleteGroupStage(data).ToList();

        // Resolve once to learn M73's concrete teams, then record a real result for it (home advances).
        var m73 = KnockoutScheduleResolver.Resolve(data, results, p, seed: 7).Fixtures.Single(f => f.MatchId == 73);
        Assert.True(m73.IsResolved);
        results.Add(new PlayedResult(m73.HomeCode!, m73.AwayCode!, 2, 0));

        var schedule = KnockoutScheduleResolver.Resolve(data, results, p, seed: 7);

        var played = schedule.Fixtures.Single(f => f.MatchId == 73);
        Assert.True(played.Played);
        Assert.Equal(2, played.HomeGoals);

        // R16 match M90's top side is "winner of M73" — it should now be M73's home team.
        var m90 = schedule.Fixtures.Single(f => f.MatchId == 90);
        Assert.Equal(m73.HomeCode, m90.HomeCode);
    }

    /// <summary>A full, deterministic group stage where the lower-pot (stronger-seeded) team always wins 2–0.</summary>
    private static List<PlayedResult> CompleteGroupStage(TournamentData data)
    {
        var results = new List<PlayedResult>();
        foreach (var f in data.GroupSchedule)
        {
            bool homeWins = data.Team(f.HomeCode).Pot <= data.Team(f.AwayCode).Pot;
            results.Add(new PlayedResult(f.HomeCode, f.AwayCode, homeWins ? 2 : 0, homeWins ? 0 : 2));
        }

        return results;
    }
}
