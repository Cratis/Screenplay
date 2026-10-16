// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_ui_profile_with_the_legacy_layout_and_a_root_template : given.a_printer
{
    const string Source =
        """
        theme Aurora
          compatible with Cratis.Components

        layout AppShell
          category Shell
          content

        ui profile Desktop

          layout AppShell
          theme Aurora

          root template AppShell
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_print_byte_identical_canonical_source() => _roundtrip.Printed.TrimEnd().ShouldEqual(Source.TrimEnd());
    [Fact] void should_keep_the_legacy_layout() => _roundtrip.Reparsed.Value!.UiProfiles!.Single().Layout.ShouldEqual("AppShell");
    [Fact] void should_keep_the_root_template() => _roundtrip.Reparsed.Value!.UiProfiles!.Single().Root!.Name.ShouldEqual("AppShell");
}
