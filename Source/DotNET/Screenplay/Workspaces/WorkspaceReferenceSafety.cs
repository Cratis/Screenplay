// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces;

static class WorkspaceReferenceSafety
{
    internal static void RequireRenameContinuity(WorkspaceReferenceBindings before, WorkspaceReferenceBindings after)
    {
        var remaining = after.Bindings.ToDictionary(binding => binding.Reference.Key, StringComparer.Ordinal);
        foreach (var previous in before.Bindings)
        {
            if (!remaining.Remove(previous.Reference.Key, out var current))
            {
                throw new InvalidWorkspaceAuthoring($"Rename lost reference occurrence '{previous.Reference.Key}'.");
            }

            if (previous.Outcome != current.Outcome || previous.Target?.Key != current.Target?.Key ||
                (previous.Target is null && previous.Reference.Text != current.Reference.Text))
            {
                throw new InvalidWorkspaceAuthoring($"Rename changes binding at '{previous.Reference.Key}' ({previous.Reference.Text}: {previous.Outcome} -> {current.Reference.Text}: {current.Outcome}). Capture, ambiguity, newly resolved debt, and retargeting are not admitted.");
            }
        }

        if (remaining.Count > 0)
        {
            throw new InvalidWorkspaceAuthoring("Rename introduced unexpected reference occurrences.");
        }
    }
}
