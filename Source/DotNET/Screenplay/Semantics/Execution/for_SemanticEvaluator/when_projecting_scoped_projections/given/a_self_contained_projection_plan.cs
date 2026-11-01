// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_projecting_scoped_projections.given;

public class a_self_contained_projection_plan : for_SemanticModelBinder.given.a_semantic_binder
{
    protected SemanticExecutionPlanCompilation _compilation;
    protected ExecutableSemanticModel _model;
    protected ImmutableArray<SemanticReadModelInstance> _instances;
    protected string? _failure;

    protected void Plan(string source)
    {
        var compilation = Bind(source);
        compilation.Diagnostics.Where(diagnostic => diagnostic.Severity == Diagnostics.DiagnosticSeverity.Error).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();
        _model = compilation.Value!.Model;
        _compilation = SemanticExecutionPlan.Compile(_model);
        _compilation.Issues.Select(issue => issue.Details).ShouldBeEmpty();
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

    protected SemanticValue Value(string readModel, string key, string property) =>
        Instance(key)!.Values.Single(_ => _.TargetProperty == ReadModelProperty(readModel, property)).Value;

    protected SemanticValue Member(SemanticValue composite, string type, string property) =>
        ((SemanticCompositeValue)composite).Properties.Single(_ => _.TargetProperty == TypeProperty(type, property)).Value;

    protected SemanticId EventId(string name) => Slices.SelectMany(_ => _.Events).Single(_ => _.Name == name).Id;

    protected SemanticId EventProperty(string eventName, string name) =>
        Slices.SelectMany(_ => _.Events).Single(_ => _.Name == eventName).Properties.Single(_ => _.Name == name).Id;

    protected SemanticId ReadModelProperty(string readModel, string name) =>
        Slices.SelectMany(_ => _.ReadModels).Single(_ => _.Name == readModel).Properties.Single(_ => _.Name == name).Id;

    protected SemanticId TypeProperty(string type, string name) =>
        _model.Application.Types.Single(_ => _.Name == type).Properties.Single(_ => _.Name == name).Id;

    IEnumerable<SemanticSlice> Slices => _model.Application.Modules.Single().Features.Single().Slices;
}
