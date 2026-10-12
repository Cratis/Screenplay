// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_validating_query_filters : Specification
{
    SemanticApplication _application;
    SemanticSlice _slice;

    void Establish()
    {
        _application = query_filter_source.Compile().Model.Application;
        _slice = _application.Modules.Single().Features.Single().Slices.Single();
    }

    [Fact] void should_refuse_filters_in_frozen_versions() => Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V1, SemanticVersion.V1, _application)).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_v14_without_filters() => Refuse(_slice with { Queries = [.. _slice.Queries.Select(query => query with { Filters = [] })] }).ShouldBeOfExactType<InvalidSemanticContract>();

    [Theory]
    [InlineData("property")]
    [InlineData("name")]
    [InlineData("type")]
    [InlineData("collection")]
    [InlineData("cardinality")]
    [InlineData("duplicate")]
    [InlineData("by")]
    void should_refuse_invalid_declarations(string kind)
    {
        var query = _slice.Queries.Single(value => value.Name == "Filtered");
        var filter = query.Filters.First();
        query = kind switch
        {
            "property" => query with { Filters = [filter with { Property = query.Id }] },
            "name" => query with { Filters = [filter with { Name = "unknown" }] },
            "type" => query with { Filters = [filter with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Boolean) }] },
            "collection" => query with { Filters = [filter with { Type = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text, isCollection: true) }] },
            "cardinality" => query with { Cardinality = SemanticQueryCardinality.ZeroOrOne },
            "duplicate" => query with { Filters = [filter, filter with { Id = query.Filters.Last().Id }] },
            "by" => query with { Argument = new(query.Filters.Last().Id, filter.Name, filter.Type), KeyProperty = filter.Property, Filters = [filter] },
            _ => query
        };
        Refuse(_slice with { Queries = [.. _slice.Queries.Select(value => value.Name == query.Name ? query : value)] }).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Theory]
    [InlineData("unknown")]
    [InlineData("repeated")]
    [InlineData("missing")]
    [InlineData("null")]
    [InlineData("type")]
    void should_refuse_invalid_specification_filter_values(string kind)
    {
        var specification = _slice.Specifications.Single(value => value.Name == "ReversedRows");
        var result = specification.ThenQueries.Single();
        var value = result.Filters.Single();
        result = kind switch
        {
            "unknown" => result with { Filters = [value with { Filter = result.Query }] },
            "repeated" => result with { Filters = [value, value] },
            "missing" => result with { Filters = [] },
            "null" => result with { Filters = [value with { Value = SemanticValue.Null }] },
            "type" => result with { Filters = [value with { Value = SemanticValue.Number(1) }] },
            _ => result
        };
        Refuse(_slice with { Specifications = [specification with { ThenQueries = [result] }] }).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_refuse_identical_query_calls()
    {
        var specification = _slice.Specifications.Single(value => value.Name == "DistinctCalls");
        Refuse(_slice with { Specifications = [specification with { ThenQueries = [specification.ThenQueries[0], specification.ThenQueries[0]] }] }).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_distinguish_omission_from_a_supplied_optional_filter()
    {
        var specification = _slice.Specifications.Single(value => value.Name == "DistinctCalls");
        var result = specification.ThenQueries[0];
        var optional = _slice.Queries.Single(query => query.Id == result.Query).Filters.Last();
        Refuse(_slice with { Specifications = [specification with { ThenQueries = [result, result with { Filters = result.Filters.Add(new(optional.Id, SemanticValue.Text("x"))) }] }] }).ShouldBeNull();
    }

    Exception? Refuse(SemanticSlice slice)
    {
        var module = _application.Modules.Single();
        var feature = module.Features.Single();
        var application = _application with { Modules = [module with { Features = [feature with { Slices = [slice] }] }] };

        return Catch.Exception(() => ExecutableSemanticModel.Create(LanguageVersion.V14, SemanticVersion.V14, application));
    }
}
