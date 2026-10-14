// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed class GlushkovAutomaton
{
    // Counts character positions after repeat unrolling, excluding the synthetic start position.
    // Built-in shapes can exceed 1024 characters; this independent NFA bound precedes DFA construction.
    internal const int PositionBudget = 2048;

    readonly List<string> _characters = [string.Empty];
    readonly List<HashSet<int>> _follow = [[]];
    readonly HashSet<int> _final;

    internal GlushkovAutomaton(RegexExpression expression)
    {
        if (PositionCount(expression) > PositionBudget) throw new RegexStateBudgetExceeded("position budget exceeded");
        var positions = Build(expression);
        _follow[0] = positions.First;
        _final = positions.Last;
        if (positions.Nullable) _final.Add(0);
    }

    internal bool Accepts(int[] state) => state.Any(_final.Contains);

    internal int[] Move(int[] state, char character)
    {
        var next = new HashSet<int>();
        foreach (var position in state)
        {
            foreach (var target in _follow[position])
            {
                if (_characters[target].Contains(character, StringComparison.Ordinal)) next.Add(target);
            }
        }

        return [.. next.Order()];
    }

    static long PositionCount(RegexExpression expression) => expression.Kind switch
    {
        "characters" => 1,
        "alternate" or "concat" => Math.Min(PositionBudget + 1, PositionCount(expression.Left!) + PositionCount(expression.Right!)),
        "repeat" => Math.Min(PositionBudget + 1, PositionCount(expression.Left!) * (expression.Maximum ?? ((long)expression.Minimum + 1))),
        _ => 0
    };

    static RegexPositions Alternate(RegexPositions left, RegexPositions right) => new(left.Nullable || right.Nullable, [.. left.First, .. right.First], [.. left.Last, .. right.Last]);

    RegexPositions Build(RegexExpression expression) => expression.Kind switch
    {
        "empty" => new(true, [], []),
        "none" => new(false, [], []),
        "characters" => Leaf(expression.Characters),
        "alternate" => Alternate(Build(expression.Left!), Build(expression.Right!)),
        "concat" => Concat(Build(expression.Left!), Build(expression.Right!)),
        "repeat" => Repeat(expression),
        _ => throw new InvalidSemanticContract("Unknown regular expression node.")
    };

    RegexPositions Leaf(string characters)
    {
        if (_characters.Count > PositionBudget) throw new RegexStateBudgetExceeded("position budget exceeded");
        var position = _characters.Count;
        _characters.Add(characters);
        _follow.Add([]);

        return new(false, [position], [position]);
    }

    RegexPositions Concat(RegexPositions left, RegexPositions right)
    {
        foreach (var position in left.Last) _follow[position].UnionWith(right.First);

        return new(left.Nullable && right.Nullable, left.Nullable ? [.. left.First, .. right.First] : left.First, right.Nullable ? [.. left.Last, .. right.Last] : right.Last);
    }

    RegexPositions Repeat(RegexExpression expression)
    {
        if (PositionCount(expression.Left!) == 0)
        {
            var empty = Build(expression.Left!);
            return empty with { Nullable = expression.Minimum == 0 || empty.Nullable };
        }

        var result = new RegexPositions(true, [], []);
        for (var index = 0; index < expression.Minimum; index++) result = Concat(result, Build(expression.Left!));
        if (expression.Maximum is null)
        {
            var tail = Build(expression.Left!);
            foreach (var position in tail.Last) _follow[position].UnionWith(tail.First);
            result = Concat(result, tail with { Nullable = true });
        }
        else
        {
            for (var index = expression.Minimum; index < expression.Maximum; index++) result = Concat(result, Build(expression.Left!) with { Nullable = true });
        }

        return result;
    }
}
