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

        var matches = new List<KnockoutMatchDef>
        {
            // Round of 32 (M73–M88), in bracket order.
            new(73, Stage.RoundOf32, R('A'), R('B'), "R32-1"),
            new(74, Stage.RoundOf32, W('E'), T('E'), "R32-2"),
            new(75, Stage.RoundOf32, W('F'), R('C'), "R32-3"),
            new(76, Stage.RoundOf32, W('C'), R('F'), "R32-4"),
            new(77, Stage.RoundOf32, W('I'), T('I'), "R32-5"),
            new(78, Stage.RoundOf32, R('E'), R('I'), "R32-6"),
            new(79, Stage.RoundOf32, W('A'), T('A'), "R32-7"),
            new(80, Stage.RoundOf32, W('L'), T('L'), "R32-8"),
            new(81, Stage.RoundOf32, W('D'), T('D'), "R32-9"),
            new(82, Stage.RoundOf32, W('G'), T('G'), "R32-10"),
            new(83, Stage.RoundOf32, R('K'), R('L'), "R32-11"),
            new(84, Stage.RoundOf32, W('H'), R('J'), "R32-12"),
            new(85, Stage.RoundOf32, W('B'), T('B'), "R32-13"),
            new(86, Stage.RoundOf32, W('J'), R('H'), "R32-14"),
            new(87, Stage.RoundOf32, W('K'), T('K'), "R32-15"),
            new(88, Stage.RoundOf32, R('D'), R('G'), "R32-16"),

            // Round of 16 (M89–M96).
            new(89, Stage.RoundOf16, Win(74), Win(77), "R16-1"),
            new(90, Stage.RoundOf16, Win(73), Win(75), "R16-2"),
            new(91, Stage.RoundOf16, Win(76), Win(78), "R16-3"),
            new(92, Stage.RoundOf16, Win(79), Win(80), "R16-4"),
            new(93, Stage.RoundOf16, Win(85), Win(84), "R16-5"),
            new(94, Stage.RoundOf16, Win(81), Win(82), "R16-6"),
            new(95, Stage.RoundOf16, Win(86), Win(88), "R16-7"),
            new(96, Stage.RoundOf16, Win(87), Win(83), "R16-8"),

            // Quarter-finals (M97–M100).
            new(97, Stage.QuarterFinal, Win(89), Win(90), "QF-1"),
            new(98, Stage.QuarterFinal, Win(93), Win(94), "QF-2"),
            new(99, Stage.QuarterFinal, Win(91), Win(92), "QF-3"),
            new(100, Stage.QuarterFinal, Win(95), Win(96), "QF-4"),

            // Semi-finals (M101–M102).
            new(101, Stage.SemiFinal, Win(97), Win(98), "SF-1"),
            new(102, Stage.SemiFinal, Win(99), Win(100), "SF-2"),

            // Third-place playoff (M103) and Final (M104).
            new(103, Stage.ThirdPlacePlayoff, Lose(101), Lose(102), "3rd Place"),
            new(104, Stage.Final, Win(101), Win(102), "Final"),
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
