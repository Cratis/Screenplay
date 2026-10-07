// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.for_Samples;

public class when_repairing_the_sample_timelines(Xunit.Abstractions.ITestOutputHelper output) : given.the_samples
{
    [Theory]
    [InlineData("Library", 2, 2)]
    [InlineData("Invoicing", 4, 2)]
    [InlineData("Commerce", 7, 4)]
    [InlineData("TimeTracking", 9, 7)]
    void should_repair_only_the_findings_in_the_plan_table(string name, int findings, int repairable)
    {
        var folder = _samples.Single(sample => Path.GetFileName(sample) == name);
        var documents = Directory.GetFiles(folder, "*.play", SearchOption.AllDirectories).Select(path =>
            WorkspaceDocument.Create(Path.GetRelativePath(folder, path).Replace('/', '_'), PortablePlayPath.Parse(Path.GetRelativePath(folder, path)), File.ReadAllBytes(path))).ToArray();

        // Source-only workspaces still have established document identities, independent of ESM admission.
        var catalog = SemanticIdentityCatalog.Create(
            ApplicationIdentity.Create(name),
            [.. documents.Select(document => new DocumentIdentityAssignment(document.StableKey, document.Id, SemanticIdentityOrigin.Persisted))],
            [],
            []);
        var workspace = ScreenplayWorkspace.Create(name, [.. documents], catalog);
        var diagnostics = workspace.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice || diagnostic.Code == DiagnosticCodes.TimelineCycleGroup).ToArray();
        diagnostics.Length.ShouldEqual(findings);
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var offered = 0;
        foreach (var diagnostic in diagnostics)
        {
            var repairs = WorkspaceDiagnosticRepairs.Find(index, workspace.Revision, diagnostic);
            var expected = Expected(name, diagnostic);
            if (expected is null)
            {
                repairs.ShouldBeEmpty();
                continue;
            }

            offered++;
            if (repairs.Length != 1)
            {
                var unverified = WorkspaceTimelineRepairs.Find(index, workspace.Revision, diagnostic, false);
                var preview = unverified.Length == 1 ? Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order.given.a_timeline.Propose(workspace, unverified[0]) : null;
                Xunit.Assert.Fail($"{name}: {diagnostic.Message}; unverified={unverified.Length}; conflicts={string.Join(';', preview?.Conflicts.Select(conflict => conflict.Message) ?? [])}; ESM present={workspace.Compilation.Value is not null}; baseline errors={string.Join(';', workspace.Compilation.Diagnostics.Where(value => value.Severity == DiagnosticSeverity.Error).Select(value => value.Code + ':' + value.Message))}");
            }
            var repair = repairs.Single();
            var operation = repair.Operations.Single();
            var actual = operation switch
            {
                MoveWorkspaceNode move => move.Expected switch
                {
                    FeatureSyntax feature => feature.Name,
                    SliceSyntax slice => slice.Name,
                    FileImportSyntax import => import.Pattern,
                    _ => string.Empty
                },
                AddWorkspaceNode add => ((FileImportSyntax)add.Node).Pattern,
                ReplaceWorkspaceNode { Expected: ApplicationSyntax before, Node: ApplicationSyntax after } => string.Join(',', after.FileImports.Select(import => import.Pattern).Except(before.FileImports.Select(import => import.Pattern), StringComparer.Ordinal)),
                _ => string.Empty
            };
            actual.ShouldEqual(expected);
            output.WriteLine($"{name}: {diagnostic.Message} -> {actual}");
            Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order.given.a_timeline.Preserves(workspace, repair);
        }

        offered.ShouldEqual(repairable);
        var remaining = workspace;
        for (var step = 0; step < findings; step++)
        {
            var current = WorkspaceSyntaxIndex.Create(remaining);
            var next = current.RepairableDiagnostics.SelectMany(diagnostic => WorkspaceDiagnosticRepairs.Find(current, remaining.Revision, diagnostic))
                .FirstOrDefault(repair => repair.DiagnosticCode == DiagnosticCodes.EventFromLaterSlice);
            if (next is null) break;
            var result = Workspaces.for_WorkspaceAuthoring.when_repairing_timeline_order.given.a_timeline.Propose(remaining, next);
            result.Accepted.ShouldBeTrue();
            var removed = remaining.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice)
                .Select(diagnostic => diagnostic.Message).Except(result.Workspace!.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).Select(diagnostic => diagnostic.Message));
            output.WriteLine($"{name} rediscovery step {step + 1}: {string.Join(';', removed)}");
            remaining = result.Workspace!;
        }

        remaining.Compilation.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.EventFromLaterSlice).All(diagnostic => Expected(name, diagnostic) is null).ShouldBeTrue();
    }

    static string? Expected(string sample, Diagnostic diagnostic)
    {
        if (diagnostic.Code == DiagnosticCodes.TimelineCycleGroup) return null;
        string? Event(string name, string repair) => diagnostic.Message.Contains($"event '{name}'", StringComparison.Ordinal) ? repair : null;

        return sample switch
        {
            "Library" => "Loans",
            "Invoicing" => Event("InvoiceReminderSent", "ChaseOverdueInvoices"),
            "Commerce" => Event("ProductRegistered", "RegisterProduct.play") ?? Event("OrderPlaced", "PlaceOrder.play") ?? Event("OrderPaid", "Payments/Payments.play") ?? Event("ShipmentRequested", "StartFulfillment.play"),
            "TimeTracking" => (diagnostic.Message.Contains("reads read model 'DraftTimesheet'", StringComparison.Ordinal) ? "Engagements/Engagements.play,Timesheets/Timesheets.play" : null) ??
                (diagnostic.Message.Contains("Slice 'QueueApprovedTimesheets'", StringComparison.Ordinal) ? "Engagements/Engagements.play,Timesheets/Timesheets.play" : null) ??
                Event("TimesheetApproved", "Approval") ?? Event("TimesheetRejected", "Approval") ?? Event("TimesheetStarted", "Recording/StartingAWeek.play") ?? Event("TimesheetSubmitted", "Recording/SubmittingTheWeek.play") ?? Event("AbsenceReported", "Absences/ImportingAbsences.play"),
            _ => null
        };
    }
}
