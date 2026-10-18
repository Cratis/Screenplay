// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal sealed partial class MatchAmbiguityGraph
{
    internal bool HasExponentialAmbiguity(MatchAmbiguityNode owner, HashSet<int> live)
    {
        var following = Enumerable.Range(0, _positions.Count).Select(_ => new Dictionary<int, int>()).ToArray();
        foreach (var edge in _edges.Where(edge => live.Contains(edge.Source) && live.Contains(edge.Target) && edge.Origin.Offset >= owner.Offset && edge.Origin.Offset + edge.Origin.Length <= owner.Offset + owner.Length))
        {
            following[edge.Source][edge.Target] = Add(following[edge.Source].GetValueOrDefault(edge.Target), edge.Count);
        }
        var overlaps = new Dictionary<(int, int), bool>();
        foreach (var origin in live)
        {
            var pending = new Queue<(int Left, int Right, bool Different)>();
            var visited = new HashSet<(int, int, bool)> { (origin, origin, false) };
            pending.Enqueue((origin, origin, false));
            while (pending.TryDequeue(out var pair))
            {
                foreach (var left in following[pair.Left])
                {
                    foreach (var right in following[pair.Right])
                    {
                        if (++_work > WorkLimit)
                        {
                            Exhausted = true;
                            return false;
                        }
                        var key = (Math.Min(left.Key, right.Key), Math.Max(left.Key, right.Key));
                        if (!overlaps.TryGetValue(key, out var overlap))
                        {
                            overlap = _positions[left.Key].Characters!.Ranges.Any(first => _positions[right.Key].Characters!.Ranges.Any(second => first.First <= second.Last && second.First <= first.Last));
                            overlaps.Add(key, overlap);
                        }
                        if (!overlap) continue;
                        var different = pair.Different || pair.Left != pair.Right || left.Key != right.Key || left.Value > 1 || right.Value > 1;
                        if (left.Key == origin && right.Key == origin && different) return true;
                        var next = (key.Item1, key.Item2, different);
                        if (visited.Add(next)) pending.Enqueue(next);
                    }
                }
            }
        }

        return false;
    }
}
