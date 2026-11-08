// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_sink : given.a_refactoring_workspace
{
    const string Source = """
        sink Reporting for readmodels
        sink People for graphs
        concept PersonId : Uuid
        module M
          feature F
            slice StateView S
              readmodel Row
                sink Reporting
                id String
              readmodel Other
                id String
                sink Reporting
              graph Organization
                sink People
                node Person
                  personId PersonId key
        """;

    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<SinkSyntax>("Reporting", "Analytics"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration_and_every_directive_that_uses_it() => _result.Workspace.Documents.Single().Text.ShouldEqual(Source.Replace("Reporting", "Analytics", StringComparison.Ordinal));
    [Fact] void should_keep_every_sink_reference_resolved() => new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(_result.Workspace)).Bindings.Where(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Sink).All(binding => binding.Target is not null).ShouldBeTrue();
    [Fact]
    void should_refuse_a_name_that_differs_only_in_case_from_another_sink()
    {
        var refused = Workspace.ProposeRename(Rename<SinkSyntax>("Reporting", "people"));
        refused.Accepted.ShouldBeFalse();
        refused.Conflicts.Single().Message.ShouldContain("already declares sink 'people'");
    }
}
