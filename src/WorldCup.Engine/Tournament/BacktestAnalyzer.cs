using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;
using WorldCup.Engine.Simulation;

namespace WorldCup.Engine.Tournament;

/// <summary>
/// One backtested match: the model's pre-match win/draw/loss probabilities scored against the
/// real result that was actually played.
/// </summary>
/// <param name="HomeCode">Home team 3-letter code.</param>
/// <param name="HomeName">Home team display name.</param>
/// <param name="AwayCode">Away team 3-letter code.</param>
/// <param name="AwayName">Away team display name.</param>
/// <param name="HomeGoals">Goals the home team actually scored.</param>
/// <param name="AwayGoals">Goals the away team actually scored.</param>
/// <param name="PHome">Pre-match probability of a home win.</param>
/// <param name="PDraw">Pre-match probability of a draw.</param>
/// <param name="PAway">Pre-match probability of an away win.</param>
/// <param name="Predicted">The model's most-likely outcome ("Home win" / "Draw" / "Away win").</param>
/// <param name="Actual">The outcome that actually occurred ("Home win" / "Draw" / "Away win").</param>
/// <param name="Hit">True when <paramref name="Predicted"/> matches <paramref name="Actual"/>.</param>
public sealed record BacktestMatchRow(
    string HomeCode, string HomeName, string AwayCode, string AwayName, int HomeGoals, int AwayGoals,
    double PHome, double PDraw, double PAway, string Predicted, string Actual, bool Hit);

/// <summary>
/// One reliability-diagram bin: how the model's predicted probabilities in a probability band
/// compared with the frequency the predicted outcomes actually occurred.
/// </summary>
/// <param name="LowEdge">Inclusive lower edge of the probability band.</param>
/// <param name="HighEdge">Upper edge of the probability band.</param>
/// <param name="Count">Number of (predicted-probability, occurred) pairs that fell in the band.</param>
/// <param name="PredictedAvg">Mean predicted probability across the pairs in the band.</param>
/// <param name="ObservedFreq">Fraction of those pairs whose outcome actually occurred.</param>
public sealed record CalibrationBin(double LowEdge, double HighEdge, int Count, double PredictedAvg, double ObservedFreq);

/// <summary>
/// The aggregate scorecard for a set of backtested matches: proper scoring rules, hit rate, the
/// per-match detail and the calibration (reliability) bins.
/// </summary>
/// <param name="Matches">Number of matches scored.</param>
/// <param name="BrierScore">Average 3-class Brier score (0 = perfect, lower is better).</param>
/// <param name="LogLoss">Average negative log-likelihood of the actual outcome (lower is better).</param>
/// <param name="FavouriteHitRate">Fraction of matches whose most-likely outcome actually occurred.</param>
/// <param name="Rows">Per-match detail.</param>
/// <param name="Calibration">The non-empty reliability-diagram bins.</param>
public sealed record BacktestReport(
    int Matches, double BrierScore, double LogLoss, double FavouriteHitRate,
    IReadOnlyList<BacktestMatchRow> Rows, IReadOnlyList<CalibrationBin> Calibration);

/// <summary>
/// Scores the model's pre-match win/draw/loss predictions against the real results already played,
/// to report accuracy (favourite hit rate) and calibration (Brier score, log loss, reliability bins).
/// </summary>
public static class BacktestAnalyzer
{
    private const string HomeWinLabel = "Home win";
    private const string DrawLabel = "Draw";
    private const string AwayWinLabel = "Away win";

    private static readonly double[] BinEdges = { 0.0, 0.2, 0.4, 0.6, 0.8, 1.0 };

    /// <summary>
    /// Score each played result against the model's pre-match outcome probabilities and aggregate
    /// the accuracy and calibration metrics. Results referencing teams not present in
    /// <paramref name="data"/> are skipped. Safe on an empty <paramref name="playedResults"/>
    /// (returns zeros and empty lists).
    /// </summary>
    /// <param name="data">The tournament (teams, hosts) the predictions are made over.</param>
    /// <param name="playedResults">The real results to score the model against.</param>
    /// <param name="p">The simulation parameters defining team strengths and the goal model.</param>
    /// <returns>The aggregate backtest report.</returns>
    public static BacktestReport Analyze(TournamentData data, IReadOnlyList<PlayedResult> playedResults, SimulationParameters p)
    {
        // Codes of the host teams (matched on Name, case-insensitive) — host matches are not played at a neutral venue.
        var hostNames = new HashSet<string>(data.Metadata.Hosts, StringComparer.OrdinalIgnoreCase);
        var hostCodes = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        foreach (var team in data.Teams)
        {
            if (hostNames.Contains(team.Name))
            {
                hostCodes.Add(team.Code);
            }
        }

        double sigma = Math.Sqrt(
            p.Global.UpsetVariance * p.Global.UpsetVariance +
            p.Global.MatchTempoVariance * p.Global.MatchTempoVariance);

        var rows = new List<BacktestMatchRow>();
        double brierSum = 0.0;
        double logLossSum = 0.0;
        int hits = 0;

        // Calibration pairs: one (predictedProb, occurred) per outcome class, accumulated across all matches.
        var calibProbs = new List<double>();
        var calibOccurred = new List<double>();

        foreach (var r in playedResults)
        {
            if (!data.TryGetTeam(r.HomeCode, out var home) || !data.TryGetTeam(r.AwayCode, out var away))
            {
                continue;
            }

            bool neutral = !hostCodes.Contains(home.Code);
            var (lh, la) = MatchModel.ExpectedGoals(
                p.EffectiveStrength(home), p.EffectiveStrength(away), p.Global, neutral);
            var grid = MatchModel.ScoreGridWithForm(lh, la, p.Global.DrawCoupling, sigma);
            var (pH, pD, pA) = MatchModel.OutcomeProbabilities(grid);

            // One-hot actual outcome.
            double aH = r.HomeGoals > r.AwayGoals ? 1.0 : 0.0;
            double aA = r.AwayGoals > r.HomeGoals ? 1.0 : 0.0;
            double aD = (aH == 0.0 && aA == 0.0) ? 1.0 : 0.0;

            string actual = aH > 0.0 ? HomeWinLabel : aA > 0.0 ? AwayWinLabel : DrawLabel;
            string predicted = ArgMaxLabel(pH, pD, pA);
            bool hit = predicted == actual;

            // 3-class Brier score.
            brierSum += (pH - aH) * (pH - aH) + (pD - aD) * (pD - aD) + (pA - aA) * (pA - aA);

            // Log loss on the probability assigned to the actual class.
            double pActual = aH > 0.0 ? pH : aA > 0.0 ? pA : pD;
            logLossSum += -Math.Log(Math.Clamp(pActual, 1e-9, 1.0));

            if (hit)
            {
                hits++;
            }

            calibProbs.Add(pH);
            calibOccurred.Add(aH);
            calibProbs.Add(pD);
            calibOccurred.Add(aD);
            calibProbs.Add(pA);
            calibOccurred.Add(aA);

            rows.Add(new BacktestMatchRow(
                home.Code, home.Name, away.Code, away.Name, r.HomeGoals, r.AwayGoals,
                pH, pD, pA, predicted, actual, hit));
        }

        int matches = rows.Count;
        double brier = matches > 0 ? brierSum / matches : 0.0;
        double logLoss = matches > 0 ? logLossSum / matches : 0.0;
        double favouriteHitRate = matches > 0 ? (double)hits / matches : 0.0;
        var calibration = BuildCalibration(calibProbs, calibOccurred);

        return new BacktestReport(matches, brier, logLoss, favouriteHitRate, rows, calibration);
    }

    /// <summary>The label for the most-likely of the three outcomes (ties resolve home &gt; draw &gt; away).</summary>
    private static string ArgMaxLabel(double pH, double pD, double pA)
    {
        if (pH >= pD && pH >= pA)
        {
            return HomeWinLabel;
        }

        return pD >= pA ? DrawLabel : AwayWinLabel;
    }

    /// <summary>
    /// Bin the (predicted-probability, occurred) pairs into five fixed bands and summarise each
    /// non-empty band. The top band is closed on the right so a predicted probability of exactly 1.0
    /// is included.
    /// </summary>
    private static IReadOnlyList<CalibrationBin> BuildCalibration(
        IReadOnlyList<double> probs, IReadOnlyList<double> occurred)
    {
        int bins = BinEdges.Length - 1;
        var counts = new int[bins];
        var probSums = new double[bins];
        var occurredSums = new double[bins];

        for (int i = 0; i < probs.Count; i++)
        {
            int bin = BinIndex(probs[i], bins);
            counts[bin]++;
            probSums[bin] += probs[i];
            occurredSums[bin] += occurred[i];
        }

        var result = new List<CalibrationBin>();
        for (int b = 0; b < bins; b++)
        {
            if (counts[b] == 0)
            {
                continue;
            }

            result.Add(new CalibrationBin(
                BinEdges[b], BinEdges[b + 1], counts[b],
                probSums[b] / counts[b], occurredSums[b] / counts[b]));
        }

        return result;
    }

    /// <summary>Index of the bin a probability falls into; the final bin is closed on the right edge.</summary>
    private static int BinIndex(double prob, int bins)
    {
        for (int b = 0; b < bins; b++)
        {
            if (prob < BinEdges[b + 1])
            {
                return b;
            }
        }

        return bins - 1;
    }
}
