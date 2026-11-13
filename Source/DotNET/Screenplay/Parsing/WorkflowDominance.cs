// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Checks that a workflow only reads a node's returned values through a path guaranteed to have passed that node.
/// </summary>
internal static class WorkflowDominance
{
    static readonly HashSet<string> _valueless = new(StringComparer.Ordinal) { "failed", "expired" };

    internal static void Validate(WorkflowSyntax workflow, ParserContext context)
    {
        var nodes = workflow.Nodes.GroupBy(node => node.Name, StringComparer.Ordinal).ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
        var producers = nodes.Values
            .Where(node => node.NodeKind == WorkflowNodeKind.Compute || (node.NodeKind == WorkflowNodeKind.Action && node.Command?.Contains('.', StringComparison.Ordinal) == true))
            .Select(node => node.Name)
            .ToHashSet(StringComparer.Ordinal);
        if (producers.Count == 0) return;

        var roots = workflow.Nodes.Where(node => node.NodeKind == WorkflowNodeKind.Trigger).Select(node => node.Name).ToList();
        if (roots.Count == 0 && workflow.Nodes.FirstOrDefault() is { } first) roots.Add(first.Name);

        foreach (var reader in workflow.Nodes)
        {
            foreach (var (path, location) in Reads(reader))
            {
                var root = path.Split('.')[0];
                if (!producers.Contains(root)) continue;
                if (root == reader.Name || ReachableWithoutValues(nodes, roots, root, reader.Name))
                    context.Error(DiagnosticCodes.NonDominatingNodeRead, $"Node '{reader.Name}' reads '{path}', but not every path to it passes '{root}' with a returned value.", location);
            }
        }
    }

    static IEnumerable<(string Path, SourceLocation Location)> Reads(WorkflowNodeSyntax reader)
    {
        foreach (var mapping in reader.Mappings)
        {
            if (mapping.Source is PathExpressionSyntax path) yield return (path.Path, mapping.Location);
        }

        if (reader.Condition is { } condition)
        {
            foreach (var read in ConditionReads(condition)) yield return read;
        }
    }

    static IEnumerable<(string Path, SourceLocation Location)> ConditionReads(ConditionSyntax condition)
    {
        switch (condition)
        {
            case ComparisonConditionSyntax comparison:
                yield return (comparison.Left, comparison.Location);
                if (comparison.Right is PathExpressionSyntax right) yield return (right.Path, comparison.Location);
                break;
            case LogicalConditionSyntax logical:
                foreach (var read in ConditionReads(logical.Left).Concat(ConditionReads(logical.Right))) yield return read;
                break;
        }
    }

    static bool ReachableWithoutValues(Dictionary<string, WorkflowNodeSyntax> nodes, List<string> roots, string producer, string target)
    {
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var stack = new Stack<string>(roots);
        while (stack.Count > 0)
        {
            var name = stack.Pop();
            if (!seen.Add(name)) continue;
            if (name == target) return true;
            if (!nodes.TryGetValue(name, out var node)) continue;
            foreach (var edge in node.Edges)
            {
                if (name == producer && !_valueless.Contains(edge.Outlet)) continue;
                stack.Push(edge.Target);
            }
        }

        return false;
    }
}
