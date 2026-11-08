// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Api.for_ApiDescription;

public class when_describing_with_command_names_in_every_route : given.a_lending_model
{
    ApiDescription _description;

    void Because() => _description = ApiDescription.For(Model, new ApiConventions("service/v1", true));

    [Fact] void should_name_the_only_command_of_a_slice() => _description.Operations.Single(operation => operation.Name == "AddBook").Route.ShouldEqual("/service/v1/lending/catalog/add-book/add-book");
    [Fact] void should_use_the_route_prefix_for_queries() => _description.Operations.Single(operation => operation.Name == "AllBooks").Route.ShouldEqual("/service/v1/lending/catalog/catalog-list/all-books");
}
