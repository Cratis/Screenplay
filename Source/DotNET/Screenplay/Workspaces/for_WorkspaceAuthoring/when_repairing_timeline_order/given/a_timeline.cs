// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order.given;

public class a_timeline : Specification
{
    internal static ScreenplayWorkspace Create(params (string Path, string Text)[] sources) => ScreenplayWorkspace.Create("Timeline",
        [.. sources.Select(source => WorkspaceDocument.Create(source.Path, PortablePlayPath.Parse(source.Path), Encoding.UTF8.GetBytes(source.Text)))],
        SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Timeline")));

    internal static string Slice(string name, string[] produces, params string[] consumes)
    {
        var lines = new List<string> { $"slice {(produces.Length > 0 ? "StateChange" : "StateView")} {name}" };
        foreach (var produced in produces) lines.Add($"  event {produced}\n    id Uuid");
        if (consumes.Length > 0)
        {
            lines.Add($"  readmodel {name}View\n    id Uuid\n  query Find{name} => {name}View optional\n    by id Uuid\n  projection {name}Projection => {name}View");
            foreach (var consumed in consumes) lines.Add($"    from {consumed} key id");
        }

        return string.Join('\n', lines) + "\n";
    }

    internal static string Indent(string source, int spaces) => string.Join('\n', source.TrimEnd('\n').Split('\n').Select(line => new string(' ', spaces) + line)) + "\n";

    internal static ScreenplayWorkspace Features(params (string Name, string[] Produces, string[] Consumes)[] features) => Create(("root.play",
        "module M\n" + string.Concat(features.Select(feature => $"  feature {feature.Name}\n" + Indent(Slice(feature.Name + "Slice", feature.Produces, feature.Consumes), 4)))));

    internal static Diagnostic Finding(ScreenplayWorkspace workspace, string @event) => workspace.Compilation.Diagnostics.Single(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice && diagnostic.Message.Contains($"event '{@event}'", StringComparison.Ordinal));

    internal static WorkspaceDiagnosticRepair Repair(ScreenplayWorkspace workspace, string @event)
    {
        var diagnostic = Finding(workspace, @event);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var repairs = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic);
        if (repairs.Length != 1)
        {
            var unverified = WorkspaceTimelineRepairs.Find(index, workspace.Revision, diagnostic, false);
            var preview = unverified.Length == 1 ? Propose(workspace, unverified[0]) : null;
            Xunit.Assert.Fail($"Expected one repair for {@event}; unverified={unverified.Length}; conflicts={string.Join(';', preview?.Conflicts.Select(conflict => conflict.Message) ?? [])}; baseline={string.Join(';', workspace.Compilation.Diagnostics.Select(value => value.Code + ':' + value.Message))}");
        }

        return repairs.Single();
    }

    internal static WorkspaceAuthoringResult Propose(ScreenplayWorkspace workspace, WorkspaceDiagnosticRepair repair) => WorkspaceDiagnosticRepairs.ProposeRepair(workspace, repair, new()
    {
        ExpectedRevision = workspace.Revision,
        ExpectedCatalogRevision = workspace.IdentityCatalog.Revision,
        Validation = WorkspaceAuthoringValidation.Authoring,
        Formatting = WorkspaceAuthoringFormatting.CanonicalizeTouchedDocuments
    });

    internal static void Preserves(ScreenplayWorkspace workspace, WorkspaceDiagnosticRepair repair)
    {
        var result = Propose(workspace, repair);
        result.Conflicts.ShouldBeEmpty();
        result.Accepted.ShouldBeTrue();
        if (workspace.Compilation.Value is not null)
        {
            WorkspaceRepairVerification.SameModel(workspace, result.Workspace!).ShouldBeTrue();
        }
        else
        {
            WorkspaceRepairVerification.SameModel(workspace, result.Workspace!).ShouldBeFalse();
            WorkspaceTimelineRepairs.KeepsTimeline(WorkspaceSyntaxIndex.Create(workspace), repair, result).ShouldBeTrue();
        }
        result.Workspace!.IdentityCatalog.Revision.ShouldEqual(workspace.IdentityCatalog.Revision);
        result.ExecutableReady.ShouldEqual(workspace.Compilation.Success);
        result.Workspace.Documents.Select(document => document.Path).ShouldEqual(workspace.Documents.Select(document => document.Path));
        repair.CanFixAll.ShouldBeTrue();
    }
}
