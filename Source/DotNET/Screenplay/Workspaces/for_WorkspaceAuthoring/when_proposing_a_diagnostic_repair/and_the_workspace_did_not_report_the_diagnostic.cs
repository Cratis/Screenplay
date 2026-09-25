// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class and_the_workspace_did_not_report_the_diagnostic : given.a_legacy_validation
{
    ImmutableArray<WorkspaceDiagnosticRepair> _repairs;

    void Because() => _repairs = WorkspaceDiagnosticRepairs.Find(Workspace, Workspace.Revision, Diagnostic.Warning(DiagnosticCodes.LegacyInlineCodeFence, "changed message", new SourceLocation(1, 1, "billing.play")));

    [Fact] void should_not_offer_a_repair_for_a_diagnostic_the_workspace_did_not_report() => _repairs.ShouldBeEmpty();
}
