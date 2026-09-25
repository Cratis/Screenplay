// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_proposing_a_diagnostic_repair : Specification
{
    const string Source = """
        module Billing
          feature Invoices
            slice StateChange Register
              command Register
                validate csharp
                  ```
                  yield return "invalid";
                  ```
        """;

    ScreenplayWorkspace _workspace = null!;
    WorkspaceDiagnosticRepair _repair = null!;
    WorkspaceAuthoringResult _preview = null!;
    WorkspaceAuthoringResult _stale = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("billing", PortablePlayPath.Parse("billing.play"), Encoding.UTF8.GetBytes(Source));
        _workspace = ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
        var warning = WorkspaceSyntaxIndex.Create(_workspace).Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence);
        _repair = WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, warning).Single();
    }

    void Because()
    {
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Operations = _repair.Operations
        };
        _preview = _workspace.ProposeAuthoring(request);
        _stale = _workspace.ProposeAuthoring(request with { ExpectedRevision = default });
    }

    [Fact] void should_key_a_typed_repair_by_code_and_subject() =>
        (_repair.DiagnosticCode == DiagnosticCodes.LegacyInlineCodeFence &&
         _repair.Subject.Revision == _workspace.Revision &&
         _repair.Operations.Single() is ReplaceWorkspaceNode replace && replace.Target == _repair.Subject).ShouldBeTrue();
    [Fact] void should_preview_without_mutating_original_bytes() =>
        Encoding.UTF8.GetString(_workspace.Documents[0].Bytes.AsSpan()).ShouldEqual(Source);
    [Fact] void should_print_the_canonical_fence() => _preview.Workspace!.Documents[0].Text.ShouldContain("validate\n          ```csharp");
    [Fact] void should_compile_without_the_warning() =>
        WorkspaceSyntaxIndex.Create(_preview.Workspace!).Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.LegacyInlineCodeFence).ShouldBeFalse();
    [Fact] void should_offer_a_write_plan_only_for_an_accepted_preview() =>
        (_preview.Accepted && _preview.WritePlan is not null).ShouldBeTrue();
    [Fact] void should_refuse_stale_revision_without_partial_candidate() =>
        (_stale.Conflicts.Single().Kind == WorkspaceConflictKind.StaleWorkspaceRevision && _stale.Workspace is null && _stale.WritePlan is null).ShouldBeTrue();

    [Fact] void should_not_offer_a_repair_that_requires_a_choice()
    {
        var diagnostic = Diagnostic.Warning(DiagnosticCodes.LegacyInlineCodeFence, "changed message", new SourceLocation(1, 1, "billing.play"));
        WorkspaceDiagnosticRepairs.Find(_workspace, _workspace.Revision, diagnostic).ShouldBeEmpty();
    }

    [Fact] void should_refuse_an_added_node_with_an_unresolved_reference_under_safe_policy()
    {
        var index = WorkspaceSyntaxIndex.Create(_workspace);
        var root = index.Entries.Single(entry => entry.Parent is null);
        var type = new ScreenplayCompiler().Parse("type DraftType\n  value MissingType").Value!.Types!.Single();
        var request = new WorkspaceAuthoringRequest
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Safe,
            Operations = [new AddWorkspaceNode(root.Handle, root.Node, "types", type)]
        };
        var result = _workspace.ProposeAuthoring(request);
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
        _workspace.ProposeAuthoring(request with { ReferencePolicy = WorkspaceAuthoringReferencePolicy.Draft }).Accepted.ShouldBeTrue();
    }
}
