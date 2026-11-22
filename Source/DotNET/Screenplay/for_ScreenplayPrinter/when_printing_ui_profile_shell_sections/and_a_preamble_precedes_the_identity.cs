// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_ui_profile_shell_sections;

public class and_a_preamble_precedes_the_identity : given.a_printer
{
    const string Source = "ui profile Shop\n  target platform web\n\n  identity\n    name \"Shop\"";
    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_insert_one_separator_after_the_preamble() => _roundtrip.Printed.ShouldEqual(Source + "\n");
    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
