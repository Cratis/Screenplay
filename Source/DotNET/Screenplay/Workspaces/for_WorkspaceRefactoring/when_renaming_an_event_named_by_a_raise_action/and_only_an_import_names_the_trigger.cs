// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_renaming_an_event_named_by_a_raise_action;

public class and_only_an_import_names_the_trigger : given.a_refactoring_workspace
{
    WorkspaceReferenceBinding _binding = null!;

    void Establish()
    {
        const string Source = """
            import External.Created
            module App
              feature Items
                slice StateView Details
                  screen Details
                    on click
                      raise Created
            """;
        Workspace = ScreenplayWorkspace.Create("App", [Document("model", "model.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
    }

    void Because() => _binding = new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(Workspace)).Bindings.Single(binding => binding.Reference.Entry.Node is RaiseTriggerActionSyntax);

    [Fact] void should_not_bind_to_an_import() => _binding.Outcome.ShouldEqual("unresolved");
    [Fact] void should_have_no_target() => _binding.Target.ShouldBeNull();
}
