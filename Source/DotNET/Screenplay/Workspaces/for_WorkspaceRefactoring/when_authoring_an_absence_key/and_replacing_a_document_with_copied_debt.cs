// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_authoring_an_absence_key;

public class and_replacing_a_document_with_copied_debt : given.a_workspace_with_an_absent_read_model_key
{
    const string Debt = "then no readmodel InvoiceView for {\"id\":\"first\",\"detail\":{\"missing\":\"old\"}}";

    WorkspaceAuthoringResult _safe = null!;
    WorkspaceAuthoringResult _draft = null!;

    void Establish() => CreateWith(Source.Replace(Assertion, Debt, StringComparison.Ordinal));

    void Because()
    {
        var copied = Source.Replace(Assertion, $"{Debt}\n        {Debt}", StringComparison.Ordinal);
        _safe = Workspace.ProposeAuthoring(ReplaceDocument(WorkspaceAuthoringReferencePolicy.Safe, copied));
        _draft = Workspace.ProposeAuthoring(ReplaceDocument(WorkspaceAuthoringReferencePolicy.Draft, copied));
    }

    [Fact] void should_refuse_under_safe() => _safe.Accepted.ShouldBeFalse();
    [Fact] void should_refuse_under_draft() => _draft.Accepted.ShouldBeFalse();
    [Fact] void should_explain_the_ambiguous_correspondence() => _draft.Conflicts.Single().Message.ShouldContain("ambiguous");
    [Fact] void should_not_return_write_plans()
    {
        _safe.WritePlan.ShouldBeNull();
        _draft.WritePlan.ShouldBeNull();
    }
}
