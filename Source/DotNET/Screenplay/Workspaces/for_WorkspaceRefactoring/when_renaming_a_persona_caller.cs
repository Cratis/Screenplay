// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_persona_caller : given.a_refactoring_workspace
{
    const string Source = """
        policy Member
          require role "A"
        persona Person
          policy Member
        module M
          feature F
            slice StateChange S
              command C
                authorize Member
                produces E
              event E
              specification X
                given caller as Person
                when C
                then E
        """;
    WorkspaceAuthoringResult _result = null!;

    void Establish() => Workspace = ScreenplayWorkspace.Create("Records", [Document("records", "application.play", Source)], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Records")));

    void Because() => _result = Workspace.ProposeRename(Rename<PersonaSyntax>("Person", "Clerk"));

    [Fact] void should_accept() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration_and_caller_reference() => _result.Workspace.Documents.Single().Text.ShouldEqual(Source.Replace("persona Person", "persona Clerk", StringComparison.Ordinal).Replace("given caller as Person", "given caller as Clerk", StringComparison.Ordinal));
    [Fact] void should_keep_the_persona_reference_resolved() => new WorkspaceReferenceBindings(WorkspaceSyntaxIndex.Create(_result.Workspace)).Bindings.Single(binding => binding.Reference.Domain == WorkspaceReferenceDomain.Persona).Target.ShouldNotBeNull();
}
