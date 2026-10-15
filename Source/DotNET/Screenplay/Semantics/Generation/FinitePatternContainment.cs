// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal static class FinitePatternContainment
{
    internal static RuleConsistencyResult Check(GenerationPattern pattern, RegexExpression expression, bool complement = false)
    {
        try
        {
            var shape = pattern;
            if (shape.ChecksumWidth > 0) shape = shape with { Tokens = [.. shape.Tokens, new("0123456789", shape.ChecksumWidth, shape.ChecksumWidth)], Checksum = null };
            var generation = new GenerationPatternAutomaton(shape, null, false, new());
            var regex = new RegexDeterminization(new GlushkovAutomaton(expression));
            var start = (Generation: generation.Start, Regex: regex.Start);
            var pending = new Queue<(int Generation, int Regex, string Word)>();
            var reached = new HashSet<(int Generation, int Regex)> { start };
            pending.Enqueue((start.Generation, start.Regex, string.Empty));
            while (pending.TryDequeue(out var state))
            {
                if (generation.Accepts(state.Generation) && regex.Accepts(state.Regex) == complement) return RuleConsistencyResult.Refuted(state.Word);
                for (var character = ' '; character <= '~'; character++)
                {
                    var target = generation.Move(state.Generation, character);
                    if (generation.IsEmpty(target)) continue;
                    var next = (Generation: target, Regex: regex.Move(state.Regex, character));
                    if (reached.Contains(next)) continue;
                    if (reached.Count == RegexDeterminization.StateBudget) throw new RegexStateBudgetExceeded();
                    reached.Add(next);
                    pending.Enqueue((next.Generation, next.Regex, state.Word + character));
                }
            }

            return RuleConsistencyResult.Proven;
        }
        catch (RegexStateBudgetExceeded error)
        {
            return RuleConsistencyResult.Unproven(error.Message);
        }
        catch (GenerationSupportBudgetExceeded error)
        {
            return RuleConsistencyResult.Unproven(error.Message);
        }
    }
}
