// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal sealed partial class MatchAmbiguityGraph
{
    // Assertions are zero-width edges guarded by two monotone flags: some input has
    // been consumed, and end-of-input has been asserted. Search can start anywhere.
    internal HashSet<int> Trim()
    {
        var following = Enumerable.Range(0, _positions.Count).Select(_ => new HashSet<int>()).ToArray();
        foreach (var edge in _edges) following[edge.Source].Add(edge.Target);
        following[0].UnionWith(_root.First.Keys);
        var reverse = Enumerable.Range(0, _positions.Count * 4).Select(_ => new HashSet<int>()).ToArray();
        var reached = new HashSet<int> { 0, 1 };
        var pending = new Queue<int>(reached);
        var finals = new HashSet<int>();
        while (pending.TryDequeue(out var state))
        {
            var position = state / 4;
            var phase = state % 4;
            if (_root.Last.ContainsKey(position) || (position == 0 && _root.Nullable > 0)) finals.Add(state);
            foreach (var target in following[position])
            {
                var node = _positions[target];
                if (node.Kind == "start" && (phase & 1) != 0) continue;
                if (node.Kind == "characters" && ((phase & 2) != 0 || !node.Characters!.Ranges.Any())) continue;
                var nextPhase = node.Kind switch { "end" => phase | 2, "characters" => phase | 1, _ => phase };
                var next = (target * 4) + nextPhase;
                reverse[next].Add(state);
                if (reached.Add(next)) pending.Enqueue(next);
            }
        }
        var live = new HashSet<int>(finals);
        pending = new(finals);
        while (pending.TryDequeue(out var state))
        {
            foreach (var previous in reverse[state].Where(live.Add)) pending.Enqueue(previous);
        }

        return [.. Enumerable.Range(1, _positions.Count - 1).Where(position => _positions[position].Kind == "characters" && reached.Contains((position * 4) + 1) && live.Contains((position * 4) + 1))];
    }
}
