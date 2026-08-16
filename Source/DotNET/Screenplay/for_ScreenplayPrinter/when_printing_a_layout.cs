// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_layout : given.a_printer
{
    const string Source =
        """
        layout AppShell
          topbar
          navigation contributes Navigation
          content
          footer

          arrangement flow
            column
              topbar height 56
              row
                navigation width 240
                content grow
              footer height 32

        ui profile Desktop
          target platform web
          layout AppShell
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_layout_header() => _roundtrip.Printed.ShouldContain("layout AppShell");
    [Fact] void should_print_the_slots_it_declares() => _roundtrip.Printed.ShouldContain("layout AppShell\n  topbar\n  navigation contributes Navigation\n  content\n  footer");
    [Fact] void should_print_the_arrangement() => _roundtrip.Printed.ShouldContain("arrangement flow");
    [Fact] void should_print_the_nested_tree() => _roundtrip.Printed.ShouldContain("navigation width 240");
    [Fact] void should_print_the_layout_the_profile_selects() => _roundtrip.Printed.ShouldContain("ui profile Desktop\n  target platform web\n\n  layout AppShell");
    [Fact] void should_preserve_the_layout_slots() => Layout.Slots.Select(slot => slot.Name).ShouldContainOnly("topbar", "navigation", "content", "footer");
    [Fact] void should_preserve_the_contribution_point() => Layout.Slots.Single(slot => slot.Name == "navigation").Contributes.ShouldEqual("Navigation");
    [Fact] void should_preserve_the_profile_layout_selection() => _roundtrip.Reparsed.Value!.UiProfiles!.Single().Layout.ShouldEqual("AppShell");

    LayoutSyntax Layout => _roundtrip.Reparsed.Value!.Layouts!.Single();
}
