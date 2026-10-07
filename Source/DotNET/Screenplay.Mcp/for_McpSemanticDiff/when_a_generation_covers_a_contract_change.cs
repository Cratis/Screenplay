// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_a_generation_covers_a_contract_change : given.a_semantic_comparison
{
    string _propertyId = string.Empty;

    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        Workspace = Create(source);
        var address = Workspace.IdentityCatalog.EventContracts.Single().Address;
        var assignment = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property);
        var property = assignment.Address;
        _propertyId = assignment.Id.ToString();
        var result = Workspace.Propose(new()
        {
            ExpectedRevision = Workspace.Revision,
            ExpectedCatalogRevision = Workspace.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument { Document = Workspace.Documents[0].Id, Bytes = [.. Encoding.UTF8.GetBytes(source + "      event Registered generation 2\n        name String\n        extra String\n")] }],
            SemanticRenames = [new(property, SemanticAddress.ForEventProperty(address, new(1), "name"))],
            EventRevisionAdvancements = [new(address, new(2))]
        });
        Assert.True(result.Success, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message)));
        Proposal = new McpProposal(Workspace, result);
        Diff = Read();
    }

    [Fact] void should_report_generation_scope_readdressing_as_identity_migration() => Items("identities").Any(item => item.GetProperty("semanticId").GetString() == _propertyId && item.GetProperty("changeKind").GetString() == "migrated").ShouldBeTrue();
    [Fact] void should_not_report_generation_scope_readdressing_as_an_owner_move() => Items("declarations").Any(item => item.GetProperty("semanticId").GetString() == _propertyId && item.GetProperty("changeKind").GetString() == "moved").ShouldBeFalse();
    [Fact] void should_report_the_new_property() => Items("events").Single(item => item.GetProperty("changeKind").GetString() == "property-added").GetProperty("member").GetString().ShouldEqual("extra");
    [Fact] void should_keep_contract_risk_visible() => Items("events").Single(item => item.GetProperty("changeKind").GetString() == "property-added").GetProperty("contractBreaking").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_the_preserved_previous_generation_as_coverage() => Items("events").Single(item => item.GetProperty("changeKind").GetString() == "property-added").GetProperty("generationCovered").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_both_generations() => Items("events").Single(item => item.GetProperty("changeKind").GetString() == "generation-added").GetProperty("afterGeneration").GetUInt32().ShouldEqual(2u);
}
