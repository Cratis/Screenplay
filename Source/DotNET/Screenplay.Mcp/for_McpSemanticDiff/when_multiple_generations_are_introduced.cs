// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp.for_McpSemanticDiff;

public class when_multiple_generations_are_introduced : given.a_semantic_comparison
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
            Operations = [new ReplaceWorkspaceDocument { Document = Workspace.Documents[0].Id, Bytes = [.. Encoding.UTF8.GetBytes(source + "      event Registered generation 2\n        name String\n        extra String\n      event Registered generation 3\n        name String\n")] }],
            SemanticRenames = [new(property, SemanticAddress.ForEventProperty(address, new(1), "name"))],
            EventRevisionAdvancements = [new(address, new(3))]
        });
        Assert.True(result.Success, string.Join("; ", result.Conflicts.Select(conflict => conflict.Message).Concat(result.Diagnostics.Select(diagnostic => diagnostic.Message))));
        Proposal = new McpProposal(Workspace, result);
        Diff = Read();
    }

    [Fact] void should_report_the_intermediate_property_addition() => Items("events").Any(item => item.GetProperty("changeKind").GetString() == "property-added" && item.GetProperty("afterGeneration").GetUInt32() == 2 && item.GetProperty("member").GetString() == "extra").ShouldBeTrue();
    [Fact] void should_report_the_subsequent_property_removal() => Items("events").Any(item => item.GetProperty("changeKind").GetString() == "property-removed" && item.GetProperty("afterGeneration").GetUInt32() == 3 && item.GetProperty("member").GetString() == "extra").ShouldBeTrue();
    [Fact] void should_compare_both_new_generations() => Items("events").Count(item => item.GetProperty("changeKind").GetString() == "generation-added").ShouldEqual(2);
    [Fact] void should_report_each_transition_as_covered_by_retained_generations() => Items("events").All(item => item.GetProperty("generationCovered").GetBoolean()).ShouldBeTrue();
}
