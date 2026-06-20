using Spectre.Console;

namespace WorldCup.Reporting;

/// <summary>Shared console-formatting helpers (colours, headers, panels, tables, number formatting).</summary>
public static class Ui
{
    // Semantic colour names (markup strings).
    public const string Accent = "deepskyblue1";
    public const string Good = "green3";
    public const string Warn = "gold1";
    public const string Bad = "red3";
    public const string Muted = "grey";
    public const string Gold = "gold1"; // headline / trophy highlight

    // Matching Spectre colours (for borders).
    public static readonly Color AccentColor = Color.DeepSkyBlue1;
    public static readonly Color GoldColor = Color.Gold1;
    public static readonly Color GoodColor = Color.Green3;
    public static readonly Color LineColor = Color.Grey42; // dim table/panel border

    public static void Header(string title, string? subtitle = null)
    {
        AnsiConsole.Write(new Rule($"[bold {Accent}]▌ {Markup.Escape(title)}[/]").RuleStyle(Accent).LeftJustified());
        if (subtitle is not null)
        {
            AnsiConsole.MarkupLine($"  [{Muted}]{Markup.Escape(subtitle)}[/]");
        }
    }

    /// <summary>A table with the one consistent house style (rounded, dim border). Pass a styled title.</summary>
    public static Table Table(string? titleMarkup = null)
    {
        var t = new Table().Border(TableBorder.Rounded).BorderColor(LineColor);
        if (!string.IsNullOrEmpty(titleMarkup))
        {
            t.Title(titleMarkup!);
        }

        return t;
    }

    /// <summary>A headline panel in the one consistent house style (rounded, coloured border, padded).</summary>
    public static void Hero(string markup, string? header = null, Color? border = null)
    {
        AnsiConsole.Write(new Panel(new Markup(markup))
        {
            Header = string.IsNullOrEmpty(header) ? null : new PanelHeader($" {header} "),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(border ?? AccentColor),
            Padding = new Padding(2, 1, 2, 1),
        });
    }

    public static void Banner()
    {
        AnsiConsole.Write(new FigletText("World Cup").Color(GoldColor).Centered());
        AnsiConsole.Write(new Rule(
                $"[bold {Accent}]⚽ 2026 Monte Carlo Simulator[/]   [{Muted}]·  Canada · Mexico · USA[/]")
            .RuleStyle(LineColor).Centered());
        AnsiConsole.WriteLine();
    }

    /// <summary>A friendly sign-off panel shown when leaving the app.</summary>
    public static void Goodbye()
    {
        AnsiConsole.WriteLine();
        AnsiConsole.Write(new Rule($"[bold {Gold}]⚽ Thanks for playing — see you at the final! 🏆[/]")
            .RuleStyle(GoldColor).Centered());
        AnsiConsole.WriteLine();
    }

    /// <summary>When a prediction is produced, e.g. "Friday, 19 June 2026 at 14:32" — shown on every report.</summary>
    public static string PredictedOnText() => DateTime.Now.ToString("dddd, d MMMM yyyy 'at' HH:mm");

    /// <summary>Prints a dim "🔮 This was predicted on …" stamp under a standalone report's header.</summary>
    public static void PredictedOn() =>
        AnsiConsole.MarkupLine($"[{Muted}]🔮 This was predicted on {Markup.Escape(PredictedOnText())}[/]");

    public static void RunConfig(string scenario, string parameterLabel, ulong seed, string fidelity, long? n = null, bool showPredictedOn = true)
    {
        var grid = new Grid().AddColumn().AddColumn();
        grid.AddRow($"[{Muted}]Scenario[/]", $"[bold {Accent}]{Markup.Escape(scenario)}[/]");
        grid.AddRow($"[{Muted}]Parameters[/]", Markup.Escape(parameterLabel));
        grid.AddRow($"[{Muted}]Fidelity[/]", Markup.Escape(fidelity));
        if (n is not null)
        {
            grid.AddRow($"[{Muted}]Iterations (N)[/]", $"[{Gold}]{n:N0}[/]");
        }

        grid.AddRow($"[{Muted}]RNG seed[/]", $"[{Muted}]{seed}[/]");
        if (showPredictedOn)
        {
            grid.AddRow($"[{Muted}]Predicted on[/]", $"[{Muted}]{Markup.Escape(PredictedOnText())}[/]");
        }

        AnsiConsole.Write(new Panel(grid)
        {
            Header = new PanelHeader($"[bold {Gold}] ⚙ Run configuration [/]"),
            Border = BoxBorder.Rounded,
            BorderStyle = new Style(LineColor),
            Padding = new Padding(2, 0, 2, 0),
        });
    }

    public static string Pct(double fraction) => $"{fraction * 100:0.0}%";

    public static string Pct2(double fraction) => $"{fraction * 100:0.00}%";

    /// <summary>A probability rendered with a heat colour (green = likely → grey = remote).</summary>
    public static string Heat(double fraction)
    {
        string c = fraction >= 0.50 ? Good : fraction >= 0.25 ? Warn : fraction >= 0.08 ? Accent : Muted;
        return $"[{c}]{Pct(fraction)}[/]";
    }

    public static string Throughput(double simsPerSec)
    {
        if (simsPerSec >= 1_000_000)
        {
            return $"{simsPerSec / 1_000_000:0.00}M sims/s";
        }

        if (simsPerSec >= 1_000)
        {
            return $"{simsPerSec / 1_000:0.0}K sims/s";
        }

        return $"{simsPerSec:0} sims/s";
    }

    public static void Info(string message) => AnsiConsole.MarkupLine($"[{Accent}]ℹ[/] {Markup.Escape(message)}");

    public static void Success(string message) => AnsiConsole.MarkupLine($"[{Good}]✓[/] {Markup.Escape(message)}");

    public static void Warning(string message) => AnsiConsole.MarkupLine($"[{Warn}]![/] {Markup.Escape(message)}");

    public static void Blank() => AnsiConsole.WriteLine();
}
