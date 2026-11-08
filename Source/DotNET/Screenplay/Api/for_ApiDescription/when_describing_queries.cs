// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Api.for_ApiDescription;

public class when_describing_queries : given.a_lending_model
{
    ApiDescription _description;

    void Because() => _description = ApiDescription.For(Model);

    ApiOperation Query(string name) => _description.Operations.Single(operation => operation.Kind == ApiOperationKind.Query && operation.Name == name);

    [Fact] void should_get_every_query() => _description.Operations.Where(operation => operation.Kind == ApiOperationKind.Query).Select(operation => operation.Method).ShouldContainOnly(["GET", "GET"]);
    [Fact] void should_route_a_query_under_its_slice_by_name() => Query("AllBooks").Route.ShouldEqual("/api/lending/catalog/catalog-list/all-books");
    [Fact] void should_describe_an_observable_query_as_live() => Query("AllBooks").Live.ShouldBeTrue();
    [Fact] void should_describe_a_snapshot_query_as_not_live() => Query("BookById").Live.ShouldBeFalse();
    [Fact] void should_read_the_key_from_the_query_string() => Query("BookById").Parameters.Select(parameter => (parameter.Name, parameter.Location, parameter.Required)).ShouldContainOnly([("bookId", ApiParameterLocation.Query, true)]);
    [Fact] void should_accept_paging_and_sorting_for_a_list() => Query("AllBooks").Parameters.Select(parameter => parameter.Name).ShouldContainOnly(["page", "pageSize", "sortBy", "sortDirection"]);
    [Fact] void should_return_the_read_model() => Query("AllBooks").ReadModel!.Name.ShouldEqual("CatalogEntry");
    [Fact] void should_return_many_from_a_list_query() => Query("AllBooks").Cardinality.ShouldEqual(SemanticQueryCardinality.Many);
}
