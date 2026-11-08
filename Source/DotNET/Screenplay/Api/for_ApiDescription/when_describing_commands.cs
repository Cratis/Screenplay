// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Api.for_ApiDescription;

public class when_describing_commands : given.a_lending_model
{
    ApiDescription _description;

    void Because() => _description = ApiDescription.For(Model);

    ApiOperation Command(string name) => _description.Operations.Single(operation => operation.Kind == ApiOperationKind.Command && operation.Name == name);

    [Fact] void should_post_every_command() => _description.Operations.Where(operation => operation.Kind == ApiOperationKind.Command).Select(operation => operation.Method).ShouldContainOnly(["POST", "POST", "POST"]);
    [Fact] void should_route_the_only_command_of_a_slice_to_the_slice() => Command("AddBook").Route.ShouldEqual("/api/lending/catalog/add-book");
    [Fact] void should_name_each_command_when_its_slice_declares_several() => Command("RetireBook").Route.ShouldEqual("/api/lending/catalog/shelve-book/retire-book");
    [Fact] void should_offer_a_validation_route() => Command("AddBook").ValidationRoute.ShouldEqual("/api/lending/catalog/add-book/validate");
    [Fact] void should_name_the_declaring_slice() => Command("AddBook").Slice.ShouldEqual("Lending.Catalog.AddBook");
    [Fact] void should_carry_the_semantic_identity() => Command("AddBook").Id.ShouldEqual(Model.Application.Modules[0].Features[0].Slices[0].Commands[0].Id);
    [Fact] void should_take_the_inputs_from_the_body() => Command("AddBook").Parameters.Select(parameter => (parameter.Name, parameter.Location, parameter.Required)).ShouldContainOnly([("bookId", ApiParameterLocation.Body, true), ("title", ApiParameterLocation.Body, true)]);
}
