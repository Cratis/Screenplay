// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_reading_typed_capture_keys : Specification
{
    [Fact]
    void should_read_v32_and_execute_the_typed_given_against_its_read_model()
    {
        var vector = EsmGoldenVectors.Get(32);
        var model = SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan());
        model.SemanticVersion.ShouldEqual(SemanticVersion.V32);
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single(value => !value.Captures.IsEmpty);
        var key = SemanticTypeReference.ForConcept(model.Application.Concepts.Single().Id);
        slice.Captures.Single().Appends.Single().EventSourceType.ShouldEqual(key);
        slice.Specifications.Single().GivenEvents.Single().EventSource!.Type.ShouldEqual(key);
        var plan = SemanticExecutionPlan.Compile(model).Plan!;
        new SemanticSpecificationRunner().Run(plan, slice.Specifications.Single().Id).Passed.ShouldBeTrue();
        SemanticModelSerializer.Serialize(model).SequenceEqual(vector.Bytes).ShouldBeTrue();
    }
}
