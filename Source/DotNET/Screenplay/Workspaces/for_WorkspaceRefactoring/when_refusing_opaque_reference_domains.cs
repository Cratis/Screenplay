// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_refusing_opaque_reference_domains : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        Registration = Document(Registration.StableKey, Registration.Path.Value, $"import External.ProjectName\n{RegistrationSource}");
        Workspace = ScreenplayWorkspace.Create("Projects", [Concepts, Registration], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("ProjectName", "ProjectTitle") with { Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments });

    [Fact] void should_refuse_even_with_canonicalization_permission() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_identify_the_opaque_occurrence() => _result.Conflicts.Single().Message.Contains("ImportSyntax", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_never_offer_partial_writes() => _result.WritePlan.ShouldBeNull();
}
