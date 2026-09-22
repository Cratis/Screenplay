// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_opaque_code_might_depend_on_a_renamed_type : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("opaque", "opaque.play", """
            concept ProjectName : String
            module Projects
              feature Registration
                slice StateChange Register
                  command Register
                    handler
                      csharp
                        ```
                        return new ProjectName("name");
                        ```
            """);
        Workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("ProjectName", "ProjectTitle") with { Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments });

    [Fact] void should_refuse_opaque_code_even_with_permission_to_reprint() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_identify_the_opaque_code_occurrence() => _result.Conflicts.Single().Message.Contains("CodeBlockSyntax", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_offer_a_partial_rewrite() => _result.WritePlan.ShouldBeNull();
}
