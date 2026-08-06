// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Screenplay.Diagnostics.for_DiagnosticCodes;

public class when_reading_the_catalogue : Specification
{
    const string Prefix = "PLAY";
    const int Length = 8;

    List<string> _codes;

    void Establish() => _codes =
    [
        .. typeof(DiagnosticCodes)
            .GetFields(BindingFlags.Public | BindingFlags.Static)
            .Where(field => field.IsLiteral)
            .Select(field => (string)field.GetRawConstantValue()!)
    ];

    [Fact] void should_declare_codes() => _codes.ShouldNotBeEmpty();
    [Fact] void should_prefix_every_code_with_the_language_prefix() => _codes.TrueForAll(code => code.StartsWith(Prefix, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_number_every_code_with_four_digits() => _codes.TrueForAll(code => code.Length == Length && code[Prefix.Length..].All(char.IsAsciiDigit)).ShouldBeTrue();
    [Fact] void should_never_hand_one_number_to_two_codes() => _codes.Distinct(StringComparer.Ordinal).Count().ShouldEqual(_codes.Count);
    [Fact] void should_not_share_the_prefix_the_generator_reports_under() => _codes.Exists(code => code.StartsWith("SP", StringComparison.Ordinal)).ShouldBeFalse();
}
