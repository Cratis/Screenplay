// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceRepairVerification;

public class when_removing_a_redundant_production_route : Specification
{
    WorkspaceAuthoringResult _result = null!;

    void Because()
    {
        const string source = "eventsource Account\n  identifier String\n  stream All\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        stream Account.All\n        produces event E\n          stream Account.All\n";
        var document = WorkspaceDocument.Create("source", PortablePlayPath.Parse("source.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("Routes", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Routes")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantProductionRoute);
        var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        _result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
    }

    [Fact] void should_accept_the_repair() => Assert.True(_result.Accepted, string.Join(';', _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_remove_only_the_override() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<ProducesSyntax>().Single().Stream.ShouldBeNull();
    [Fact] void should_preserve_the_command_route() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<CommandSyntax>().Single().Stream.ShouldNotBeNull();
}
