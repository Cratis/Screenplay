// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class and_the_diagnostic_is_a_standalone_language_line : Specification
{
    ImmutableArray<WorkspaceDiagnosticRepair> _repairs;
    ScreenplayWorkspace _workspace = null!;
    Diagnostic _warning = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("billing", PortablePlayPath.Parse("billing.play"), Encoding.UTF8.GetBytes("module Billing\n  feature Invoices\n    slice StateChange Register\n      command Register\n        handler\n          csharp\n            ```\n            return [];\n            ```"));
        _workspace = ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
        _warning = WorkspaceSyntaxIndex.Create(_workspace).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence);
    }

    void Because() => _repairs = WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, _warning);

    [Fact] void should_not_offer_a_repair() => _repairs.ShouldBeEmpty();
}
