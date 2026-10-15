// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticGeneratedFixture;

public class when_checking_phase_three_shapes : Specification
{
    [Theory]
    [InlineData("2024-03-01T00:30:00+02:00", "20240229-24-0229-A", true)]
    [InlineData("2024-03-01T00:30:00+02:00", "20240301-24-0301-A", false)]
    [InlineData("2024-02-29T23:30:00-02:00", "20240301-24-0301-A", true)]
    [InlineData("2024-02-29T23:30:00-02:00", "20240301-24-0229-A", false)]
    [InlineData(null, "99999999-00-9999-A", true)]
    [InlineData(null, "20240229-24-0229-a", false)]
    [InlineData(null, "2024022-24-0229-A", false)]
    void should_use_one_utc_instant_or_only_digit_widths_without_a_clock(string? clock, string value, bool expected)
    {
        var concept = Concept(new("pattern") { Pattern = "{yyyy}{MM}{dd}-{yy}-{MM}{dd}-?" });
        SemanticGeneratedFixture.Matches(concept, SemanticValue.Text(value), clock is null ? null : DateTimeOffset.Parse(clock, System.Globalization.CultureInfo.InvariantCulture)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("luhn", "0000", true)]
    [InlineData("luhn", "0009", true)]
    [InlineData("luhn", "000X", false)]
    [InlineData("mod11", "0060", true)]
    [InlineData("mod97", "00000", true)]
    [InlineData("mod97", "0000", false)]
    void should_check_checksum_suffix_shape_not_arithmetic(string checksum, string value, bool expected)
    {
        var concept = Concept(new("pattern") { Pattern = "#{3}", Checksum = checksum });
        SemanticGeneratedFixture.Matches(concept, SemanticValue.Text(value)).ShouldEqual(expected);
    }

    [Theory]
    [InlineData("-9223372036854775808", true)]
    [InlineData("9223372036854775807", true)]
    [InlineData("9223372036854775808", false)]
    [InlineData("-9223372036854775809", false)]
    [InlineData("0.5", false)]
    void should_compare_integral_numeric_fixtures_exactly(string value, bool expected)
    {
        var concept = Concept(new("random") { Minimum = long.MinValue, Maximum = long.MaxValue }) with { Primitive = SemanticPrimitiveType.WholeNumber };
        SemanticGeneratedFixture.Matches(concept, SemanticValue.Number(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture))).ShouldEqual(expected);
    }

    [Fact]
    void should_not_coerce_numeric_text() => SemanticGeneratedFixture.Matches(Concept(new("random") { Minimum = 1, Maximum = 9 }), SemanticValue.Text("4")).ShouldBeFalse();

    [Fact]
    void should_accept_unknown_implementation_shape_without_running_a_body() => SemanticGeneratedFixture.Matches(Concept(new("implementation")), SemanticValue.Text("any primitive string")).ShouldBeTrue();

    [Fact]
    void should_not_coerce_an_implementation_fixture() => SemanticGeneratedFixture.Matches(Concept(new("implementation")), SemanticValue.Number(4)).ShouldBeFalse();

    static SemanticConcept Concept(SemanticGeneration generation) => new(default, "Value", SemanticPrimitiveType.Text, [], []) { Generation = generation };
}
