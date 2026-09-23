// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_beside_unrelated_opaque_code : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("opaque", "opaque.play", """
            import External.Customer
            concept ProjectName : String
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    handler
                      csharp
                        ```
                        return new ProjectNameSuffix("ProjectNames");
                        ```
            """);
        Workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("ProjectName", "ProjectTitle"));

    [Fact] void should_accept_the_rename() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_rename_the_declaration() => _result.Workspace!.Documents.Single().Text.Contains("concept ProjectTitle : String", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_leave_the_opaque_code_untouched() => _result.Workspace!.Documents.Single().Text.Contains("return new ProjectNameSuffix(\"ProjectNames\");", StringComparison.Ordinal).ShouldBeTrue();
}
