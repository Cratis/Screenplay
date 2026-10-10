// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison.for_ModelComparison;

public class when_an_event_gains_a_property : given.two_models
{
    void Establish()
    {
        _before = Create(EventSource);
        ChangeSource(EventSource + "        extra String\n");
    }

    void Because() => _result = ModelComparison.Compare(ComparedModel.WithIdentities(_before), ComparedModel.WithIdentities(_after));

    [Fact] void should_keep_contract_risk_visible() => _result.Events.Single().ContractBreaking.ShouldBeTrue();
    [Fact] void should_not_claim_generation_coverage() => _result.Events.Single().GenerationCovered.ShouldBeFalse();
    [Fact] void should_report_property_addition() => _result.Events.Single().Change.ShouldEqual(EventContractChangeKind.PropertyAdded);
}
