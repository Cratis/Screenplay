// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_golden_model : Specification
{
    byte[] _expected;
    byte[] _serialized;
    byte[] _reserialized;
    ExecutableSemanticModel _model;
    ExecutableSemanticModel _roundTripped;
    SemanticSpecificationError[] _errors;
    SemanticProducedEvent[] _produced;

    void Establish()
    {
        _expected = canonical_serialization_golden_vectors.SemanticModelBytes;
        _model = canonical_serialization_golden_vectors.CreateSemanticModel();
    }

    void Because()
    {
        _serialized = SemanticModelSerializer.Serialize(_model);
        _roundTripped = SemanticModelSerializer.Deserialize(_expected);
        _reserialized = SemanticModelSerializer.Serialize(_roundTripped);
        var slices = _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices;
        _errors = [.. slices.SelectMany(_ => _.Specifications).SelectMany(_ => _.ThenErrors)];
        _produced = [.. slices.Single(_ => _.Kind == SemanticSliceKind.StateChange).Commands.Single().Produces];
    }

    [Fact] void should_match_the_checked_in_utf8_bytes() => _serialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_reserialize_the_golden_bytes_identically() => _reserialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_distinct_semantic_revision() => _roundTripped.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_cover_a_produced_event_without_a_condition_or_destination() => _produced.Any(_ => _.Condition is null && _.Destination is null).ShouldBeTrue();
    [Fact] void should_cover_a_bare_rejection() => _errors.Any(_ => _.Code is null && _.Message is null).ShouldBeTrue();
    [Fact] void should_cover_a_message_only_rejection() => _errors.Any(_ => _.Code is null && _.Message == "Title is invalid").ShouldBeTrue();
    [Fact] void should_keep_the_behavior_order() =>
        _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices
            .Single(_ => _.Kind == SemanticSliceKind.StateView).Projections.Single().Transitions
            .Select(_ => _.AffectedInstance.Cardinality)
            .ShouldContainOnly([AffectedInstanceCardinality.ZeroOrOne, AffectedInstanceCardinality.One, AffectedInstanceCardinality.Many]);
}
#endif
