// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_rejecting_printer_omissions : given.an_authoring_workspace
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        var concept = Index.Entries.Single(entry => entry.Node is ConceptSyntax concept && concept.Name == "ProjectName");
        var replacement = (ConceptSyntax)concept.Node with
        {
            Validations = [new DeclarativeValidateSyntax(
                [new ValidationRuleSyntax(
                    ValidationRuleSyntax.ConceptValue,
                    ValidationRuleKind.NotEmpty,
                    null,
                    null,
                    SourceLocation.Start,
                    File: new FileReferenceSyntax("Rules/Name.cs", SourceLocation.Start))],
                SourceLocation.Start)]
        };
        _result = Workspace.ProposeAuthoring(Authoring(new ReplaceWorkspaceNode(concept.Handle, concept.Node, replacement)));
    }

    [Fact] void should_reject_a_field_the_printer_only_mentions_in_a_comment() => _result.Accepted.ShouldBeFalse();
    [Fact] void should_report_failed_structural_fidelity() => _result.Conflicts.Single().Message.Contains("intended typed AST", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_not_publish_lossy_source() => _result.WritePlan.ShouldBeNull();
}
