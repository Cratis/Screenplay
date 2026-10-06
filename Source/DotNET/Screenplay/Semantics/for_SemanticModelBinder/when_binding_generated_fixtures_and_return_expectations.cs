// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_generated_fixtures_and_return_expectations : given.a_semantic_binder
{
    const string Prefix = "concept Id : Uuid\nmodule M\n  feature F\n    slice StateChange S\n      command C\n        id Id generated identifier\n        receipt Id generated\n        name String\n";

    [Theory]
    [InlineData("111111111111111111111111111111AA")]
    [InlineData("{11111111-1111-1111-1111-1111111111AA}")]
    [InlineData("11111111-1111-1111-1111-1111111111AA")]
    void should_lower_identifier_fixtures_and_generated_return_expectations_to_normalized_uuid_values(string uuid)
    {
        var result = Bind(Prefix + $"        returns id\n      specification Accepted\n        when C\n          for \"{uuid}\"\n          name = \"hello\"\n          generated receipt = \"222222222222222222222222222222AA\"\n        then returns \"{uuid}\"");
        result.Success.ShouldBeTrue();
        var slice = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single();
        var specification = slice.Specifications.Single();
        var command = slice.Commands.Single();
        specification.When!.EventSource.ShouldBeNull();
        specification.When.Values.Select(value => value.TargetProperty).ShouldContainOnly(command.Properties.Single(property => property.Name == "name").Id);
        specification.When.GeneratedValues.Length.ShouldEqual(2);
        specification.When.GeneratedValues.Single(value => value.TargetProperty == command.Properties.Single(property => property.Name == "id").Id).Value.ShouldEqual(SemanticValue.Text("11111111-1111-1111-1111-1111111111aa"));
        ((SemanticScalarSpecificationResponse)specification.ThenReturns!).Value.ShouldEqual(SemanticValue.Text("11111111-1111-1111-1111-1111111111aa"));
    }

    [Fact]
    void should_bind_a_record_subset_and_an_optional_null()
    {
        var result = Bind(Prefix + "        note String?\n        returns\n          receipt = receipt\n          note = note\n      specification Accepted\n        when C\n          name = \"hello\"\n          note = \"present\"\n        then returns\n          note = null");
        result.Success.ShouldBeTrue();
        var expected = (SemanticRecordSpecificationResponse)result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().ThenReturns!;
        expected.Fields.Single().Name.ShouldEqual("note");
        expected.Fields.Single().Value.ShouldEqual(SemanticValue.Null);
    }

    [Fact]
    void should_allow_missing_fixtures_at_binding()
    {
        var result = Bind(Prefix + "        returns id\n      specification Accepted\n        when C\n          name = \"hello\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        result.Success.ShouldBeTrue();
        result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!.GeneratedValues.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("42")]
    [InlineData("\"not-a-uuid\"")]
    void should_refuse_incompatible_identifier_fixtures(string value)
    {
        var result = Bind(Prefix + $"        returns id\n      specification Accepted\n        when C\n          for {value}\n          name = \"hello\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273").ShouldBeTrue();
    }

    [Theory]
    [InlineData("id")]
    [InlineData("name")]
    [InlineData("foreign")]
    void should_refuse_non_generated_or_identifier_targets_in_the_generated_clause(string target)
    {
        var result = Bind(Prefix + $"        returns id\n      specification Accepted\n        when C\n          name = \"hello\"\n          generated {target} = \"11111111-1111-1111-1111-111111111111\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0273").ShouldBeTrue();
    }

    [Fact]
    void should_keep_for_on_a_nongenerated_identifier_in_the_event_source_channel()
    {
        var result = Bind(Prefix.Replace("id Id generated identifier", "id Id identifier", StringComparison.Ordinal) + "        returns id\n      specification Accepted\n        when C\n          for \"11111111-1111-1111-1111-111111111111\"\n          id = \"11111111-1111-1111-1111-111111111111\"\n          name = \"hello\"\n          generated receipt = \"22222222-2222-2222-2222-222222222222\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        result.Success.ShouldBeTrue();
        var when = result.Value!.Model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().When!;
        when.EventSource.ShouldNotBeNull();
        when.GeneratedValues.Length.ShouldEqual(1);
    }

    [Fact]
    void should_refuse_generated_values_in_the_request_shape()
    {
        var result = Bind(Prefix + "        returns id\n      specification Accepted\n        when C\n          id = \"11111111-1111-1111-1111-111111111111\"\n          name = \"hello\"\n        then returns \"11111111-1111-1111-1111-111111111111\"");
        result.Success.ShouldBeFalse();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0485").ShouldBeTrue();
    }
}
