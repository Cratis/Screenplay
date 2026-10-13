// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_reading_policy_negation : given.golden_files
{
    EsmGoldenVector _vector;
    ExecutableSemanticModel _model;

    void Because()
    {
        _vector = EsmGoldenVectors.PolicyNegationV7;
        _model = SemanticModelSerializer.Deserialize(_vector.Bytes.AsSpan());
    }

    [Fact] void should_preserve_the_extension_file_bytes() => _vector.Bytes.SequenceEqual(File.ReadAllBytes(Path.Combine(_goldenFolder, "policy-negation-v7.json"))).ShouldBeTrue();
    [Fact] void should_select_v7() => (_vector.Version == 7 && _model.SemanticVersion == SemanticVersion.V7).ShouldBeTrue();
    [Fact] void should_pin_the_extension_revision() => _vector.SemanticRevision.ShouldEqual(_model.Revision);
    [Fact] void should_round_trip_through_the_strict_reader() => SemanticModelSerializer.Serialize(_model).SequenceEqual(_vector.Bytes).ShouldBeTrue();
}
