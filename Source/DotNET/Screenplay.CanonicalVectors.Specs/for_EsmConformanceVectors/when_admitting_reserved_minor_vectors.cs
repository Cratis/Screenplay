// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Security.Cryptography;
using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EsmConformanceVectors;

public class when_admitting_reserved_minor_vectors : Specification
{
    EsmConformanceVector[] _vectors;
    UnknownPreservingSemanticDocument _document;
    Exception?[] _refusals;
    Exception?[] _strictRefusals;

    void Because()
    {
        _vectors = [.. EsmConformanceVectors.Names.Select(EsmConformanceVectors.Get)];
        var range = new EsmAdmissionRange(34, 0, EsmConformanceVectors.ReservedMinor);
        _document = UnknownPreservingSemanticModelReader.Read(_vectors.Single(vector => vector.ShouldAdmit).Bytes.AsSpan(), range);
        _refusals = [.. _vectors.Where(vector => !vector.ShouldAdmit).Select(vector => Catch.Exception(() => UnknownPreservingSemanticModelReader.Read(vector.Bytes.AsSpan(), range)))];
        _strictRefusals = [.. _vectors.Select(vector => Catch.Exception(() => SemanticModelSerializer.Deserialize(vector.Bytes.AsSpan())))];
    }

    [Fact] void should_reemit_the_admitted_vector_byte_identically_after_opaque_canonical_reconstruction() => _document.CopyBytes().AsSpan().SequenceEqual(_vectors.Single(vector => vector.ShouldAdmit).Bytes.AsSpan()).ShouldBeTrue();
    [Fact] void should_refuse_misordered_opaque_members_and_unknown_discriminators() => _refusals.All(error => error is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_never_admit_reserved_pairs_through_the_strict_reader() => _strictRefusals.All(error => error is InvalidSemanticContract).ShouldBeTrue();
    [Fact] void should_keep_reserved_pairs_out_of_the_registered_version_sets() => (EsmSchemaV34Support.LanguageVersions.All(version => version.Minor != EsmConformanceVectors.ReservedMinor) && EsmSchemaV34Support.SemanticVersions.All(version => version.Minor != EsmConformanceVectors.ReservedMinor)).ShouldBeTrue();
    [Fact] void should_preserve_the_full_wire_revision() => _document.Revision.ShouldEqual(SemanticRevision.Parse("rev1:589acd95a27ca88df3a491b5e4306192a1a902680d989fdfc1a4a33b4ecbc090"));
    [Fact] void should_distinguish_ordinary_additions_from_known_identity_mapping_positions() => (_document.Unknowns.Count(member => member.IsSecurityRelevant) == 1 && _document.Unknowns.Length == 3).ShouldBeTrue();
    [Fact] void should_not_add_reserved_pairs_to_the_real_golden_versions() => EsmGoldenVectors.Versions.ShouldEqual(Enumerable.Range(1, 34));
    [Fact] void should_freeze_all_conformance_fixture_bytes_on_first_publication() => _vectors.Select(vector => Convert.ToHexStringLower(SHA256.HashData(vector.Bytes.AsSpan()))).ShouldEqual(
    [
        "699d3a32bccdad0ad2ffaeee13c36ef2644eabdf10d41082bcc8f132c6b650e1",
        "c6255a6e1ea1c10c939f06fd5e8dc89abcef63c50114ef69bd74e05643e1b748",
        "4e3fe49afb6d5d869ca0d59e52151d9e597658cd12831958a28e7475a1628424"
    ]);
}
