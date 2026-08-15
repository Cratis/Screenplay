// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_ui_profile : given.a_printer
{
    const string Source =
        """
        ui profile Desktop
          target platform web
          target size expanded

          packages
            core
            PrimeReact
            Internal.Widgets
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_profile_header() => _roundtrip.Printed.ShouldContain("ui profile Desktop");
    [Fact] void should_print_the_target_platform() => _roundtrip.Printed.ShouldContain("target platform web");
    [Fact] void should_print_the_target_size() => _roundtrip.Printed.ShouldContain("target size expanded");
    [Fact] void should_print_the_packages_in_order() => _roundtrip.Printed.ShouldContain("packages\n    core\n    PrimeReact\n    Internal.Widgets");
    [Fact] void should_preserve_the_platform() => Profile.Platforms.ShouldContainOnly("web");
    [Fact] void should_preserve_the_size_class() => Profile.DefaultSizeClass.ShouldEqual("expanded");
    [Fact] void should_preserve_the_packages() => Profile.Packages.ShouldContainOnly("core", "PrimeReact", "Internal.Widgets");

    UiProfileSyntax Profile => _roundtrip.Reparsed.Value!.UiProfiles!.Single();
}
