// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmGoldenVectors;

public class when_reading_a_minor_vector : given.golden_files
{
    EsmMinorGoldenVector _vector;

    void Because() => _vector = EsmGoldenVectors.GetMinor(SemanticVersion.V37_1);

    [Fact]
    void should_discover_nonzero_minors_separately()
    {
        SemanticVersion[] expected = [SemanticVersion.V37_1];
        EsmGoldenVectors.MinorVersions.ShouldEqual(expected);
    }
    [Fact] void should_not_change_the_integer_api() => EsmGoldenVectors.Versions.ShouldEqual(Enumerable.Range(1, 38).Append(40).Append(41).Append(42).Append(43).Append(44).Append(45).Append(46).Append(47));
    [Fact] void should_leave_get_36_pointing_to_the_original_golden() => EsmGoldenVectors.Get(36).Bytes.ShouldEqual(File.ReadAllBytes(Path.Combine(_goldenFolder, "full-esm-v36.json")));
    [Fact] void should_leave_get_37_pointing_to_the_original_golden() => EsmGoldenVectors.Get(37).Bytes.ShouldEqual(File.ReadAllBytes(Path.Combine(_goldenFolder, "full-esm-v37.json")));
    [Fact] void should_read_exact_minor_bytes() => _vector.Bytes.ShouldEqual(File.ReadAllBytes(Path.Combine(_goldenFolder, "full-esm-v37.1.json")));
    [Fact] void should_report_the_exact_pair() => _vector.Version.ShouldEqual(SemanticVersion.V37_1);
    [Fact] void should_verify_the_revision() => SemanticModelSerializer.Deserialize(_vector.Bytes.AsSpan()).Revision.ShouldEqual(_vector.SemanticRevision);
    [Fact] void should_refuse_an_absent_minor() => Catch.Exception(() => EsmGoldenVectors.GetMinor(new(37, 2))).ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_refuse_zero_minors_in_the_minor_api() => Catch.Exception(() => EsmGoldenVectors.GetMinor(SemanticVersion.V37)).ShouldBeOfExactType<InvalidSemanticContract>();
}
