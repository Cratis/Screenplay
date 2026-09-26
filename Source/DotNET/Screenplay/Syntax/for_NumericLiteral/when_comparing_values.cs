// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Syntax.for_NumericLiteral;

public class when_comparing_values
{
    [Fact] public void should_compare_whole_values_across_clr_kinds() => NumericLiteral.Equal(2L, 2.0m).ShouldBeTrue();
    [Fact] public void should_compare_exact_binary_fractions_with_decimal_values() => NumericLiteral.Equal(2.5d, 2.5m).ShouldBeTrue();
    [Fact] public void should_not_round_an_imprecise_double_into_a_decimal() => NumericLiteral.Equal(0.1d, 0.1m).ShouldBeFalse();
    [Fact] public void should_not_round_large_integer_values_through_a_double() => NumericLiteral.Equal(9007199254740993L, 9007199254740992d).ShouldBeFalse();
    [Fact] public void should_compare_values_outside_decimal_range_without_overflow() => NumericLiteral.Equal(1e100d, 1e100d).ShouldBeTrue();
}
