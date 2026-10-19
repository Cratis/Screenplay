// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

static class WorkspacePropertyReports
{
    internal static WorkspacePropertyReport Readiness(ScreenplayWorkspace before, ScreenplayWorkspace candidate, WorkspacePropertyReport report)
    {
        var known = candidate.Compilation.Success;
        var previous = before.Compilation.Value?.Model.SemanticVersion.ToString();
        var current = candidate.Compilation.Value?.Model.SemanticVersion.ToString();
        return report with
        {
            EvolutionDraft = report.Generations.Any(generation => generation.Mode == "newGeneration"),
            DeployableWithoutTransformation = !WorkspaceSyntaxIndex.Create(candidate).Entries.Any(entry => entry.Node is EventSyntax @event && @event.Generation > 1),
            EsmVersionKnown = known,
            EsmVersionChange = known && previous is { } from && current is { } to && from != to ? new(from, to) : null
        };
    }
}
