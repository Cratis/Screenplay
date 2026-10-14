// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Numerics;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.Generation.for_GenerationPattern;

public class when_comparing_exact_entropy_thresholds
{
    [Fact]
    void should_never_round_a_below_threshold_support_up_to_admission()
    {
        var threshold = BigInteger.One << 64;
        GenerationEntropy.HasAtLeastBits(threshold - 1, 64).ShouldBeFalse();
        GenerationEntropy.HasAtLeastBits(threshold, 64).ShouldBeTrue();
        GenerationEntropy.HasAtLeastBits(threshold + 1, 64).ShouldBeTrue();
        GenerationEntropy.SupportBits(threshold).ShouldEqual(64d);
    }

    [Fact]
    void should_compare_the_full_signed_int64_range_exactly() => GenerationEntropy.HasAtLeastBits(GenerationIntegerRange.Parse("-9223372036854775808..9223372036854775807").Count, 64).ShouldBeTrue();

    [Fact]
    void should_expose_exact_support_without_admitting_an_opaque_strategy()
    {
        GenerationEntropy.ExactSupportCount(new("random", SourceLocation.Start) { Range = "-9223372036854775808..9223372036854775807" }, 2026).ShouldEqual(BigInteger.One << 64);
        GenerationEntropy.ExactSupportCount(new("pattern", SourceLocation.Start) { Pattern = "{MM}[AB]" }, 2020).ShouldEqual(new BigInteger(2));
        GenerationEntropy.ExactSupportCount(new("rule", SourceLocation.Start) { Rule = "TenantOrderId" }, 2026).ShouldBeNull();
    }

    [Fact]
    void should_refuse_zero_support_instead_of_reporting_entropy() => Catch.Exception(() => GenerationEntropy.SupportBits(BigInteger.Zero)).ShouldBeOfExactType<InvalidSemanticContract>();
}
