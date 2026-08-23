// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using System.Text;
using Cratis.Screenplay.Semantics.Serialization.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.Serialization.for_CanonicalSemanticModelSerializer;

public class when_serializing_semantic_decimals : Specification
{
    static readonly decimal NegativeZero = new(0, 0, 0, true, 0);
    readonly decimal_case[] _canonicalCases =
    [
        new(decimal.MinValue, "-79228162514264337593543950335"),
        new(decimal.MaxValue, "79228162514264337593543950335"),
        new(0.0000000000000000000000000001m, "0.0000000000000000000000000001"),
        new(-0.0000000000000000000000000001m, "-0.0000000000000000000000000001"),
        new(123456789.012345678900m, "123456789.0123456789"),
        new(-123456789.012345678900m, "-123456789.0123456789")
    ];
    readonly string[] _nonCanonicalNumbers = ["1.0", "1.00", "1e0", "1E+0", "01", "-0"];
    byte[][] _equivalentBytes;
    SemanticRevision[] _equivalentRevisions;
    byte[] _zeroBytes;
    byte[] _negativeZeroBytes;
    string[] _canonicalJson;
    bool[] _roundTripsWereIdentical;
    Exception[] _nonCanonicalErrors;

    void Because()
    {
        var equivalents = new[]
        {
            canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(1m),
            canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(1.0m),
            canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(1.00m)
        };
        _equivalentBytes = [.. equivalents.Select(SemanticModelSerializer.Serialize)];
        _equivalentRevisions = [.. equivalents.Select(_ => _.Revision)];
        _zeroBytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(0m));
        _negativeZeroBytes = SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(NegativeZero));
        _canonicalJson =
        [
            .. _canonicalCases.Select(_ => Encoding.UTF8.GetString(
                SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(_.Value))))
        ];
        _roundTripsWereIdentical =
        [
            .. _canonicalCases
                .Select(_ => SemanticModelSerializer.Serialize(canonical_serialization_golden_vectors.CreateSemanticModelWithDecimal(_.Value)))
                .Select(bytes => SemanticModelSerializer.Serialize(SemanticModelSerializer.Deserialize(bytes)).SequenceEqual(bytes))
        ];

        var canonicalOne = Encoding.UTF8.GetString(_equivalentBytes[0]);
        _nonCanonicalErrors =
        [
            .. _nonCanonicalNumbers
                .Select(number => canonicalOne.Replace(
                    "\"operand\":{\"kind\":\"number\",\"value\":1},\"message\":\"Exact amount\"",
                    $"\"operand\":{{\"kind\":\"number\",\"value\":{number}}},\"message\":\"Exact amount\"",
                    StringComparison.Ordinal))
                .Select(json => Catch.Exception(() => SemanticModelSerializer.Deserialize(Encoding.UTF8.GetBytes(json))))
        ];
    }

    [Fact] void should_make_equivalent_scales_byte_identical() => _equivalentBytes.Skip(1).All(_ => _.SequenceEqual(_equivalentBytes[0])).ShouldBeTrue();
    [Fact] void should_make_equivalent_scales_revision_identical() => _equivalentRevisions.Skip(1).All(_ => _ == _equivalentRevisions[0]).ShouldBeTrue();
    [Fact] void should_canonicalize_negative_zero_as_zero() => _negativeZeroBytes.SequenceEqual(_zeroBytes).ShouldBeTrue();
    [Fact] void should_write_each_extreme_and_fraction_without_an_exponent_or_insignificant_zero() => _canonicalCases.Zip(_canonicalJson).All(_ => _.Second.Contains($"\"operand\":{{\"kind\":\"number\",\"value\":{_.First.Expected}}},\"message\":\"Exact amount\"", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_round_trip_each_extreme_and_fraction_byte_identically() => _roundTripsWereIdentical.All(_ => _).ShouldBeTrue();
    [Fact] void should_reject_every_valid_or_invalid_noncanonical_number_spelling() => _nonCanonicalErrors.All(_ => _ is InvalidSemanticContract).ShouldBeTrue();

    readonly record struct decimal_case(decimal Value, string Expected);
}
#endif
