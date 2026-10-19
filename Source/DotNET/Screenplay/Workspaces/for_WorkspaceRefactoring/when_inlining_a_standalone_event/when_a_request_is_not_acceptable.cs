// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRefactoring.when_inlining_a_standalone_event;

public class when_a_request_is_not_acceptable : given.a_standalone_event
{
    WorkspaceAuthoringResult _withoutConsent;
    WorkspaceAuthoringResult _stale;

    void Because()
    {
        _withoutConsent = Inline(_workspace, WorkspaceAuthoringFormatting.PreserveTrivia);
        var subject = WorkspaceSyntaxIndex.Create(_workspace).Entries.First(entry => entry.Node is EventSyntax);
        _stale = WorkspaceEventRefactorings.ProposeInlineStandaloneEvent(_workspace, subject.Handle, new WorkspaceAuthoringRequest
        {
            ExpectedRevision = WorkspaceRevision.Parse("wsrev1:" + new string('0', 64)),
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
    }

    [Fact] void should_require_formatting_consent() => _withoutConsent.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);
    [Fact] void should_reject_stale_revisions() => _stale.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.StaleWorkspaceRevision);
}
