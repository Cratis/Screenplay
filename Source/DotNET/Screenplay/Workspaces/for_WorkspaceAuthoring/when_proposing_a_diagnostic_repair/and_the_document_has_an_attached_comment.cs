// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class and_the_document_has_an_attached_comment : Specification
{
    ScreenplayWorkspace _workspace = null!;
    WorkspaceDiagnosticRepair _repair = null!;
    WorkspaceAuthoringRequest _request = null!;
    WorkspaceAuthoringResult _result = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("billing", PortablePlayPath.Parse("billing.play"), Encoding.UTF8.GetBytes(given.a_legacy_validation.Source.Replace("validate csharp", "validate csharp // detached", StringComparison.Ordinal)));
        _workspace = ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
        var warning = WorkspaceSyntaxIndex.Create(_workspace).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence);
        _repair = WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, warning).Single();
        _request = new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = _repair.RequiredFormatting
        };
    }

    void Because() => _result = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repair, _request);

    [Fact] void should_preserve_the_comment() => _result.Workspace!.Documents[0].Text.ShouldContain("// detached");
    [Fact] void should_accept_the_repair() => _result.Accepted.ShouldBeTrue();
}
