// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_rejecting_an_unproven_identifier_span : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceRenameRequest _request = null!;

    void Establish()
    {
        var document = Document("same", "same.play", "concept name : String\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        name name");
        Workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
        _request = Rename<ConceptSyntax>("name", "Title");
    }

    void Because() => _result = Workspace.ProposeRename(_request);

    [Fact] void should_reject_nonunique_line_tokens() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_silently_canonicalize() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_allow_explicit_canonicalization() => Workspace.ProposeRename(_request with { Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments }).Accepted.ShouldBeTrue();
}
