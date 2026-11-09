// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;

namespace Cratis.Screenplay.Semantics.for_LegacyNumberLowering;

public class when_preserving_runtime_independent_conversions : Specification
{
    [Theory]
    [InlineData("1234567890123456", "1234567890123460")]
    [InlineData("9999999999999996", "10000000000000000")]
    [InlineData("-1234567890123456", "-1234567890123460")]
    [InlineData("1.23456789012E-17", "0.0000000000000000123456789012")]
    [InlineData("0.1", "0.1")]
    [InlineData("9007199254740991", "9007199254740990")]
    [InlineData("1E-28", "0.0000000000000000000000000001")]
    [InlineData("1E-100", "0")]
    [InlineData("1E28", "10000000000000000000000000000")]
    [InlineData("0.9999999999999996", "1.0")]
    void should_preserve_the_net10_value_and_scale(string input, string expected)
    {
        var result = LegacyNumberLowering.FromDouble(double.Parse(input, CultureInfo.InvariantCulture));
        decimal.GetBits(result).ShouldEqual(decimal.GetBits(decimal.Parse(expected, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData(double.NaN)]
    [InlineData(double.PositiveInfinity)]
    [InlineData(double.NegativeInfinity)]
    [InlineData(7.922816251426434E28)]
    [InlineData(-7.922816251426434E28)]
    void should_preserve_overflow(double input) =>
        Catch.Exception(() => LegacyNumberLowering.FromDouble(input)).ShouldBeOfExactType<OverflowException>();

    [Theory]
    [InlineData("1234567890123456", "1234567890123456")]
    [InlineData("9999999999999995", "9999999999999996")]
    [InlineData("-1234567890123456", "-1234567890123456")]
    [InlineData("0.0000000000000000123456789012", "1.23456789012E-17")]
    [InlineData("79228162514264337593543950335", "7.922816251426434E28")]
    [InlineData("0.1", "0.1")]
    [InlineData("9007199254740991", "9007199254740991")]
    [InlineData("0.0000000000000000000000000001", "1.0000000000000001E-28")]
    void should_preserve_the_net10_decimal_to_double_round_trip(string input, string expected)
    {
        var result = LegacyNumberLowering.ToDouble(decimal.Parse(input, CultureInfo.InvariantCulture));
        BitConverter.DoubleToInt64Bits(result).ShouldEqual(BitConverter.DoubleToInt64Bits(double.Parse(expected, CultureInfo.InvariantCulture)));
    }

    [Theory]
    [InlineData("1234567890123456", true)]
    [InlineData("9999999999999995", true)]
    [InlineData("-1234567890123456", true)]
    [InlineData("9007199254740991", true)]
    [InlineData("79228162514264337593543950335", true)]
    [InlineData("0.1", false)]
    [InlineData("0.0000000000000000123456789012", false)]
    [InlineData("0.0000000000000000000000000001", false)]
    void should_preserve_exact_number_version_selection(string input, bool expected) =>
        SemanticModelValidator.DiffersFromLegacyLowering(decimal.Parse(input, CultureInfo.InvariantCulture)).ShouldEqual(expected);
}
