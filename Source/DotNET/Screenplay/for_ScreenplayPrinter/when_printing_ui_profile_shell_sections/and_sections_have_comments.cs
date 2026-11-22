// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_ui_profile_shell_sections;

public class and_sections_have_comments : given.a_printer
{
    const string Source =
        """
        ui profile Shop
          target platform web

          packages
            crm

          // root note
          root template blueprint crm AppShell version "^4.5"
            // option note
            option navigationMode slim // option trailing

          // identity note
          identity
            name "Shop"

          // appearance note
          appearance
            color scheme dark

          // new screen note
          new screens use template blueprint crm Workspace version "~1.2"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_keep_comment_anchors_and_separators() => _roundtrip.Printed.ShouldEqual(Source + "\n");
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
