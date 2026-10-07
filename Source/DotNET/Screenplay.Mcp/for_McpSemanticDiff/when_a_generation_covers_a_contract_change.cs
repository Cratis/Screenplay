// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_a_generation_covers_a_contract_change : given.a_semantic_comparison
{
    void Because()
    {
        const string source = "module Projects\n  feature Registration\n    slice StateChange Register\n      event Registered\n        name String\n";
        Workspace = Create(source);
        var address = Workspace.IdentityCatalog.EventContracts.Single().Address;
        var property = Workspace.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property).Address;
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

    [Fact] void should_report_the_new_property() => Items("events").Single().GetProperty("member").GetString().ShouldEqual("extra");
    [Fact] void should_keep_contract_risk_visible() => Items("events").Single().GetProperty("contractBreaking").GetBoolean().ShouldBeTrue();
    [Fact] void should_report_the_preserved_previous_generation_as_coverage() => Items("events").Single().GetProperty("generationCovered").GetBoolean().ShouldBeTrue();
    [Fact] void should_identify_both_generations() => Items("events").Single().GetProperty("afterGeneration").GetUInt32().ShouldEqual(2u);
}
