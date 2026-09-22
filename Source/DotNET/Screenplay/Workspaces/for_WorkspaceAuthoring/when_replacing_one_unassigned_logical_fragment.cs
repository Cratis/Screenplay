// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_replacing_one_unassigned_logical_fragment : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;
    WorkspaceDocument _first = null!;

    void Establish()
    {
        _first = Document("first", "a.play", "trigger Tick\nmodule App\n  feature One");
        var second = Document("second", "b.play", "module App\n  feature Two");
        Workspace = ScreenplayWorkspace.Create("App", [_first, second], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("App")));
    }

    void Because() => _result = Workspace.ProposeAuthoring(Authoring() with
    {
        Documents = [new ReplaceWorkspaceSyntaxDocument(_first.Id, Syntax("trigger Tick\nmodule Renamed\n  feature One"))]
    });

    [Fact] void should_refuse_the_partial_document_rename() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_expose_no_split_logical_declaration() => _result.Workspace.ShouldBeNull();
    [Fact] void should_explain_the_multiple_source_fragments() => _result.Conflicts.Single().Message.Contains("multiple source fragments", StringComparison.Ordinal).ShouldBeTrue();
}
