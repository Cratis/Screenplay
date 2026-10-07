// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Dependencies;

/// <summary>
/// Deterministic Tarjan decomposition shared by dependency queries and timeline diagnostics.
/// </summary>
internal static class StronglyConnectedGroups
{
    internal static IReadOnlyList<IReadOnlyList<string>> In(IReadOnlyDictionary<string, HashSet<string>> graph)
    {
        var indices = new Dictionary<string, int>(StringComparer.Ordinal);
        var low = new Dictionary<string, int>(StringComparer.Ordinal);
        var stack = new Stack<string>();
        var active = new HashSet<string>(StringComparer.Ordinal);
        var groups = new List<IReadOnlyList<string>>();
        void Visit(string node)
        {
            indices[node] = indices.Count;
            low[node] = indices[node];
            stack.Push(node);
            active.Add(node);
            foreach (var next in graph[node].Order(StringComparer.Ordinal))
            {
                if (!indices.TryGetValue(next, out var nextIndex))
                {
                    Visit(next);
                    low[node] = Math.Min(low[node], low[next]);
                }
                else if (active.Contains(next))
                {
                    low[node] = Math.Min(low[node], nextIndex);
                }
            }

            if (low[node] != indices[node]) return;
            var group = new List<string>();
            string member;
            do
            {
                member = stack.Pop();
                active.Remove(member);
                group.Add(member);
            }
            while (member != node);
            groups.Add([.. group.Order(StringComparer.Ordinal)]);
        }

        foreach (var node in graph.Keys.Order(StringComparer.Ordinal))
        {
            if (!indices.ContainsKey(node)) Visit(node);
        }

        return groups;
    }
}
