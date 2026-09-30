// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_a_rename_candidate_breaks_absence_continuity : given.a_workspace_with_an_absent_read_model_key
{
    Exception _error = null!;

    void Because()
    {
        var before = WorkspaceSyntaxIndex.Create(Workspace);
        var part = before.Entries.Single(entry => entry.Node is PropertySyntax { Name: "part" });

        // The rewritten member binds a pre-existing 'segment' declaration rather than the renamed 'part'.
        CreateWith(Source
            .Replace("  part String", "  part String\n  segment String", StringComparison.Ordinal)
            .Replace("\"part\":\"old\"", "\"segment\":\"old\"", StringComparison.Ordinal));
        var after = WorkspaceSyntaxIndex.Create(Workspace);
        _error = Catch.Exception(() => WorkspaceRefactoring.RequireAbsenceContinuity(
            new(before, new(before)),
            new(after, new(after)),
            [],
            part.Address!,
            "segment"));
    }

    [Fact] void should_refuse_the_candidate() => _error.ShouldBeOfExactType<InvalidWorkspaceAuthoring>();
    [Fact] void should_name_the_captured_member() => _error.Message.ShouldContain("Rename changes absence key binding");
    [Fact] void should_name_the_original_member_text() => _error.Message.ShouldContain("(part)");
}
