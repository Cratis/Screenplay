// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_rejecting_newly_captured_debt : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("capture", "capture.play", "concept Existing : String\nmodule Projects\n  feature Registration\n    slice StateChange Register\n      command Register\n        name Missing");
        Workspace = ScreenplayWorkspace.Create("Projects", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Projects")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<ConceptSyntax>("Existing", "Missing"));

    [Fact] void should_reject_an_unresolved_reference_becoming_resolved() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_not_expose_partial_writes() => _result.WritePlan.ShouldBeNull();
    [Fact] void should_explain_the_binding_change() => _result.Conflicts.Single().Message.Contains("binding", StringComparison.Ordinal).ShouldBeTrue();
}
