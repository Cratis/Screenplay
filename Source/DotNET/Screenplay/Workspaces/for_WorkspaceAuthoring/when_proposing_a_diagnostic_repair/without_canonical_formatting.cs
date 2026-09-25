// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class without_canonical_formatting : given.a_legacy_validation
{
    WorkspaceAuthoringResult _result = null!;

    void Because() => _result = WorkspaceDiagnosticRepairs.ProposeRepair(Workspace, Repair, Request with { Formatting = WorkspaceAuthoringFormatting.PreserveTrivia });

    [Fact] void should_require_formatting_consent() => _result.Conflicts.Single().Kind.ShouldEqual(WorkspaceConflictKind.FormattingConsentRequired);
    [Fact] void should_not_offer_a_candidate() => _result.Workspace.ShouldBeNull();
}
