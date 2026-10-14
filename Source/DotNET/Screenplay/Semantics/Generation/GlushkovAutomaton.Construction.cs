// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed partial class GlushkovAutomaton
{
    static long PositionCount(RegexExpression expression)
    {
        var pending = new Stack<(RegexExpression Expression, long Copies)>();
        pending.Push((expression, 1));
        long count = 0;
        while (pending.TryPop(out var step))
        {
            if (step.Copies == 0) continue;
            if (step.Expression.Kind == "characters")
            {
                count += step.Copies;
                if (count > PositionBudget) return PositionBudget + 1;
            }
            else if (step.Expression.Kind == "concat" || step.Expression.Kind == "alternate")
            {
                pending.Push((step.Expression.Right!, step.Copies));
                pending.Push((step.Expression.Left!, step.Copies));
            }
            else if (step.Expression.Kind == "repeat")
            {
                var copies = step.Expression.Maximum ?? ((long)step.Expression.Minimum + 1);
                pending.Push((step.Expression.Left!, Math.Min(PositionBudget + 1, step.Copies * copies)));
            }
        }

        return count;
    }

    RegexPositions Build(RegexExpression expression)
    {
        var pending = new Stack<ConstructionStep>();
        var values = new Stack<RegexPositions>();
        pending.Push(new(expression, false));
        while (pending.TryPop(out var step))
        {
            if (step.Complete)
            {
                values.Push(Complete(step, values));
                continue;
            }

            switch (step.Expression.Kind)
            {
                case "empty": values.Push(new(true, [], [])); break;
                case "none": values.Push(new(false, [], [])); break;
                case "characters": values.Push(Leaf(step.Expression.Characters)); break;
                case "concat":
                case "alternate":
                    pending.Push(step with { Complete = true });
                    pending.Push(new(step.Expression.Right!, false));
                    pending.Push(new(step.Expression.Left!, false));
                    break;
                case "repeat":
                    var copies = PositionCount(step.Expression.Left!) == 0 ? 1 : step.Expression.Maximum ?? (step.Expression.Minimum + 1);
                    pending.Push(step with { Complete = true, Copies = copies });
                    for (var index = 0; index < copies; index++) pending.Push(new(step.Expression.Left!, false));
                    break;
                default: throw new InvalidSemanticContract("Unknown regular expression node.");
            }
        }

        return values.Pop();
    }

    RegexPositions Complete(ConstructionStep step, Stack<RegexPositions> values)
    {
        if (step.Expression.Kind == "repeat") return Repeat(step, values);
        var right = values.Pop();
        var left = values.Pop();

        return step.Expression.Kind == "concat" ? Concat(left, right) : Alternate(left, right);
    }

    RegexPositions Repeat(ConstructionStep step, Stack<RegexPositions> values)
    {
        var parts = new RegexPositions[step.Copies];
        for (var index = parts.Length - 1; index >= 0; index--) parts[index] = values.Pop();
        if (PositionCount(step.Expression.Left!) == 0) return parts[0] with { Nullable = step.Expression.Minimum == 0 || parts[0].Nullable };
        var result = new RegexPositions(true, [], []);
        for (var index = 0; index < parts.Length; index++)
        {
            var part = parts[index];
            if (step.Expression.Maximum is null && index == step.Expression.Minimum)
            {
                foreach (var position in part.Last) _follow[position].UnionWith(part.First);
            }

            result = Concat(result, index < step.Expression.Minimum ? part : part with { Nullable = true });
        }

        return result;
    }

    sealed record ConstructionStep(RegexExpression Expression, bool Complete, int Copies = 0);
}
