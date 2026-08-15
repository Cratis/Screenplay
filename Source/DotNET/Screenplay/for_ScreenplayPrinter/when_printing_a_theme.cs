// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_theme : given.a_printer
{
    const string Source =
        """
        theme Aurora
          compatible with core
          compatible with PrimeReact
          compatible with Internal.Widgets

        ui profile Desktop
          packages
            core
            PrimeReact
            Internal.Widgets

          theme Aurora
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_theme_header() => _roundtrip.Printed.ShouldContain("theme Aurora");
    [Fact] void should_print_the_compatible_packages_in_order() => _roundtrip.Printed.ShouldContain("compatible with core\n  compatible with PrimeReact\n  compatible with Internal.Widgets");
    [Fact] void should_print_the_profile_theme_reference() => _roundtrip.Printed.ShouldContain("theme Aurora");
    [Fact] void should_preserve_the_theme_reference() => _roundtrip.Reparsed.Value!.UiProfiles!.Single().Theme.ShouldEqual("Aurora");
}
