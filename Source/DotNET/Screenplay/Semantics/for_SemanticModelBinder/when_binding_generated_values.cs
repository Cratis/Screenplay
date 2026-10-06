// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_generated_values : given.a_semantic_binder
{
    const string Source = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        receipt Id generated\n        name String\n        produces event Created\n          id Id = id\n          receipt Id = receipt\n        returns id";
    CompilationResult<SemanticCompilation> _result;
    SemanticCommand _command;

    void Because()
    {
        _result = Bind(Source);
        if (_result.Success) _command = _result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
    }

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_v7() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V7);
    [Fact] void should_bind_generated_properties() => _command.Properties.Where(property => property.IsGenerated).Select(property => property.Name).ShouldContainOnly("id", "receipt");
    [Fact] void should_keep_the_inline_destination() => ((SemanticResolvedExpression)_command.Destination!.Value!).Target.ShouldEqual(_command.Properties.Single(property => property.Name == "id").Id);
    [Fact] void should_bind_the_response_to_the_generated_identifier() => ((SemanticScalarCommandResponse)_command.Response!).Source.ShouldEqual(_command.Properties.Single(property => property.Name == "id").Id);

    [Fact]
    void should_keep_the_property_identity_when_generated_is_added()
    {
        var ordinary = Bind(Source.Replace(" generated", string.Empty, StringComparison.Ordinal));
        ordinary.Success.ShouldBeTrue();
        _command.Properties.Select(property => property.Id).ShouldEqual(ordinary.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single().Properties.Select(property => property.Id));
    }

    [Theory]
    [InlineData(false, "Id")]
    [InlineData(true, "Id")]
    [InlineData(false, "OtherId")]
    [InlineData(true, "String")]
    void should_bind_a_generated_identifier_fixture_separately_from_an_explicitly_routed_production(bool sharedPayloadName, string routeType)
    {
        var source = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        other Id\n        returns id\n        produces Created\n          for other\n      event Created\n      specification Accepted\n        when C\n          for \"11111111-1111-1111-1111-111111111111\"\n          other = \"22222222-2222-2222-2222-222222222222\"\n        then Created\n          for \"22222222-2222-2222-2222-222222222222\"\n        then returns \"11111111-1111-1111-1111-111111111111\"";
        if (sharedPayloadName)
        {
            source = source.Replace("          for other\n      event Created\n", "          for other\n          other = other\n      event Created\n        other Id\n", StringComparison.Ordinal)
                .Replace("        then returns", "          other = \"22222222-2222-2222-2222-222222222222\"\n        then returns", StringComparison.Ordinal);
        }

        source = "concept OtherId : Uuid\n" + source.Replace("other Id", "other " + routeType, StringComparison.Ordinal);
        var result = Bind(source);
        result.Success.ShouldBeTrue();
        var slice = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        ((SemanticResolvedExpression)command.Produces.Single().Destination!).Target.ShouldEqual(command.Properties.Single(property => property.Name == "other").Id);
        slice.Specifications.Single().When!.EventSource.ShouldBeNull();
        slice.Specifications.Single().When!.GeneratedValues.Single().TargetProperty.ShouldEqual(command.Properties.Single(property => property.Name == "id").Id);
    }

    [Fact]
    void should_not_retarget_plain_legacy_productions_when_another_command_selects_typed_destinations()
    {
        var result = Bind("concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command Typed\n        id Id identifier\n        produces TypedEvent\n          for id\n      event TypedEvent\n      command C\n        id Id generated identifier\n        produces Created\n      event Created");
        result.Success.ShouldBeTrue();
        var command = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single(command => command.Name == "C");
        command.Produces.Single().Destination.ShouldBeNull();
        command.Destination.ShouldBeNull();
    }

    [Fact]
    void should_keep_plain_productions_on_the_legacy_allocated_channel()
    {
        var result = Bind("concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        produces Created\n      event Created");
        result.Success.ShouldBeTrue();
        var command = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Commands.Single();
        command.Produces.Single().Destination.ShouldBeNull();
        command.Destination.ShouldBeNull();
    }
}
