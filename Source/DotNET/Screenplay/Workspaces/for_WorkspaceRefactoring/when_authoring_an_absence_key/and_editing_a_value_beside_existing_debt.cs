// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_editing_a_value_beside_existing_debt : given.a_workspace_with_an_absent_read_model_key
{
    WorkspaceAuthoringResult _result = null!;

    void Establish() => CreateWith(Source.Replace("\"part\":\"old\"", "\"missing\":\"old\"", StringComparison.Ordinal));

    void Because()
    {
        var value = WorkspaceSyntaxIndex.Create(Workspace).Find(Member("id").Handle with { Path = $"{Member("id").Handle.Path}/value" })!;
        _result = Workspace.ProposeAuthoring(Authoring(
            WorkspaceAuthoringReferencePolicy.Safe,
            new ReplaceWorkspaceNode(value.Handle, value.Node, ((LiteralExpressionSyntax)value.Node) with { Value = "second" })));
    }

    [Fact] void should_retain_the_proven_unchanged_debt_under_safe() => _result.Accepted.ShouldBeTrue();
    [Fact] void should_write_the_edited_value() => Text(_result).ShouldContain("{\"id\":\"second\",\"detail\":{\"missing\":\"old\"}}");
    [Fact] void should_report_the_retained_debt() => AbsenceDebt(_result).Single().ShouldContain("'missing'");
}
