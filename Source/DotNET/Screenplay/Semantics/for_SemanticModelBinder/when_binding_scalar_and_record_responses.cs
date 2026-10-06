// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_scalar_and_record_responses : given.a_semantic_binder
{
    const string Prefix = "concept Id : Uuid\ntype Detail\n  amount Int\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated\n        note String?\n        detail Detail\n";

    [Theory]
    [InlineData("id")]
    [InlineData("note")]
    [InlineData("detail")]
    void should_infer_the_scalar_source_type(string source)
    {
        var result = Bind(Prefix + "        returns " + source);
        result.Success.ShouldBeTrue();
        var command = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var property = command.Properties.Single(property => property.Name == source);
        var response = (SemanticScalarCommandResponse)command.Response!;
        response.Source.ShouldEqual(property.Id);
        response.Type.ShouldEqual(property.Type);
        command.Produces.ShouldBeEmpty();
    }

    [Fact]
    void should_keep_record_fields_in_authored_order_with_source_types()
    {
        var result = Bind(Prefix + "        returns\n          z = id\n          a String? = note\n          details = detail");
        result.Success.ShouldBeTrue();
        var command = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        var response = (SemanticRecordCommandResponse)command.Response!;
        response.Fields.Select(field => field.Name).ShouldContainOnly("z", "a", "details");
        response.Fields.Select(field => field.Type).ShouldEqual(response.Fields.Select(field => command.Properties.Single(property => property.Id == field.Source).Type));
    }
}
