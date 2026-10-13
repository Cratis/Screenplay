// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_generation : given.a_printer
{
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip("concept Reference : String\n  // Recognizable prefix, with no extra random bits.\n  generate nanoid 64 prefix \"ref_\" alphabet \"ab\" // Pinned alphabet\nconcept TimeId : Uuid\n  generate uuidv7\nconcept Preview : String\n  generate pattern \"ORD-#{7}\" checksum luhn");

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_modifiers_in_canonical_order() => _roundtrip.Printed.ShouldContain("generate nanoid 64 alphabet \"ab\" prefix \"ref_\"");
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_the_preview_strategy() => _roundtrip.Printed.ShouldContain("generate pattern \"ORD-#{7}\" checksum luhn");
}
