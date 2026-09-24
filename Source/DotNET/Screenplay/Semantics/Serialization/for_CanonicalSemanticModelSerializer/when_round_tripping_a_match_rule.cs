// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_a_match_rule : Specification
{
    byte[] _serialized;
    ExecutableSemanticModel _roundTrip;

    void Because()
    {
        _serialized = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModel());
        _roundTrip = SemanticModelSerializer.Deserialize(_serialized);
    }

    [Fact] void should_write_the_canonical_discriminator() => Encoding.UTF8.GetString(_serialized).Contains("\"kind\":\"matches\"", StringComparison.Ordinal).ShouldBeTrue();
    [Fact] void should_restore_the_pattern() => _roundTrip.Application.Modules.Single().Features.Single().Features.Single().Slices.SelectMany(_ => _.Commands).Single(_ => _.Name == "CreateEntity").Validations.Single(_ => _.Kind == SemanticValidationRuleKind.Matches).Operand.ShouldEqual(SemanticValue.Text(SemanticMatchPattern.Email));
    [Fact] void should_preserve_canonical_bytes() => SemanticModelSerializer.Serialize(_roundTrip).SequenceEqual(_serialized).ShouldBeTrue();
}
