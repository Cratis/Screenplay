// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_SemanticModelSerializer;

public class when_round_tripping_the_reaction_identity_golden_bytes : Specification
{
    byte[] _bytes;
    ExecutableSemanticModel _model;

    void Establish()
    {
        using var stream = typeof(when_round_tripping_the_reaction_identity_golden_bytes).Assembly.GetManifestResourceStream("Cratis.Screenplay.CanonicalVectors.Golden.full-esm-v10.json")!;
        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        _bytes = memory.ToArray();
    }

    void Because() => _model = SemanticModelSerializer.Deserialize(_bytes);

    [Fact] void should_admit_v10() => _model.SemanticVersion.ShouldEqual(SemanticVersion.V10);
    [Fact] void should_preserve_exact_canonical_bytes() => SemanticModelSerializer.Serialize(_model).SequenceEqual(_bytes).ShouldBeTrue();
}
