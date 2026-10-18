// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

internal sealed record MatchAmbiguity(int Offset, int Length, string Subexpression, string? Suggestion)
{
    internal string Description => $"Match subexpression '{Subexpression}' at offset {Offset} is exponentially ambiguous and can cause excessive browser backtracking.{(Suggestion is { } suggestion ? $" Use '{suggestion}' instead." : string.Empty)}";

    // Call only after admission. Budget exhaustion means no finding, not a dialect refusal.
    internal static MatchAmbiguity? Find(string pattern)
    {
        var expression = new MatchAmbiguityParser(pattern).Parse();
        var graph = new MatchAmbiguityGraph(expression);
        if (graph.Exhausted) return null;
        var live = graph.Trim();
        foreach (var candidate in Descendants(expression).Where(node => node.Kind == "repeat" && node.Maximum is null).OrderBy(node => node.Length).ThenBy(node => node.Offset))
        {
            if (graph.HasExponentialAmbiguity(candidate, live)) return new(candidate.Offset, candidate.Length, pattern.Substring(candidate.Offset, candidate.Length), Rewrite(candidate, pattern));
            if (graph.Exhausted) return null;
        }

        return null;
    }

    static IEnumerable<MatchAmbiguityNode> Descendants(MatchAmbiguityNode node) => new[] { node }.Concat(node.Children.SelectMany(Descendants));

    static MatchAmbiguityNode Unwrap(MatchAmbiguityNode node)
    {
        while ((node.Kind == "group" || node.Kind == "concat" || node.Kind == "alternate") && node.Children.Length == 1) node = node.Children[0];

        return node;
    }

    static string? Rewrite(MatchAmbiguityNode node, string pattern)
    {
        if (node.Minimum > 1) return null;
        var body = Unwrap(node.Children[0]);
        var minimum = node.Minimum;
        if (body.Kind == "repeat" && body.Maximum is null && body.Minimum <= 1)
        {
            minimum *= body.Minimum;
            body = Unwrap(body.Children[0]);
        }
        else if (body.Kind == "alternate" && body.Children.Length == 2)
        {
            var left = Unwrap(body.Children[0]);
            var right = Unwrap(body.Children[1]);
            if (pattern.Substring(left.Offset, left.Length) != pattern.Substring(right.Offset, right.Length)) return null;
            body = left;
        }
        else { return null; }
        var text = pattern.Substring(body.Offset, body.Length);

        return (body.Kind == "characters" ? text : $"(?:{text})") + (minimum == 0 ? "*" : "+");
    }
}
