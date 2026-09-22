// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring;

public class when_renaming_a_variant_output : given.a_refactoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = Document("model", "model.play", "module App\n  feature F\n    slice StateView S\n      event Created\n      readmodel Open\n        id Uuid\n      projection Lifecycle\n        variant Open\n          enters on Created\n      query Get => Open");
        Workspace = ScreenplayWorkspace.Create("App", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
    }

    void Because() => _result = Workspace.ProposeRename(Rename<ReadModelSyntax>("Open", "Active"));

    [Fact] void should_refuse_an_unproven_variant_output_rename() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_expose_no_disconnected_candidate() => _result.Workspace.ShouldBeNull();
    [Fact] void should_explain_the_variant_output_boundary() => _result.Conflicts.Single().Message.Contains("variant", StringComparison.Ordinal).ShouldBeTrue();
}
