// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization.given;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_the_v3_golden_model : Specification
{
    byte[] _expected = [];
    byte[] _written = [];
    ExecutableSemanticModel _read = null!;
    ExecutableSemanticModel _model = null!;

    void Establish()
    {
        _expected = canonical_serialization_golden_vectors.SemanticModelV3Bytes;
        _model = canonical_serialization_golden_vectors.CreateSemanticModelV3();
    }

    void Because()
    {
        _written = SemanticModelSerializer.Serialize(_model);
        _read = SemanticModelSerializer.Deserialize(_expected);
    }

    [Fact] void should_match_the_checked_in_v3_bytes() => _written.SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_preserve_the_v3_revision() => _read.Revision.ShouldEqual(_model.Revision);
    [Fact] void should_reserialize_identically() => SemanticModelSerializer.Serialize(_read).SequenceEqual(_expected).ShouldBeTrue();
    [Fact] void should_write_schema_version_three()
    {
        using var document = JsonDocument.Parse(_written);
        document.RootElement.GetProperty("schemaVersion").GetInt32().ShouldEqual(3);
    }
}
#endif
