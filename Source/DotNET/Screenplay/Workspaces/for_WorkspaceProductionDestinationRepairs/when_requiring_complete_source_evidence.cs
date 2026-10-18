// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces.for_WorkspaceProductionDestinationRepairs;

public class when_requiring_complete_source_evidence : given.a_destination_model
{
    [Theory]
    [InlineData("optional")]
    [InlineData("collection")]
    void should_refuse_an_incompatible_selected_property(string shape)
    {
        var source = Source.Replace("        otherId Uuid", shape == "optional" ? "        otherId Uuid optional" : "        otherId Uuid[]", StringComparison.Ordinal);
        var workspace = Create(source);
        var rows = WorkspaceProductionDestinationRepairs.Discover(workspace);
        rows.Length.ShouldEqual(3);
        var intent = Intent(rows);
        intent = intent with { Productions = [intent.Productions[0] with { For = new PathExpressionSyntax("otherId", intent.Productions[0].For.Location) }, .. intent.Productions.Skip(1)] };
        var result = Propose(workspace, intent);
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
        result.ProductionDestinationReport.ShouldBeNull();
    }

    [Fact]
    void should_not_offer_intent_for_a_command_without_an_actual_diagnostic()
    {
        var valid = Source.Replace("        produces event Created\n", "        produces event Created\n          for id\n", StringComparison.Ordinal)
            .Replace("        produces Existing\n", "        produces Existing\n          for id\n", StringComparison.Ordinal);
        var workspace = Create(valid);
        WorkspaceProductionDestinationRepairs.Discover(workspace).ShouldBeEmpty();
        var index = WorkspaceSyntaxIndex.Create(workspace);
        var command = index.Entries.Single(entry => entry.Node is CommandSyntax);
        var result = WorkspaceProductionDestinationRepairs.Propose(workspace, command.Handle, new(1, []), Request(workspace));
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
    }

    [Fact]
    void should_refuse_missing_imports_without_synthesizing_a_partial_occurrence_set()
    {
        var workspace = Create("import \"Missing.play\"\n" + Source);
        WorkspaceProductionDestinationRepairs.Discover(workspace).ShouldBeEmpty();
    }

    [Fact]
    void should_reject_legacy_confirmation_for_inherited_named_routing()
    {
        var workspace = Create("eventsource Project\n  identifier Uuid\n  stream Ledger\n" + Source.Replace("        // keep", "        stream Project.Ledger\n        // keep", StringComparison.Ordinal));
        var intent = Intent(WorkspaceProductionDestinationRepairs.Discover(workspace));
        intent = intent with { Productions = [.. intent.Productions.Select(selection => selection with { Route = new("Legacy") })] };
        var result = Propose(workspace, intent);
        result.Accepted.ShouldBeFalse();
        result.WritePlan.ShouldBeNull();
    }

    [Fact]
    void should_preserve_all_prior_assignments_from_a_valid_catalog()
    {
        var initial = Propose(Create()).Workspace!;
        var original = initial.Documents.Single();
        var invalid = ScreenplayWorkspace.Create(initial.IdentityCatalog.Application, initial.ApplicationName,
            [WorkspaceDocument.Create(original.Id, original.StableKey, original.Path, System.Text.Encoding.UTF8.GetBytes(Source))], initial.IdentityCatalog);
        var result = Propose(invalid);
        result.Accepted.ShouldBeTrue();
        var catalog = result.Workspace!.IdentityCatalog;
        initial.IdentityCatalog.Documents.All(catalog.Documents.Contains).ShouldBeTrue();
        initial.IdentityCatalog.Semantics.All(catalog.Semantics.Contains).ShouldBeTrue();
        initial.IdentityCatalog.EventContracts.All(catalog.EventContracts.Contains).ShouldBeTrue();
        result.ProductionDestinationReport!.NewIdentities.ShouldBeEmpty();
    }
}
