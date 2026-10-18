// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceProductionDestinationRepairs;

public class when_establishing_complete_destinations : given.a_destination_model
{
    [Fact]
    void should_discover_occurrences_without_guessing_a_no_input_repair()
    {
        var workspace = Create();
        var rows = WorkspaceProductionDestinationRepairs.Discover(workspace);
        rows.Length.ShouldEqual(3);
        rows.Select(row => row.Subject).Distinct().Count().ShouldEqual(3);
        rows[1].Event.ShouldEqual(rows[2].Event);
        rows[2].Condition.ShouldNotBeNull();
        rows[0].ExistingFor.ShouldBeNull();
        foreach (var diagnostic in rows[0].Diagnostics)
            WorkspaceDiagnosticRepairs.Find(workspace, workspace.Revision, diagnostic).ShouldBeEmpty();
    }

    [Fact]
    void should_bind_one_complete_candidate_without_claiming_a_baseline()
    {
        var workspace = Create();
        var result = Propose(workspace);
        result.Accepted.ShouldBeTrue();
        result.ExecutableReady.ShouldBeTrue();
        var report = result.ProductionDestinationReport!;
        report.Rows.Length.ShouldEqual(3);
        report.Rows.Count(row => row.Classification == "MadeExplicit").ShouldEqual(2);
        report.Rows[2].Classification.ShouldEqual("ExplicitUnchanged");
        report.Rows.All(row => row.EffectiveClassification == "EstablishedFromInvalidModel").ShouldBeTrue();
        report.BaselineExecutableAvailable.ShouldBeFalse();
        report.PreservationProven.ShouldBeFalse();
        report.NewIdentities.ShouldNotBeEmpty();
        result.Workspace!.Documents.Single().Text.ShouldContain("// keep production comment");
        workspace.Documents.Single().Text.ShouldEqual(Source);
        var candidate = WorkspaceSyntaxIndex.Create(result.Workspace);
        candidate.Entries.Where(entry => entry.Node is ProducesSyntax).All(entry => ((ProducesSyntax)entry.Node).For is not null).ShouldBeTrue();
        result.ExecutableDiagnostics.Any(diagnostic => diagnostic.Code == "PLAY0470").ShouldBeFalse();
    }

    [Fact]
    void should_confirm_inherited_named_routes_by_exact_declaration_handles()
    {
        var source = "eventsource Project\n  identifier Uuid\n  stream Ledger\n" + Source.Replace("        // keep", "        stream Project.Ledger\n        // keep", StringComparison.Ordinal);
        var workspace = Create(source);
        var rows = WorkspaceProductionDestinationRepairs.Discover(workspace);
        rows.Length.ShouldEqual(3);
        rows.All(row => row.Route is { Kind: "Named", Source: not null, Stream: not null }).ShouldBeTrue();
        var result = Propose(workspace);
        result.Accepted.ShouldBeTrue();
        result.ProductionDestinationReport!.Rows.All(row => row.Candidate.Route is not null).ShouldBeTrue();
    }

    [Fact]
    void should_verify_a_new_intent_afresh_instead_of_reusing_an_intent_free_verdict()
    {
        var workspace = Create();
        Propose(workspace).Accepted.ShouldBeTrue();
        var intent = Intent(WorkspaceProductionDestinationRepairs.Discover(workspace));
        intent = intent with { Productions = [.. intent.Productions.Select(selection => selection with { For = new PathExpressionSyntax("missing", selection.For.Location) })] };
        Propose(workspace, intent).Accepted.ShouldBeFalse();
    }
}
