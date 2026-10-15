// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;

namespace Cratis.Screenplay.Semantics.Generation;

internal static class GenerationPatternSupport
{
    internal static BigInteger Count(GenerationPattern pattern, DateOnly? date = null, GenerationWorkBudget? budget = null)
    {
        budget ??= new();
        if (pattern.HasDates && date is null) throw new InvalidSemanticContract("Entropy counting requires fixed UTC date segments.");
        var conditioned = pattern.Checksum == "mod11";
        var automaton = new GenerationPatternAutomaton(pattern, date, conditioned, budget);
        var start = (State: automaton.Start, Residue: 0, Phase: 0);
        var reached = new HashSet<(int State, int Residue, int Phase)> { start };
        var counts = new Dictionary<(int State, int Residue, int Phase), BigInteger> { [start] = BigInteger.One };
        var total = BigInteger.Zero;
        var maximum = pattern.MaximumLength - pattern.ChecksumWidth;
        for (var length = 0; length <= maximum && counts.Count > 0; length++)
        {
            var next = new Dictionary<(int State, int Residue, int Phase), BigInteger>();
            foreach (var (state, count) in counts)
            {
                if (automaton.Accepts(state.State) && (!conditioned || state.Residue != 1)) total += count;
                if (length == maximum) continue;
                for (var character = ' '; character <= '~'; character++)
                {
                    budget.Spend();
                    var target = automaton.Move(state.State, character);
                    if (automaton.IsEmpty(target)) continue;
                    var digit = conditioned && character is >= '0' and <= '9';
                    var key = (target, digit ? (state.Residue + ((character - '0') * (2 + state.Phase))) % 11 : state.Residue, digit ? (state.Phase + 1) % 6 : state.Phase);
                    if (!reached.Contains(key) && reached.Count == GenerationWorkBudget.StateLimit) throw new GenerationSupportBudgetExceeded("state budget (10000 distinct DFA/product states)");
                    reached.Add(key);
                    next[key] = next.GetValueOrDefault(key) + count;
                }
            }
            counts = next;
        }

        return total;
    }

    internal static bool Matches(GenerationPattern pattern, string value, DateTimeOffset? instant = null)
    {
        var width = pattern.ChecksumWidth;
        if (value.Length < pattern.MinimumLength || value.Length > pattern.MaximumLength) return false;
        if (width > 0 && !value[^width..].All(character => character is >= '0' and <= '9')) return false;
        var date = instant is null ? (DateOnly?)null : DateOnly.FromDateTime(instant.Value.UtcDateTime);
        var automaton = new GenerationPatternAutomaton(pattern, date, false, new());
        var state = automaton.Start;
        foreach (var character in value[..(value.Length - width)])
        {
            state = automaton.Move(state, character);
            if (automaton.IsEmpty(state)) return false;
        }

        return automaton.Accepts(state);
    }
}
