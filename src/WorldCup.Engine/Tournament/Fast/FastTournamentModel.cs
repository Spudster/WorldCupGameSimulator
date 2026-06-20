using WorldCup.Data.Models;
using WorldCup.Engine.Parameters;

namespace WorldCup.Engine.Tournament.Fast;

/// <summary>How one side of a knockout match is sourced, in index form for the fast path.</summary>
internal readonly struct FastFeeder
{
    public const byte GroupWinner = 0;
    public const byte GroupRunnerUp = 1;
    public const byte GroupThird = 2;
    public const byte MatchWinner = 3;
    public const byte MatchLoser = 4;

    public FastFeeder(byte kind, int arg)
    {
        Kind = kind;
        Arg = arg;
    }

    public byte Kind { get; }

    /// <summary>Group index (for group slots) or match index 0..31 (for winner/loser feeders).</summary>
    public int Arg { get; }
}

/// <summary>
/// Immutable, index-based snapshot of a tournament built once and shared (read-only) across all
/// Monte Carlo worker threads. All entities are integer indices so the hot loop is allocation-free.
/// </summary>
internal sealed class FastTournamentModel
{
    public const int GroupCount = 12;
    public const int TeamsPerGroup = 4;
    public const int KnockoutMatches = 32;

    // Local round-robin pattern (indices into a group's four teams, ordered by pot).
    public static readonly (int Home, int Away)[] GroupPattern =
    {
        (0, 1), (2, 3), (0, 2), (3, 1), (0, 3), (1, 2),
    };

    public required int TeamCount { get; init; }
    public required double[] Strength { get; init; }
    public required bool[] IsHost { get; init; }
    public required string[] Codes { get; init; }
    public required int[][] GroupTeams { get; init; } // [12][4] global team indices, pot order
    public required char[] GroupLetters { get; init; }

    // Locked group results: per group per local match (g*6 + m).
    public required bool[] LockedFlag { get; init; }
    public required int[] LockedHomeGoals { get; init; }
    public required int[] LockedAwayGoals { get; init; }

    // Knockout structure (index 0..31 maps to FIFA matches 73..104).
    public required Stage[] MatchStage { get; init; }
    public required int[] MatchStageRank { get; init; }
    public required FastFeeder[] TopFeeder { get; init; }
    public required FastFeeder[] BottomFeeder { get; init; }
    public required int FinalIndex { get; init; }
    public required int ThirdPlaceIndex { get; init; }

    // Third-place assignment.
    public required int[] WinnerGroupsForThirds { get; init; } // 8 group indices
    public required int[][] ThirdEligibleGroups { get; init; } // aligned with WinnerGroupsForThirds

    public static FastTournamentModel Build(
        TournamentData data, SimulationParameters parameters, IReadOnlyList<PlayedResult>? locked)
    {
        var teams = data.Teams.OrderBy(t => t.Group).ThenBy(t => t.Pot).ToList();
        int n = teams.Count;
        var indexByCode = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
        for (int i = 0; i < n; i++)
        {
            indexByCode[teams[i].Code] = i;
        }

        var groupLetters = data.Groups.ToArray();
        var groupIndex = new Dictionary<char, int>();
        for (int g = 0; g < groupLetters.Length; g++)
        {
            groupIndex[groupLetters[g]] = g;
        }

        var strength = new double[n];
        var isHost = new bool[n];
        var codes = new string[n];
        for (int i = 0; i < n; i++)
        {
            strength[i] = parameters.EffectiveStrength(teams[i]);
            codes[i] = teams[i].Code;
            isHost[i] = data.Metadata.Hosts.Contains(teams[i].Name, StringComparer.OrdinalIgnoreCase);
        }

        var groupTeams = new int[GroupCount][];
        for (int g = 0; g < GroupCount; g++)
        {
            groupTeams[g] = data.TeamsInGroup(groupLetters[g]).Select(t => indexByCode[t.Code]).ToArray();
        }

        // Locked results matched by unordered pair.
        var lockedByPair = new Dictionary<(string, string), PlayedResult>();
        if (locked is not null)
        {
            foreach (var r in locked)
            {
                lockedByPair[PairKey(r.HomeCode, r.AwayCode)] = r;
            }
        }

        var lockedFlag = new bool[GroupCount * 6];
        var lockedHg = new int[GroupCount * 6];
        var lockedAg = new int[GroupCount * 6];
        for (int g = 0; g < GroupCount; g++)
        {
            for (int m = 0; m < 6; m++)
            {
                var (la, lb) = GroupPattern[m];
                int home = groupTeams[g][la];
                int away = groupTeams[g][lb];
                if (lockedByPair.TryGetValue(PairKey(codes[home], codes[away]), out var pr))
                {
                    bool same = string.Equals(pr.HomeCode, codes[home], StringComparison.OrdinalIgnoreCase);
                    int idx = g * 6 + m;
                    lockedFlag[idx] = true;
                    lockedHg[idx] = same ? pr.HomeGoals : pr.AwayGoals;
                    lockedAg[idx] = same ? pr.AwayGoals : pr.HomeGoals;
                }
            }
        }

        // Knockout structure.
        var bracket = data.Bracket;
        var matchStage = new Stage[KnockoutMatches];
        var matchStageRank = new int[KnockoutMatches];
        var top = new FastFeeder[KnockoutMatches];
        var bottom = new FastFeeder[KnockoutMatches];
        int finalIdx = -1, thirdIdx = -1;
        foreach (var def in bracket.Matches)
        {
            int idx = def.Id - 73;
            matchStage[idx] = def.Stage;
            matchStageRank[idx] = Stages.Rank(def.Stage);
            top[idx] = Encode(def.Top, groupIndex);
            bottom[idx] = Encode(def.Bottom, groupIndex);
            if (def.Stage == Stage.Final)
            {
                finalIdx = idx;
            }
            else if (def.Stage == Stage.ThirdPlacePlayoff)
            {
                thirdIdx = idx;
            }
        }

        var winnerGroups = bracket.ThirdPlaceWinnerGroups.Select(c => groupIndex[c]).ToArray();
        var eligible = bracket.ThirdPlaceWinnerGroups
            .Select(c => bracket.ThirdPlaceEligibleGroups[c].Select(e => groupIndex[e]).ToArray())
            .ToArray();

        return new FastTournamentModel
        {
            TeamCount = n,
            Strength = strength,
            IsHost = isHost,
            Codes = codes,
            GroupTeams = groupTeams,
            GroupLetters = groupLetters,
            LockedFlag = lockedFlag,
            LockedHomeGoals = lockedHg,
            LockedAwayGoals = lockedAg,
            MatchStage = matchStage,
            MatchStageRank = matchStageRank,
            TopFeeder = top,
            BottomFeeder = bottom,
            FinalIndex = finalIdx,
            ThirdPlaceIndex = thirdIdx,
            WinnerGroupsForThirds = winnerGroups,
            ThirdEligibleGroups = eligible,
        };
    }

    private static FastFeeder Encode(Feeder feeder, Dictionary<char, int> groupIndex) => feeder.Kind switch
    {
        FeederKind.MatchWinner => new FastFeeder(FastFeeder.MatchWinner, feeder.MatchId - 73),
        FeederKind.MatchLoser => new FastFeeder(FastFeeder.MatchLoser, feeder.MatchId - 73),
        FeederKind.GroupSlot => feeder.Slot.Kind switch
        {
            SlotSpecKind.Winner => new FastFeeder(FastFeeder.GroupWinner, groupIndex[feeder.Slot.Group]),
            SlotSpecKind.RunnerUp => new FastFeeder(FastFeeder.GroupRunnerUp, groupIndex[feeder.Slot.Group]),
            SlotSpecKind.ThirdForWinner => new FastFeeder(FastFeeder.GroupThird, groupIndex[feeder.Slot.Group]),
            _ => throw new InvalidOperationException(),
        },
        _ => throw new InvalidOperationException(),
    };

    private static (string, string) PairKey(string a, string b) =>
        string.CompareOrdinal(a, b) <= 0 ? (a, b) : (b, a);
}
