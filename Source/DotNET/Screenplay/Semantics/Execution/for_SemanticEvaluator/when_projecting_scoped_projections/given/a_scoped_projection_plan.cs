// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections.given;

// Binds one projection over the projection-block declarations, compiles its plan and projects facts through the reference evaluator.
public class a_scoped_projection_plan : for_SemanticModelBinder.when_binding_projection_blocks.given.a_projection_block_binder
{
    protected const string FirstOrder = "00000000-0000-0000-0000-000000000001";
    protected const string SecondOrder = "00000000-0000-0000-0000-000000000002";
    protected const string Customer = "00000000-0000-0000-0000-0000000000c1";

    protected SemanticExecutionPlanCompilation _compilation;
    protected ImmutableArray<SemanticReadModelInstance> _instances;
    protected string? _failure;

    protected void Plan(string body, string header = "projection Orders => OrderView")
    {
        _result = BindProjection(header, body);
        _compilation = SemanticExecutionPlan.Compile(_result.Value!.Model);
    }

    protected void Project(params SemanticFact[] facts)
    {
        SemanticEvaluator.Establish(_compilation.Plan!, [], [.. facts], out _instances, out _failure);
    }

    protected SemanticFact Fact(string eventName, string? eventSource, params (string Property, SemanticValue Value)[] values) =>
        new(
            EventId(eventName),
            eventSource is null ? SemanticValue.Null : SemanticValue.Text(eventSource),
            [.. values.Select(_ => new SemanticPropertyValue(EventProperty(eventName, _.Property), _.Value))]);

    protected static SemanticValue Text(string value) => SemanticValue.Text(value);

    protected static SemanticValue Number(decimal value) => SemanticValue.Number(value);

    protected SemanticReadModelInstance? Instance(string key) =>
        _instances.SingleOrDefault(_ => SemanticValueRules.AreEqual(_.Key, Text(key)));

    protected SemanticValue Value(string key, string property) =>
        Instance(key)!.Values.Single(_ => _.TargetProperty == ReadModelProperty("OrderView", property)).Value;

    protected SemanticValue Member(SemanticValue composite, string type, string property) =>
        ((SemanticCompositeValue)composite).Properties.Single(_ => _.TargetProperty == TypeProperty(type, property)).Value;
}
