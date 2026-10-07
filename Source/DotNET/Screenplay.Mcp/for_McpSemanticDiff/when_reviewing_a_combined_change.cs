// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_reviewing_a_combined_change : given.a_semantic_comparison
{
    string _renamedId = string.Empty;
    string _eventId = string.Empty;
    string _sliceId = string.Empty;

    void Because()
    {
        var property = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property && assignment.Address.OwnerKind == SemanticKind.ReadModel && assignment.Address.Name == "name" && assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Slice && part.Key == "List"));
        _renamedId = property.Id.ToString();
        _eventId = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.EventContract).Id.ToString();
        _sliceId = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Slice && assignment.Address.Name == "Obsolete").Id.ToString();
        var owner = SemanticAddress.ForReadModel(SemanticAddress.ForSlice(Workspace.IdentityCatalog.Application, "Projects", "Registration", "List"), "Projects");
        var retired = Workspace.IdentityCatalog.Semantics.Where(assignment => assignment.Address.Parts.Any(part => part.Kind == SemanticAddressPartKind.Slice && part.Key == "Obsolete")).Select(assignment => assignment.Address);
        var source = Source.Replace("readmodel Projects\n        name String", "readmodel Projects\n        title String", StringComparison.Ordinal)
            .Replace("event Registered\n        name String", "event Registered\n        name String\n        extra String", StringComparison.Ordinal)
            .Replace("          name = name", "          name = name\n          extra = \"Extra\"", StringComparison.Ordinal)
            .Replace("then Registered\n          name = \"First\"", "then Registered\n          name = \"First\"\n          extra = \"Extra\"", StringComparison.Ordinal);
        source = source[..source.IndexOf("    slice StateView Obsolete", StringComparison.Ordinal)];
        var result = Workspace.Propose(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument { Document = Workspace.Documents[0].Id, Bytes = [.. Encoding.UTF8.GetBytes(source)] }],
            SemanticRenames = [new(property.Address, SemanticAddress.ForProperty(owner, "title"))],
            RetiredSemanticAddresses = [.. retired]
        });
        Assert.True(result.Success, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message).Concat(result.Diagnostics.Select(diagnostic => diagnostic.Message))));
        Proposal = new McpProposal(Workspace, result);
        Diff = Read();
    }

    [Fact] void should_preserve_and_report_the_renamed_member_identity() => Items("declarations").Any(item => item.GetProperty("semanticId").GetString() == _renamedId && item.GetProperty("changeKind").GetString() == "renamed").ShouldBeTrue();
    [Fact] void should_flag_the_event_property_addition_as_contract_breaking() => Items("events").Single().GetProperty("contractBreaking").GetBoolean().ShouldBeTrue();
    [Fact] void should_not_claim_an_unmarked_generation_covers_the_change() => Items("events").Single().GetProperty("generationCovered").GetBoolean().ShouldBeFalse();
    [Fact] void should_report_the_removed_slice() => Items("declarations").Any(item => item.GetProperty("semanticId").GetString() == _sliceId && item.GetProperty("changeKind").GetString() == "removed").ShouldBeTrue();
    [Fact] void should_report_direct_dependants_for_the_renamed_property() => Items("dependants").Any(item => item.GetProperty("semanticId").GetString() == _renamedId && item.GetProperty("dependantAddress").GetString().EndsWith(".All", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_direct_dependants_for_the_event() => Items("dependants").Any(item => item.GetProperty("semanticId").GetString() == _eventId && item.GetProperty("role").GetString() == "produces").ShouldBeTrue();
    [Fact] void should_report_dependants_of_declarations_inside_the_removed_slice() => Items("dependants").Any(item => item.GetProperty("semanticId").GetString() == _sliceId && item.GetProperty("dependantAddress").GetString().EndsWith(".History", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_migrated_identity() => Items("identities").Any(item => item.GetProperty("semanticId").GetString() == _renamedId && item.GetProperty("changeKind").GetString() == "migrated").ShouldBeTrue();
    [Fact] void should_report_a_semantic_change() => Diff.GetProperty("hasSemanticChange").GetBoolean().ShouldBeTrue();
}
