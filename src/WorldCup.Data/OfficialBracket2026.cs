using WorldCup.Data.Models;

namespace WorldCup.Data;

/// <summary>
/// Builds the official 2026 FIFA World Cup knockout bracket: the fixed R32 slot pairings and the
/// full R16 → Final tree, exactly as published by FIFA (match numbers 73–104). The mapping of the
/// eight best third-placed teams to their R32 slots is governed by the eligibility table here and
/// resolved at simulation time (the full 495-row Annex C table is approximated by a deterministic
/// matching that honours these eligibility sets — see the engine's bracket resolver).
/// </summary>
public static class OfficialBracket2026
{
    public static BracketDefinition Build()
    {
        Feeder W(char g) => Feeder.FromSlot(SlotSpec.Winner(g));
        Feeder R(char g) => Feeder.FromSlot(SlotSpec.RunnerUp(g));
        Feeder T(char g) => Feeder.FromSlot(SlotSpec.ThirdForWinner(g));
        Feeder Win(int id) => Feeder.WinnerOf(id);
        Feeder Lose(int id) => Feeder.LoserOf(id);

        // Official 2026 knockout schedule (kickoffs in UTC). Times are set within each match's
        // host-local (Americas) calendar day so day-grouping is stable across US/European time zones:
        // R32 runs Jun 28 (M73) then 3/day through Jul 3; R16 Jul 4–7; QF Jul 9–11; SF Jul 14–15;
        // third-place Jul 18; Final Jul 19 (matching the metadata's final date).
        static DateTime D(int month, int day, int hourUtc) => new(2026, month, day, hourUtc, 0, 0, DateTimeKind.Utc);

        var matches = new List<KnockoutMatchDef>
        {
            // Round of 32 (M73–M88), in bracket order.
            new(73, Stage.RoundOf32, R('A'), R('B'), "R32-1", D(6, 28, 20)),
            new(74, Stage.RoundOf32, W('E'), T('E'), "R32-2", D(6, 29, 16)),
            new(75, Stage.RoundOf32, W('F'), R('C'), "R32-3", D(6, 29, 19)),
            new(76, Stage.RoundOf32, W('C'), R('F'), "R32-4", D(6, 29, 22)),
            new(77, Stage.RoundOf32, W('I'), T('I'), "R32-5", D(6, 30, 16)),
            new(78, Stage.RoundOf32, R('E'), R('I'), "R32-6", D(6, 30, 19)),
            new(79, Stage.RoundOf32, W('A'), T('A'), "R32-7", D(6, 30, 22)),
            new(80, Stage.RoundOf32, W('L'), T('L'), "R32-8", D(7, 1, 16)),
            new(81, Stage.RoundOf32, W('D'), T('D'), "R32-9", D(7, 1, 19)),
            new(82, Stage.RoundOf32, W('G'), T('G'), "R32-10", D(7, 1, 22)),
            new(83, Stage.RoundOf32, R('K'), R('L'), "R32-11", D(7, 2, 16)),
            new(84, Stage.RoundOf32, W('H'), R('J'), "R32-12", D(7, 2, 19)),
            new(85, Stage.RoundOf32, W('B'), T('B'), "R32-13", D(7, 2, 22)),
            new(86, Stage.RoundOf32, W('J'), R('H'), "R32-14", D(7, 3, 16)),
            new(87, Stage.RoundOf32, W('K'), T('K'), "R32-15", D(7, 3, 19)),
            new(88, Stage.RoundOf32, R('D'), R('G'), "R32-16", D(7, 3, 22)),

            // Round of 16 (M89–M96).
            new(89, Stage.RoundOf16, Win(74), Win(77), "R16-1", D(7, 4, 18)),
            new(90, Stage.RoundOf16, Win(73), Win(75), "R16-2", D(7, 4, 21)),
            new(91, Stage.RoundOf16, Win(76), Win(78), "R16-3", D(7, 5, 18)),
            new(92, Stage.RoundOf16, Win(79), Win(80), "R16-4", D(7, 5, 21)),
            new(93, Stage.RoundOf16, Win(85), Win(84), "R16-5", D(7, 6, 18)),
            new(94, Stage.RoundOf16, Win(81), Win(82), "R16-6", D(7, 6, 21)),
            new(95, Stage.RoundOf16, Win(86), Win(88), "R16-7", D(7, 7, 18)),
            new(96, Stage.RoundOf16, Win(87), Win(83), "R16-8", D(7, 7, 21)),

            // Quarter-finals (M97–M100).
            new(97, Stage.QuarterFinal, Win(89), Win(90), "QF-1", D(7, 9, 20)),
            new(98, Stage.QuarterFinal, Win(93), Win(94), "QF-2", D(7, 10, 20)),
            new(99, Stage.QuarterFinal, Win(91), Win(92), "QF-3", D(7, 11, 18)),
            new(100, Stage.QuarterFinal, Win(95), Win(96), "QF-4", D(7, 11, 21)),

            // Semi-finals (M101–M102).
            new(101, Stage.SemiFinal, Win(97), Win(98), "SF-1", D(7, 14, 20)),
            new(102, Stage.SemiFinal, Win(99), Win(100), "SF-2", D(7, 15, 20)),

            // Third-place playoff (M103) and Final (M104).
            new(103, Stage.ThirdPlacePlayoff, Lose(101), Lose(102), "3rd Place", D(7, 18, 20)),
            new(104, Stage.Final, Win(101), Win(102), "Final", D(7, 19, 19)),
        };

        var winnerGroups = new[] { 'E', 'I', 'A', 'L', 'D', 'G', 'B', 'K' };

        var eligible = new Dictionary<char, IReadOnlyList<char>>
        {
            ['E'] = new[] { 'A', 'B', 'C', 'D', 'F' },
            ['I'] = new[] { 'C', 'D', 'F', 'G', 'H' },
            ['A'] = new[] { 'C', 'E', 'F', 'H', 'I' },
            ['L'] = new[] { 'E', 'H', 'I', 'J', 'K' },
            ['D'] = new[] { 'B', 'E', 'F', 'I', 'J' },
            ['G'] = new[] { 'A', 'E', 'H', 'I', 'J' },
            ['B'] = new[] { 'E', 'F', 'G', 'I', 'J' },
            ['K'] = new[] { 'D', 'E', 'I', 'J', 'L' },
        };

        return new BracketDefinition(matches, winnerGroups, eligible);
    }
}
