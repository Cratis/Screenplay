// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_a_generation_covers_the_change : given.two_models
{
    void Establish()
    {
        _before = Create(EventSource);
        var contract = _before.IdentityCatalog.EventContracts.Single().Address;
        var property = _before.IdentityCatalog.Semantics.Single(assignment => assignment.Address.Kind == SemanticKind.Property);
        var proposal = _before.Propose(new()
        {
            ExpectedRevision = _before.Revision,
            ExpectedCatalogRevision = _before.IdentityCatalog.Revision,
            Operations = [new ReplaceWorkspaceDocument { Document = _before.Documents[0].Id, Bytes = [.. Encoding.UTF8.GetBytes(EventSource + "      event Registered generation 2\n        name String\n        extra String\n")] }],
            SemanticRenames = [new(property.Address, SemanticAddress.ForEventProperty(contract, new(1), "name"))],
            EventRevisionAdvancements = [new(contract, new(2))]
        });
        Assert.True(proposal.Success);
        _after = proposal.Workspace!;
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_report_covered_shape_change() => _result.Events.Single(change => change.Change == EventContractChangeKind.PropertyAdded).GenerationCovered.ShouldBeTrue();
    [Fact] void should_not_hide_contract_risk() => _result.Events.Single(change => change.Change == EventContractChangeKind.PropertyAdded).ContractBreaking.ShouldBeTrue();
}
