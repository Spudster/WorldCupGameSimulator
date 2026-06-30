using Spectre.Console;
using WorldCup.Data;
using WorldCup.Data.Models;
using WorldCup.Engine.Calibration;
using WorldCup.Engine.Parameters;
using WorldCup.Reporting;

namespace WorldCup.Cli;

/// <summary>The Parameters submenu: view / edit / save / load / reset, calibration and auto-tune.</summary>
public sealed class ParametersMenu
{
    private readonly Session _session;

    public ParametersMenu(Session session)
    {
        _session = session;
    }

    public void Run()
    {
        while (true)
        {
            Ui.Blank();
            var choice = Nav.Show(new SelectionPrompt<string>()
                .Title("[deepskyblue1]Parameters[/]")
                .PageSize(16)
                .WrapAround()
                .AddChoices(
                    "View current parameters",
                    "View starting parameters",
                    "Edit a team's strength",
                    "Edit a player's attributes",
                    "Set a team's formation",
                    "Set a team's starting goalkeeper",
                    "Set player availability (injury/suspension)",
                    "Edit global knob",
                    "Toggle third-place playoff",
                    "Save current to file",
                    "Load current from file",
                    "Reset current to starting",
                    "Run calibration diagnostics",
                    "Auto-tune to targets",
                    "Back to main menu"));

            switch (choice)
            {
                case "View current parameters": ParametersFormatter.Print(_session.Current); break;
                case "View starting parameters": ParametersFormatter.Print(_session.Starting); break;
                case "Edit a team's strength": EditTeamStrength(); break;
                case "Edit a player's attributes": EditPlayerAttributes(); break;
                case "Set a team's formation": EditFormation(); break;
                case "Set a team's starting goalkeeper": SetGoalkeeper(); break;
                case "Set player availability (injury/suspension)": EditAvailability(); break;
                case "Edit global knob": EditGlobalKnob(); break;
                case "Toggle third-place playoff": ToggleThirdPlace(); break;
                case "Save current to file": SaveCurrent(); break;
                case "Load current from file": LoadCurrent(); break;
                case "Reset current to starting": ResetCurrent(); break;
                case "Run calibration diagnostics": RunCalibration(); break;
                case "Auto-tune to targets": AutoTune(); break;
                case "Back to main menu": return;
            }
        }
    }

    private void EditTeamStrength()
    {
        var team = ConsoleHelpers.PickTeam(_session.Data, "Pick a team to edit:");
        double currentVal = _session.Current.EffectiveStrength(team);
        double newVal = Nav.Show(new TextPrompt<double>($"New strength for {team.Name} (0-100):")
            .DefaultValue(Math.Round(currentVal, 1))
            .Validate(v => v is >= 0 and <= 100 ? ValidationResult.Success() : ValidationResult.Error("[red]0-100[/]")));
        _session.Current.TeamStrengthOverrides[team.Code] = newVal;
        Ui.Success($"{team.Name} strength set to {newVal:0.0} in Current parameters.");
    }

    private void EditPlayerAttributes()
    {
        var team = ConsoleHelpers.PickTeam(_session.Data, "Pick the player's team:");
        var playerLabel = Nav.Show(new SelectionPrompt<string>()
            .Title($"Pick a player from {team.Name}:")
            .PageSize(15)
            .AddChoices(team.Squad.Select(pl => $"{pl.Id} — {pl.Name} ({pl.Position})")));
        string id = playerLabel.Split(' ')[0];
        var player = team.Squad.First(pl => pl.Id == id);
        var attr = _session.Current.EffectiveAttributes(player);

        var which = Nav.Show(new SelectionPrompt<string>()
            .Title("Which attribute?")
            .AddChoices("Finishing", "Creativity", "Discipline", "Injury proneness", "Goalkeeping"));
        double cur = which switch
        {
            "Finishing" => attr.Finishing,
            "Creativity" => attr.Creativity,
            "Discipline" => attr.Discipline,
            "Injury proneness" => attr.InjuryProneness,
            _ => attr.Goalkeeping,
        };
        double val = Nav.Show(new TextPrompt<double>($"New {which} (0-100):")
            .DefaultValue(Math.Round(cur, 1))
            .Validate(v => v is >= 0 and <= 100 ? ValidationResult.Success() : ValidationResult.Error("[red]0-100[/]")));

        var updated = which switch
        {
            "Finishing" => attr with { Finishing = val },
            "Creativity" => attr with { Creativity = val },
            "Discipline" => attr with { Discipline = val },
            "Injury proneness" => attr with { InjuryProneness = val },
            _ => attr with { Goalkeeping = val },
        };
        _session.Current.PlayerAttributeOverrides[player.Id] = updated.Clamped();
        Ui.Success($"{player.Name}: {which} set to {val:0.0} in Current parameters.");
    }

    private void EditFormation()
    {
        var team = ConsoleHelpers.PickTeam(_session.Data, "Pick a team to set the formation:");
        string current = _session.Current.Formation(team);
        string formation = Nav.Show(new SelectionPrompt<string>()
            .Title($"Formation for {team.Name} [grey](current: {current})[/]:")
            .AddChoices(LineupProjector.Common));

        _session.Current.FormationOverrides[team.Code] = formation;
        var proj = LineupProjector.Project(team, formation, _session.Current.IsAvailable, _session.Current.PreferredStarters);
        Ui.Success($"{team.Name} will line up {formation} (Current parameters).");
        Ui.Info("XI: " + string.Join(", ", proj.Xi.Select(pl => pl.Name)));
    }

    private void SetGoalkeeper()
    {
        var team = ConsoleHelpers.PickTeam(_session.Data, "Pick the team whose goalkeeper to set:");
        ConsoleHelpers.SetStartingGoalkeeper(_session, team);
    }

    private void EditAvailability()
    {
        var team = ConsoleHelpers.PickTeam(_session.Data, "Pick the player's team:");
        string label = Nav.Show(new SelectionPrompt<string>()
            .Title($"Toggle availability for which {team.Name} player?")
            .PageSize(16)
            .AddChoices(team.Squad.Select(pl =>
                $"{pl.Id} — {pl.Name} ({pl.Position})" + (_session.Current.IsAvailable(pl) ? "" : " — OUT"))));

        string id = label.Split(' ')[0];
        var player = team.Squad.First(pl => pl.Id == id);
        if (_session.Current.UnavailablePlayers.Remove(player.Id))
        {
            Ui.Success($"{player.Name} is now AVAILABLE.");
        }
        else
        {
            _session.Current.UnavailablePlayers.Add(player.Id);
            Ui.Success($"{player.Name} is now UNAVAILABLE (injured/suspended/rested) — dropped from the XI.");
        }
    }

    private void EditGlobalKnob()
    {
        var g = _session.Current.Global;
        var categories = new Dictionary<string, string[]>
        {
            ["Match model"] = new[]
            {
                "Goal baseline", "Strength sensitivity", "Home advantage", "Draw coupling",
                "Upset variance", "Match tempo variance", "Momentum strength", "Squad quality weight",
                "Recent-form weight", "Extra-time goal scale", "Shootout strength weight", "RNG seed",
            },
            ["Event rates"] = new[]
            {
                "Yellow cards / match", "Direct red cards / match", "Penalties / match",
                "Corners / match", "Injuries / match",
            },
            ["Mistakes & officiating"] = new[]
            {
                "Def. error → goal share", "GK error → goal share", "Unpunished errors / team",
                "Wrong-penalty share", "Wrong-card share", "Referee mistakes / match",
            },
        };

        string category = Nav.Show(new SelectionPrompt<string>()
            .Title("Which group of knobs?")
            .WrapAround()
            .AddChoices(categories.Keys));
        var knob = Nav.Show(new SelectionPrompt<string>()
            .Title($"[deepskyblue1]{category}[/] — which knob?")
            .PageSize(14)
            .WrapAround()
            .AddChoices(categories[category]));

        if (knob == "RNG seed")
        {
            g.Seed = Nav.Show(new TextPrompt<ulong>("New RNG seed:").DefaultValue(g.Seed));
            Ui.Success($"Seed set to {g.Seed}.");
            return;
        }

        double cur = ReadKnob(g, knob);
        double val = Nav.Show(new TextPrompt<double>($"New value for {knob}:").DefaultValue(Math.Round(cur, 4)));
        WriteKnob(g, knob, val);
        Ui.Success($"{knob} set to {val}.");
    }

    private static double ReadKnob(GlobalParameters g, string knob) => knob switch
    {
        "Goal baseline" => g.GoalBaseline,
        "Strength sensitivity" => g.StrengthSensitivity,
        "Home advantage" => g.HomeAdvantage,
        "Draw coupling" => g.DrawCoupling,
        "Upset variance" => g.UpsetVariance,
        "Match tempo variance" => g.MatchTempoVariance,
        "Momentum strength" => g.MomentumStrength,
        "Squad quality weight" => g.SquadQualityWeight,
        "Recent-form weight" => g.FormWeight,
        "Extra-time goal scale" => g.ExtraTimeGoalScale,
        "Shootout strength weight" => g.ShootoutStrengthWeight,
        "Yellow cards / match" => g.Events.YellowCardsPerMatch,
        "Direct red cards / match" => g.Events.DirectRedCardsPerMatch,
        "Penalties / match" => g.Events.PenaltiesPerMatch,
        "Corners / match" => g.Events.CornersPerMatch,
        "Injuries / match" => g.Events.InjuriesPerMatch,
        "Def. error → goal share" => g.Events.DefensiveErrorGoalShare,
        "GK error → goal share" => g.Events.GoalkeeperErrorGoalShare,
        "Unpunished errors / team" => g.Events.UnpunishedErrorsPerMatch,
        "Wrong-penalty share" => g.Events.WrongPenaltyShare,
        "Wrong-card share" => g.Events.WrongCardShare,
        "Referee mistakes / match" => g.Events.RefereeMistakesPerMatch,
        _ => 0,
    };

    private static void WriteKnob(GlobalParameters g, string knob, double val)
    {
        switch (knob)
        {
            case "Goal baseline": g.GoalBaseline = val; break;
            case "Strength sensitivity": g.StrengthSensitivity = val; break;
            case "Home advantage": g.HomeAdvantage = val; break;
            case "Draw coupling": g.DrawCoupling = val; break;
            case "Upset variance": g.UpsetVariance = val; break;
            case "Match tempo variance": g.MatchTempoVariance = val; break;
            case "Momentum strength": g.MomentumStrength = val; break;
            case "Squad quality weight": g.SquadQualityWeight = val; break;
            case "Recent-form weight": g.FormWeight = val; break;
            case "Extra-time goal scale": g.ExtraTimeGoalScale = val; break;
            case "Shootout strength weight": g.ShootoutStrengthWeight = val; break;
            case "Yellow cards / match": g.Events.YellowCardsPerMatch = val; break;
            case "Direct red cards / match": g.Events.DirectRedCardsPerMatch = val; break;
            case "Penalties / match": g.Events.PenaltiesPerMatch = val; break;
            case "Corners / match": g.Events.CornersPerMatch = val; break;
            case "Injuries / match": g.Events.InjuriesPerMatch = val; break;
            case "Def. error → goal share": g.Events.DefensiveErrorGoalShare = val; break;
            case "GK error → goal share": g.Events.GoalkeeperErrorGoalShare = val; break;
            case "Unpunished errors / team": g.Events.UnpunishedErrorsPerMatch = val; break;
            case "Wrong-penalty share": g.Events.WrongPenaltyShare = val; break;
            case "Wrong-card share": g.Events.WrongCardShare = val; break;
            case "Referee mistakes / match": g.Events.RefereeMistakesPerMatch = val; break;
        }
    }

    private void ToggleThirdPlace()
    {
        _session.IncludeThirdPlacePlayoff = !_session.IncludeThirdPlacePlayoff;
        Ui.Success($"Third-place playoff is now {(_session.IncludeThirdPlacePlayoff ? "ON" : "OFF")}.");
    }

    private void SaveCurrent()
    {
        string name = Nav.Ask("File name", "parameters.json");
        string path = OutputFolder.Resolve(name);
        _session.Current.Save(path);
        Ui.Success($"Saved current parameters to {path}.");
    }

    private void LoadCurrent()
    {
        string name = Nav.Ask("File name", "parameters.json");
        string path = OutputFolder.Find(name);
        if (!File.Exists(path))
        {
            Ui.Warning($"File not found: {path}");
            return;
        }

        try
        {
            _session.Current = SimulationParameters.Load(path);
            Ui.Success($"Loaded parameters from {path} (label: {_session.Current.Label}).");
        }
        catch (Exception ex)
        {
            Ui.Warning($"Could not load: {ex.Message}");
        }
    }

    private void ResetCurrent()
    {
        if (Nav.Confirm("Reset Current parameters to the pristine defaults?"))
        {
            _session.ResetCurrentToStarting();
            Ui.Success("Current parameters reset to Starting.");
        }
    }

    private void RunCalibration()
    {
        var p = Nav.Show(new SelectionPrompt<string>()
            .Title("Calibrate which set?").AddChoices("Current", "Starting"));
        var paramSet = p == "Current" ? _session.Current : _session.Starting;
        int matches = (int)ConsoleHelpers.PromptIterations("Matches to measure?", 5_000);

        var report = ConsoleHelpers.RunWithProgress("Measuring", matches, (counter, ct) =>
        {
            // CalibrationRunner.Measure is internal-batch; approximate progress by chunking.
            var r = CalibrationRunner.Measure(_session.Data, paramSet, matches);
            counter.Add(matches);
            return r;
        });
        ParametersFormatter.PrintCalibration(report);
    }

    private void AutoTune()
    {
        if (!Nav.Confirm("Auto-tune will adjust the Current global rate knobs to hit the targets. Proceed?"))
        {
            return;
        }

        int matches = (int)ConsoleHelpers.PromptIterations("Matches per tuning iteration?", 4_000);
        var (tuned, before, after) = ConsoleHelpers.RunWithProgress("Auto-tuning", matches, (counter, ct) =>
        {
            var result = CalibrationRunner.AutoTune(_session.Data, _session.Current, matches);
            counter.Add(matches);
            return result;
        });

        Ui.Header("Before");
        ParametersFormatter.PrintCalibration(before);
        Ui.Header("After");
        ParametersFormatter.PrintCalibration(after);

        if (Nav.Confirm("Apply the tuned parameters to Current?"))
        {
            tuned.Label = "Current";
            _session.Current = tuned;
            Ui.Success("Tuned parameters applied to Current.");
        }
    }
}
