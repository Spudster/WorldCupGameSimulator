using WorldCup.Data.Models;
using WorldCup.Data.Providers;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Random;
using WorldCup.Engine.Simulation;
using WorldCup.Reporting;
using Xunit;

namespace WorldCup.Tests;

public class CommentaryTests
{
    private static TournamentData Data => new SeedTeamDataProvider().GetTournamentData();

    private static MatchResult Match(ulong seed = 17, string stage = "SemiFinal")
    {
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(seed);
        var s = stage == "SemiFinal" ? Stage.SemiFinal : Stage.Group;
        return MatchSimulator.Simulate(Data.Team("BRA"), Data.Team("ARG"), s, Fidelity.Detailed, p, ref rng, true);
    }

    [Fact]
    public void Commentary_Is_Nonempty_Bookended_And_Two_Voiced()
    {
        var m = Match();
        var lines = CommentaryGenerator.Generate(m);

        Assert.True(lines.Count >= 6);
        Assert.Equal(0, lines[0].Minute); // opens pre-match
        Assert.All(lines, l => Assert.False(string.IsNullOrWhiteSpace(l.Text)));
        Assert.All(lines, l => Assert.True(
            l.Speaker == CommentaryGenerator.PlayByPlay || l.Speaker == CommentaryGenerator.Analyst
            || l.Speaker == CommentaryGenerator.CrowdVoice));

        var transcript = CommentaryGenerator.ToTranscript(m, lines);
        Assert.Contains("Play-by-play commentary", transcript);
        Assert.Contains("Final score", transcript);
    }

    [Fact]
    public void Commentary_Is_Deterministic_For_The_Same_Match()
    {
        var m = Match();
        var a = CommentaryGenerator.Generate(m);
        var b = CommentaryGenerator.Generate(m);

        Assert.Equal(a.Count, b.Count);
        for (int i = 0; i < a.Count; i++)
        {
            Assert.Equal(a[i].Text, b[i].Text);
            Assert.Equal(a[i].Speaker, b[i].Speaker);
        }
    }

    [Fact]
    public void Goals_Are_Narrated_With_The_Running_Score()
    {
        // Find a match with goals so we can check the scoreline appears in the transcript.
        var p = SimulationParameters.CreateStarting();
        var rng = new Xoshiro256(3);
        MatchResult? m = null;
        for (int i = 0; i < 60 && m is null; i++)
        {
            var r = MatchSimulator.Simulate(Data.Team("BRA"), Data.Team("HAI"), Stage.Group, Fidelity.Detailed, p, ref rng, true);
            if (r.HomeGoals + r.AwayGoals >= 2)
            {
                m = r;
            }
        }

        Assert.NotNull(m);
        var transcript = CommentaryGenerator.ToTranscript(m!, CommentaryGenerator.Generate(m!));
        Assert.Contains($"Final score: {m!.HomeName} {m.HomeGoals}", transcript);
    }

    [Fact]
    public void Phrase_Pools_Are_Large_And_Varied()
    {
        var rng = new Xoshiro256(1234);
        var filler = new HashSet<string>();
        var screamer = new HashSet<string>();
        var routine = new HashSet<string>();
        var yellow = new HashSet<string>();
        var aside = new HashSet<string>();
        var intro = new HashSet<string>();
        for (int i = 0; i < 600; i++)
        {
            filler.Add(CommentaryPhrases.Filler(ref rng));
            screamer.Add(CommentaryPhrases.GoalShout(9.6, ref rng));   // screamer tier
            routine.Add(CommentaryPhrases.GoalShout(3.0, ref rng));    // routine tier
            yellow.Add(CommentaryPhrases.Yellow(ref rng));
            aside.Add(CommentaryPhrases.AnalystAside(ref rng));
            intro.Add(CommentaryPhrases.IntroOpener(ref rng));
        }

        // Every pool should be deep (30+ entries) so commentary rarely repeats.
        Assert.True(filler.Count >= 30, $"filler distinct {filler.Count}");
        Assert.True(screamer.Count >= 30, $"screamer distinct {screamer.Count}");
        Assert.True(routine.Count >= 30, $"routine distinct {routine.Count}");
        Assert.True(yellow.Count >= 30, $"yellow distinct {yellow.Count}");
        Assert.True(aside.Count >= 30, $"aside distinct {aside.Count}");
        Assert.True(intro.Count >= 30, $"intro distinct {intro.Count}");
    }

    [Fact]
    public void Downloading_The_Match_Html_Writes_A_Sibling_Transcript()
    {
        var m = Match();
        string html = Path.Combine(Path.GetTempPath(), "wc_test_match_sibling.html");
        string txt = Path.Combine(Path.GetDirectoryName(html)!, Path.GetFileNameWithoutExtension(html) + "_commentary.txt");
        if (File.Exists(txt)) File.Delete(txt);
        if (File.Exists(html)) File.Delete(html);

        HtmlExporter.MatchResultToHtml(m, html);

        Assert.True(File.Exists(txt), "a _commentary.txt transcript should be written alongside the HTML");
        Assert.Contains("Play-by-play commentary", File.ReadAllText(txt));
        Assert.Contains("Live commentary", File.ReadAllText(html)); // also embedded in the page

        File.Delete(txt);
        File.Delete(html);
    }
}
