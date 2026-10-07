// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_a_module_named_by_depends_on;

public class and_declarations_repeat_with_comments : Specification
{
    WorkspaceAuthoringResult _result;

    void Because() => _result = with_proven_targets.Rename("module Consumer\n  depends on A // first\n  // repeated intent\n  depends on A // second\nmodule A\n", "A", "B");

    [Fact] void should_accept_the_default_trivia_preserving_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_repair_every_occurrence_and_preserve_comments() => _result.Workspace!.Documents.Single().Text.ShouldEqual("module Consumer\n  depends on B // first\n  // repeated intent\n  depends on B // second\nmodule B\n");
    [Fact] void should_keep_one_effective_declaration() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Count(entry => entry.Node is DependsOnSyntax).ShouldEqual(1);
    [Fact] void should_preserve_the_repeat_warning() => _result.Workspace!.Compilation.Diagnostics.Count(diagnostic => diagnostic.Code == "PLAY0555").ShouldEqual(1);
}
