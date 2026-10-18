// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal sealed partial class MatchAmbiguityGraph
{
    internal const int PositionLimit = 256;
    internal const int WorkLimit = 500000;
    readonly List<MatchAmbiguityNode> _positions = [new("empty", 0, 0, [])];
    readonly List<Edge> _edges = [];
    readonly Positions _root;
    int _work;

    internal MatchAmbiguityGraph(MatchAmbiguityNode expression) => _root = Build(expression);

    internal bool Exhausted { get; private set; }

    static int Add(int left, int right) => Math.Min(2, left + right);

    static Dictionary<int, int> Sum(Dictionary<int, int> left, Dictionary<int, int> right, int scale = 1)
    {
        var result = new Dictionary<int, int>(left);
        if (scale == 0) return result;
        foreach (var (position, count) in right) result[position] = Add(result.GetValueOrDefault(position), count * scale);

        return result;
    }

    static Positions Empty(int nullable = 1) => new(nullable, [], []);

    Positions Build(MatchAmbiguityNode node)
    {
        if (Exhausted) return Empty(0);
        if (++_work > WorkLimit)
        {
            Exhausted = true;
            return Empty(0);
        }
        if (node.Kind == "characters" || node.Kind == "start" || node.Kind == "end")
        {
            if (_positions.Count > PositionLimit)
            {
                Exhausted = true;
                return Empty(0);
            }
            var position = _positions.Count;
            _positions.Add(node);

            return new(0, new() { [position] = 1 }, new() { [position] = 1 });
        }
        if (node.Kind == "group") return Build(node.Children[0]);
        if (node.Kind == "repeat") return Repeat(node);
        var result = Empty(node.Kind == "alternate" ? 0 : 1);
        foreach (var child in node.Children)
        {
            var part = Build(child);
            result = node.Kind == "alternate"
                ? new(Add(result.Nullable, part.Nullable), Sum(result.First, part.First), Sum(result.Last, part.Last))
                : Concat(result, part, node);
        }

        return result;
    }

    Positions Repeat(MatchAmbiguityNode node)
    {
        var result = Empty();
        var copies = node.Maximum ?? Math.Max(1, node.Minimum);
        for (var index = 0; index < copies && !Exhausted; index++)
        {
            var part = Build(node.Children[0]);
            if (node.Maximum is null && index == copies - 1)
            {
                // Saturation at two retains the only distinction EDA needs. Empty body
                // derivations permit an extra empty iteration between consumed positions.
                Connect(part.Last, part.First, node, part.Nullable > 0 ? 2 : 1);
                var scale = part.Nullable > 0 ? 2 : 1;
                part = new(node.Minimum == 0 ? scale : part.Nullable, Sum([], part.First, scale), Sum([], part.Last, scale));
            }
            else if (index >= node.Minimum) { part = part with { Nullable = Add(1, part.Nullable) }; }
            result = Concat(result, part, node);
        }

        return result;
    }

    Positions Concat(Positions left, Positions right, MatchAmbiguityNode origin)
    {
        Connect(left.Last, right.First, origin);

        return new(Math.Min(2, left.Nullable * right.Nullable), Sum(left.First, right.First, left.Nullable), Sum(right.Last, left.Last, right.Nullable));
    }

    void Connect(Dictionary<int, int> left, Dictionary<int, int> right, MatchAmbiguityNode origin, int scale = 1)
    {
        foreach (var (source, firstCount) in left)
        {
            foreach (var (target, lastCount) in right)
            {
                if (++_work > WorkLimit)
                {
                    Exhausted = true;
                    return;
                }
                _edges.Add(new(source, target, Math.Min(2, firstCount * lastCount * scale), origin));
            }
        }
    }

    sealed record Positions(int Nullable, Dictionary<int, int> First, Dictionary<int, int> Last);
    sealed record Edge(int Source, int Target, int Count, MatchAmbiguityNode Origin);
}
