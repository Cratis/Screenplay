// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_establishing_malformed_facts : for_SemanticSpecificationRunner.given.a_bound_register_project_plan
{
    SemanticExecutionResult _default = null!;
    SemanticExecutionResult _unknown = null!;
    SemanticExecutionResult _missing = null!;
    SemanticExecutionResult _badPayload = null!;
    SemanticExecutionResult _duplicate = null!;

    void Because()
    {
        var @event = _plan.Events.Values.Single();
        var key = SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6");
        var values = @event.Properties.Select(property => new SemanticPropertyValue(property.Id, key)).ToImmutableArray();
        var evaluator = new SemanticEvaluator();
        _default = evaluator.EstablishWorld(_plan, default);
        _unknown = evaluator.EstablishWorld(_plan, [new(SemanticId.Create(SemanticKind.EventContract, "not-in-plan"), key, values)]);
        _missing = evaluator.EstablishWorld(_plan, [new(@event.Id, key, [])]);
        _badPayload = evaluator.EstablishWorld(_plan, [new(@event.Id, key, [.. values.Select(value => value with { Value = SemanticValue.Number(42) })])]);
        _duplicate = evaluator.EstablishWorld(_plan, [new(@event.Id, key, [values[0], values[0]])]);
    }

    [Fact] void should_reject_default_facts() => AssertRejected(_default);
    [Fact] void should_reject_unknown_event_contracts() => AssertRejected(_unknown);
    [Fact] void should_reject_missing_event_properties() => AssertRejected(_missing);
    [Fact] void should_reject_invalid_payload_types() => AssertRejected(_badPayload);
    [Fact] void should_reject_duplicated_event_properties() => AssertRejected(_duplicate);

    static void AssertRejected(SemanticExecutionResult result)
    {
        result.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)result).Category.ShouldEqual(SemanticRejectionCategory.Contract);
        ReferenceEquals(result.World, SemanticWorld.Empty).ShouldBeTrue();
    }
}
