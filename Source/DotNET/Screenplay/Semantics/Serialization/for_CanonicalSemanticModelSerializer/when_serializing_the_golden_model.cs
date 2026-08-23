// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_golden_model : Specification
{
    byte[] _expected;
    byte[] _serialized;
    byte[] _reserialized;
    ExecutableSemanticModel _model;
    ExecutableSemanticModel _roundTripped;

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
    }

    [Fact] void should_match_the_checked_in_utf8_bytes() => _serialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_reserialize_the_golden_bytes_identically() => _reserialized.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_distinct_semantic_revision() => _roundTripped.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_keep_the_behavior_order() =>
        _roundTripped.Application.Modules.Single().Features.Single().Features.Single().Slices
            .Single(_ => _.Kind == SemanticSliceKind.StateView).Projections.Single().Transitions
            .Select(_ => _.AffectedInstance.Cardinality)
            .ShouldContainOnly([AffectedInstanceCardinality.ZeroOrOne, AffectedInstanceCardinality.One, AffectedInstanceCardinality.Many]);
}
#endif
