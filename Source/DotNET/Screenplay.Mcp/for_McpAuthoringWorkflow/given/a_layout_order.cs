// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpAuthoringWorkflow.given;

static class a_layout_order
{
    internal static string[] Sequences(ScreenplayWorkspace workspace)
    {
        var timeline = WorkspaceTimelineRepairs.Timeline(workspace);
        var sequences = new List<string>();
        void Add(string kind, string[] scope, IEnumerable<string> names)
        {
            var children = names.Distinct(StringComparer.Ordinal).Select(name => AuthoredOrder.Key(scope.Append(name)))
                .OrderBy(key => timeline.Ranks.GetValueOrDefault(key, int.MaxValue));
            sequences.Add($"{kind}:{AuthoredOrder.Key(scope)}:{string.Join('|', children)}");
        }

        void Features(IEnumerable<FeatureSyntax> features, string[] scope)
        {
            var items = features.ToArray();
            Add("features", scope, items.Select(feature => feature.Name));
            foreach (var group in items.GroupBy(feature => feature.Name, StringComparer.Ordinal))
            {
                string[] child = [.. scope, group.Key];
                Add("slices", child, group.SelectMany(feature => feature.Slices).Select(slice => slice.Name));
                Features(group.SelectMany(feature => feature.Features), child);
            }
        }

        var modules = timeline.Application!.Modules.ToArray();
        Add("modules", [], modules.Select(module => module.Name));
        foreach (var group in modules.GroupBy(module => module.Name, StringComparer.Ordinal)) Features(group.SelectMany(module => module.Features), [group.Key]);

        return [.. sequences.Order(StringComparer.Ordinal)];
    }
}
