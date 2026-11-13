// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_lifecycles.given;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_proposing_a_diagnostic_repair;

public class and_the_diagnostic_is_a_deprecated_lifecycle_verb : Specification
{
    ScreenplayWorkspace _workspace = null!;
    ImmutableArray<Diagnostic> _warnings;
    ImmutableArray<WorkspaceDiagnosticRepair> _repairs;
    WorkspaceAuthoringResult _preview = null!;

    void Establish()
    {
        var source = a_lifecycle_source.Source
            .Replace("entering invokes ChargeFee", "entering send ChargeFee", StringComparison.Ordinal)
            .Replace("action TagLine invokes", "action TagLine sends", StringComparison.Ordinal);
        var document = WorkspaceDocument.Create("billing", PortablePlayPath.Parse("billing.play"), Encoding.UTF8.GetBytes(source));
        _workspace = ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
        _warnings = [.. WorkspaceSyntaxIndex.Create(_workspace).Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence)];
    }

    void Because()
    {
        _repairs = WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, _warnings[0]);
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = _repairs.Single().RequiredFormatting
        };
        _preview = WorkspaceDiagnosticRepairs.ProposeRepair(_workspace, _repairs.Single(), request);
    }

    [Fact] void should_warn_for_each_deprecated_verb() => _warnings.Length.ShouldEqual(2);
    [Fact] void should_offer_one_typed_repair() => _repairs.Single().Operations.Single().ShouldBeOfExactType<ReplaceWorkspaceNode>();
    [Fact] void should_accept_the_preview() => _preview.Accepted.ShouldBeTrue();
    [Fact] void should_print_the_new_verbs() => _preview.Workspace!.Documents[0].Text.ShouldContain("entering invokes ChargeFee");
    [Fact] void should_resolve_every_warning() => WorkspaceSyntaxIndex.Create(_preview.Workspace!).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence).ShouldBeFalse();
}
