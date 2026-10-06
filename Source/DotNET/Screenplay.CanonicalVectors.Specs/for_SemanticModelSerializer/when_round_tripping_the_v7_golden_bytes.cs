// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalVectors.Specs.given;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_v7_golden_bytes : Specification
{
    byte[] _expected = [];
    ExecutableSemanticModel _model = null!;
    SemanticSlice _slice = null!;

    void Establish() => _expected = canonical_serialization_golden_bytes.SemanticModelV7;
    void Because()
    {
        _model = SemanticModelSerializer.Deserialize(_expected);
        _slice = _model.Application.Modules.SelectMany(module => module.Features).Single(feature => feature.Name == "GeneratedResponses").Slices.Single();
    }

    [Fact] void should_preserve_exact_bytes() => SemanticModelSerializer.Serialize(_model).SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_pin_the_revision() => _model.Revision.ToString().ShouldEqual("rev1:479a90501b463483be11c1938aad7d8a3b3c2d61218d6f47d487f86dee7c517e");
    [Fact] void should_select_v7() => (_model.LanguageVersion == LanguageVersion.V7 && _model.SemanticVersion == SemanticVersion.V7).ShouldBeTrue();
    [Fact] void should_keep_authored_response_order() => ((SemanticRecordCommandResponse)_slice.Commands.Single(command => command.Name == "CreateToken").Response).Fields.Select(field => field.Name).ShouldEqual(["token", "note"]);
    [Fact] void should_sort_return_expectations_by_name() => ((SemanticRecordSpecificationResponse)_slice.Specifications.Single(specification => specification.Name == "returns a record with a null field").ThenReturns).Fields.Select(field => field.Name).ShouldEqual(["note", "token"]);
    [Fact]
    void should_distinguish_scalar_null_from_a_null_record_field()
    {
        ((SemanticScalarSpecificationResponse)_slice.Specifications.Single(specification => specification.Name == "returns null").ThenReturns).Value.ShouldBeOfExactType<SemanticNullValue>();
        ((SemanticRecordSpecificationResponse)_slice.Specifications.Single(specification => specification.Name == "returns a record with a null field").ThenReturns).Fields.Single(field => field.Name == "note").Value.ShouldBeOfExactType<SemanticNullValue>();
    }
    [Fact]
    void should_keep_generation_separate_from_inputs()
    {
        var command = _slice.Commands.Single(value => value.Name == "CreateEntity");
        command.Properties.Single().IsGenerated.ShouldBeTrue();
        var when = _slice.Specifications.Single(value => value.Name == "returns a generated identifier").When;
        when.Values.IsEmpty.ShouldBeTrue();
        when.GeneratedValues.Single().TargetProperty.ShouldEqual(command.Properties.Single().Id);
        when.EventSource.ShouldBeNull();
    }
    [Fact] void should_retain_prior_contracts() => _model.Application.Triggers.Single().Name.ShouldEqual("BatchArrived");
}
