// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_enum_member_numbers : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip("concept Status : Enum\n  draft = -0 // First member\n  issued = 1\n  @validate = -7");

    [Fact] void should_print_negative_zero_canonically() => _roundtrip.Printed.ShouldContain("draft = 0");
    [Fact] void should_keep_explicit_positional_numbers() => _roundtrip.Printed.ShouldContain("issued = 1");
    [Fact] void should_keep_escaped_names_and_negative_numbers() => _roundtrip.Printed.ShouldContain("@validate = -7");
    [Fact] void should_preserve_the_comment() => _roundtrip.Printed.ShouldContain("// First member");
    [Fact] void should_preserve_numbers_on_reparse() => _roundtrip.Reparsed.Value!.Concepts.Single().ValueNumbers!.ShouldContainOnly(0, 1, -7);
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
