// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_translation_direction;

public class and_a_translation_uses_public_events : Specification
{
    const string Inbound = "import Outside.Arrived from \"other\"\nmodule M\n  feature F\n    slice Translate S\n      event Local\n      reaction R\n        when Arrived\n          produces Local\n";
    const string Outbound = "module M\n  feature F\n    slice Translate S\n      event Local\n      public event Published\n      reaction R\n        when Local\n          produces Published\n";

    static ScreenplayWorkspace Create(string source) => ScreenplayWorkspace.Create(
        "Repair",
        [WorkspaceDocument.Create("repair", PortablePlayPath.Parse("repair.play"), Encoding.UTF8.GetBytes(source))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Repair")));

    static WorkspaceAuthoringRequest Request(ScreenplayWorkspace workspace) => new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    };

    [Theory]
    [InlineData(Inbound, TranslationDirection.Inbound)]
    [InlineData(Outbound, TranslationDirection.Outbound)]
    void should_offer_only_the_direction_the_slice_supports_and_apply_it(string source, TranslationDirection expected)
    {
        var workspace = Create(source);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.RepairableDiagnostics.Single(value => value.Code == DiagnosticCodes.PublicTranslationRequiresDirection);
        var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        repair.CanFixAll.ShouldBeFalse();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, diagnostic.Code, repair.Subject, Request(workspace));
        result.Accepted.ShouldBeTrue();
        WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<SliceSyntax>().Single().Direction.ShouldEqual(expected);
        result.Workspace!.Compilation.Diagnostics.Any(value => value.Code == DiagnosticCodes.PublicTranslationRequiresDirection).ShouldBeFalse();
    }
}
