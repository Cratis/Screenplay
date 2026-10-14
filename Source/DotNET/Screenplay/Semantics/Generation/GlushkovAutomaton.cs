// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Generation;

internal sealed class GlushkovAutomaton
{
    readonly List<string> _characters = [string.Empty];
    readonly List<HashSet<int>> _follow = [[]];
    readonly HashSet<int> _final;

    internal GlushkovAutomaton(RegexExpression expression)
    {
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
