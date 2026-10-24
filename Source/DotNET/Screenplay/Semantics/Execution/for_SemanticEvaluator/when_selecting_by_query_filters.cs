// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator;

public class when_selecting_by_query_filters : Specification
{
    SemanticExecutionPlan _plan;
    SemanticWorld _world;
    SemanticKeyedQuery _query;

    void Establish()
    {
        _plan = SemanticExecutionPlan.Compile(query_filter_source.Compile().Model).Plan!;
        _query = _plan.Queries.Values.Single(query => query.Name == "Filtered");
        var fixture = _plan.Specifications.Values.Single(specification => specification.Name == "ReversedRows");
        _world = SemanticWorld.Create([], [.. fixture.GivenReadModels.Select(state => new SemanticReadModelInstance(state.ReadModel, state.Key, state.Values))]);
    }

    [Fact] void should_match_every_instance_and_omit_the_optional_filter() => ((SemanticAccepted)Execute([Value("status", SemanticValue.Text("open"))])).Queries.Single().Results.Length.ShouldEqual(2);
    [Fact] void should_combine_supplied_filters_with_and() => ((SemanticAccepted)Execute([Value("status", SemanticValue.Text("open")), Value("category", SemanticValue.Text("x"))])).Queries.Single().Results.Single().Key.ShouldEqual(SemanticValue.Text("a"));
    [Fact] void should_refuse_a_missing_required_filter() => Execute([]).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);
    [Fact] void should_refuse_a_null_filter() => Execute([Value("status", SemanticValue.Null)]).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);
    [Fact] void should_refuse_a_wrong_type() => Execute([Value("status", SemanticValue.Number(1))]).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);
    [Fact] void should_refuse_an_unknown_filter() => Execute([new(_query.Id, SemanticValue.Text("open"))]).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);
    [Fact] void should_refuse_a_repeated_filter() => Execute([Value("status", SemanticValue.Text("open")), Value("status", SemanticValue.Text("open"))]).Kind.ShouldEqual(SemanticExecutionOutcomeKind.Rejected);

    [Fact]
    void should_combine_by_and_filter()
    {
        var query = _plan.Queries.Values.Single(query => query.Name == "InCategory");
        var result = Execute([new(query.Filters.Single().Id, SemanticValue.Text("open"))], query, SemanticValue.Text("x"));
        ((SemanticAccepted)result).Queries.Single().Results.Single().Key.ShouldEqual(SemanticValue.Text("a"));
    }

    [Fact]
    void should_not_match_an_instance_without_the_property()
    {
        var property = _query.Filters.First().Property;
        var world = SemanticWorld.Create([], [.. _world.ReadModels.Select(instance => instance with { Values = [.. instance.Values.Where(value => value.TargetProperty != property)] })]);
        ((SemanticAccepted)Execute(world, [Value("status", SemanticValue.Text("open"))])).Queries.Single().Results.ShouldBeEmpty();
    }

    [Fact]
    void should_echo_values_in_declaration_order()
    {
        var values = new[] { Value("category", SemanticValue.Text("x")), Value("status", SemanticValue.Text("open")) };
        ((SemanticAccepted)Execute([.. values])).Queries.Single().Filters.Select(value => value.Filter).ShouldEqual(_query.Filters.Select(filter => filter.Id));
    }

    SemanticQueryFilterValue Value(string name, SemanticValue value) => new(_query.Filters.Single(filter => filter.Name == name).Id, value);

    SemanticExecutionResult Execute(ImmutableArray<SemanticQueryFilterValue> filters, SemanticKeyedQuery? query = null, SemanticValue? key = null) => Execute(_world, filters, query, key);

    SemanticExecutionResult Execute(SemanticWorld world, ImmutableArray<SemanticQueryFilterValue> filters, SemanticKeyedQuery? query = null, SemanticValue? key = null) => new SemanticEvaluator().Execute(_plan, world,
        new SemanticExecutionRequest(default, [], [new((query ?? _query).Id, key ?? SemanticValue.Null) { Filters = filters }], ImmutableDictionary<SemanticId, SemanticValue>.Empty) { IsReadOnly = true });
}
