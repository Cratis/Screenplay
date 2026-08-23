// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_round_tripping_a_model : a_valid_semantic_model
{
    byte[] _json;
    byte[] _reserialized;
    ExecutableSemanticModel _roundTripped;

    void Because()
    {
        _json = SemanticModelSerializer.Serialize(_model);
        _roundTripped = SemanticModelSerializer.Deserialize(_json);
        _reserialized = SemanticModelSerializer.Serialize(_roundTripped);
    }

    [Fact] void should_preserve_the_revision() => _roundTripped.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_be_byte_identical() => _reserialized.SequenceEqual(_json).ShouldBeTrue();
    [Fact] void should_not_have_a_utf8_bom() => _json.Take(3).SequenceEqual(new byte[] { 0xef, 0xbb, 0xbf }).ShouldBeFalse();
    [Fact] void should_not_have_whitespace() => _json.Contains((byte)'\n').ShouldBeFalse();
    [Fact] void should_retain_behavior_order() =>
        _roundTripped.Application.Modules.Single().Features.Single().Slices.Single(_ => _.Commands.Length > 0)
            .Commands.Single().Produces.Single().Mappings[0].TargetProperty.ShouldEqual(_eventProjectIdPropertyId);
}
