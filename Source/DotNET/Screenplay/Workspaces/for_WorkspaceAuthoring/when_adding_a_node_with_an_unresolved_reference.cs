// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring;

public class when_adding_a_node_with_an_unresolved_reference : Specification
{
    ScreenplayWorkspace _workspace = null!;
    WorkspaceAuthoringRequest _request = null!;
    WorkspaceAuthoringResult _safe = null!;

    void Establish()
    {
        var document = WorkspaceDocument.Create("billing", PortablePlayPath.Parse("billing.play"), Encoding.UTF8.GetBytes("module Billing\n  feature Invoices\n    slice StateChange Register\n      command Register"));
        _workspace = ScreenplayWorkspace.Create("Billing", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Billing")));
        var root = WorkspaceSyntaxIndex.Create(_workspace).Entries.Single(entry => entry.Parent is null);
        var type = new ScreenplayCompiler().Parse("type DraftType\n  value MissingType").Value!.Types!.Single();
        _request = new()
        {
            ExpectedRevision = _workspace.Revision,
            ExpectedCatalogRevision = _workspace.IdentityCatalog.Revision,
            Validation = WorkspaceAuthoringValidation.Authoring,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            ReferencePolicy = WorkspaceAuthoringReferencePolicy.Safe,
            Operations = [new AddWorkspaceNode(root.Handle, root.Node, "types", type)]
        };
    }

    void Because() => _safe = _workspace.ProposeAuthoring(_request);

    [Fact] void should_refuse_the_unresolved_reference() => _safe.Conflicts.Any(conflict => conflict.Message.Contains("reference", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_not_provide_a_write_plan() => _safe.WritePlan.ShouldBeNull();
}
