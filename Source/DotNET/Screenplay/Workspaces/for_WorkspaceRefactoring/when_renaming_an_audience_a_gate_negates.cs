// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_audience_a_gate_negates : given.a_refactoring_workspace
{
    const string Source = """
        audience Staff
        audience Incident
        module M
          available for Staff and not Incident
          feature F
            slice StateChange S
              command C
                produces E
              event E
        """;
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<AudienceSyntax>("Incident", "Outage"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration_and_the_negated_reference() => _result.Workspace.Documents.Single().Text.ShouldEqual(Source
        .Replace("audience Incident", "audience Outage", StringComparison.Ordinal)
        .Replace("not Incident", "not Outage", StringComparison.Ordinal));
}
