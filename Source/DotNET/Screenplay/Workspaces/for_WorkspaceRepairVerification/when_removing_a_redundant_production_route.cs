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

    [Fact]
    void should_preserve_a_keyed_command_route_when_removing_its_redundant_override()
    {
        const string source = "eventsource Account\n  identifier String\n  stream Notes\n    streamId String\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id String identifier\n        period String\n        stream Account.Notes\n          streamId = period\n        produces event E\n          stream Account.Notes\n            streamId = period\n";
        var document = WorkspaceDocument.Create("keyed", PortablePlayPath.Parse("keyed.play"), Encoding.UTF8.GetBytes(source));
        var workspace = ScreenplayWorkspace.Create("KeyedRoutes", [document], SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("KeyedRoutes")));
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var diagnostic = index.RepairableDiagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.RedundantProductionRoute);
        var repair = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic).Single();
        var result = WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, new()
        {
            ExpectedRevision = workspace.Revision,
            ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
            Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments,
            Validation = WorkspaceAuthoringValidation.Authoring
        });
        Assert.True(result.Accepted, string.Join(';', result.Conflicts.Select(conflict => conflict.Message)));
        var command = WorkspaceSyntaxIndex.Create(result.Workspace!).Entries.Select(entry => entry.Node).OfType<CommandSyntax>().Single();
        command.Produces.Single().Stream.ShouldBeNull();
        command.Stream!.Stream.ShouldEqual("Notes");
        ((PathExpressionSyntax)command.Stream.StreamId!.Source).Path.ShouldEqual("period");
    }

    [Fact] void should_accept_the_repair() => Assert.True(_result.Accepted, string.Join(';', _result.Conflicts.Select(conflict => conflict.Message)));
    [Fact] void should_remove_only_the_override() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<ProducesSyntax>().Single().Stream.ShouldBeNull();
    [Fact] void should_preserve_the_command_route() => WorkspaceSyntaxIndex.Create(_result.Workspace!).Entries.Select(entry => entry.Node).OfType<CommandSyntax>().Single().Stream.ShouldNotBeNull();
}
