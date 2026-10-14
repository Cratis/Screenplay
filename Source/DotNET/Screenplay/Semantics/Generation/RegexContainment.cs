// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal static class RegexContainment
{
    internal const string AmbiguousPatternReason = "ambiguous pattern; the runtime regex may backtrack";

    internal static RuleConsistencyResult Check(GenerationShape shape, RegexExpression expression, bool complement = false, bool requireDeterministic = false)
    {
        try
        {
            var positions = shape.Positions;
            var positionsAutomaton = new GlushkovAutomaton(expression);
            var automaton = new RegexDeterminization(positionsAutomaton);
            var layers = Walk(positions, automaton);
            var bad = Backward(positions, automaton, layers, complement);

            if (bad[0].Contains(automaton.Start)) return RuleConsistencyResult.Refuted(Witness(positions, automaton, bad));

            return requireDeterministic && !positionsAutomaton.IsDeterministic
                ? RuleConsistencyResult.Unproven(AmbiguousPatternReason)
                : RuleConsistencyResult.Proven;
        }
        catch (RegexStateBudgetExceeded error)
        {
            return RuleConsistencyResult.Unproven(error.Message);
        }
    }

    static HashSet<int>[] Walk(string[] positions, RegexDeterminization automaton)
    {
        var layers = new HashSet<int>[positions.Length + 1];
        layers[0] = [automaton.Start];
        for (var index = 0; index < positions.Length; index++)
        {
            var next = new HashSet<int>();
            foreach (var state in layers[index])
            {
                foreach (var character in positions[index]) next.Add(automaton.Move(state, character));
            }

            layers[index + 1] = next;
        }

        return layers;
    }

    static HashSet<int>[] Backward(string[] positions, RegexDeterminization automaton, HashSet<int>[] layers, bool complement)
    {
        var bad = new HashSet<int>[layers.Length];
        bad[^1] = [.. layers[^1].Where(state => automaton.Accepts(state) == complement)];
        for (var index = positions.Length - 1; index >= 0; index--)
        {
            bad[index] = [.. layers[index].Where(state => positions[index].Any(character => bad[index + 1].Contains(automaton.Move(state, character))))];
        }

        return bad;
    }

    static string Witness(string[] positions, RegexDeterminization automaton, HashSet<int>[] bad)
    {
        var result = new char[positions.Length];
        var state = automaton.Start;
        for (var index = 0; index < positions.Length; index++)
        {
            result[index] = positions[index].First(character => bad[index + 1].Contains(automaton.Move(state, character)));
            state = automaton.Move(state, result[index]);
        }

        return new(result);
    }
}
