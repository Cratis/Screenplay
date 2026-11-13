// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_an_audience : given.a_refactoring_workspace
{
    const string Source = """
        audience Staff
        availability StaffOnly
          for Staff
        persona Clerk
          audience Staff
        module M
          available for Staff or Customers
          feature F
            available with StaffOnly
            slice StateChange S
              command C
                produces E
              event E
        audience Customers
        """;
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<AudienceSyntax>("Staff", "Employees"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration_and_every_reference() => _result.Workspace.Documents.Single().Text.ShouldEqual(Source
        .Replace("audience Staff", "audience Employees", StringComparison.Ordinal)
        .Replace("for Staff", "for Employees", StringComparison.Ordinal));
}
