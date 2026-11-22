// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.for_ScreenplayPrinter.when_printing_ui_profile_shell_sections;

public class and_all_sections_are_present : given.a_printer
{
    const string Source =
        """
        ui profile Shop
          target platform web

          packages
            crm

          root template blueprint crm AppShell version "^4.5"
            option title "Contoso \"CRM\""
            option density 2
            option showFooter true
            option navigationMode slim

          identity
            name "Shop"
            logo icon package
            show name false

          appearance
            color scheme dark
            accent "#4f46e5"
            corners round
            font source-serif-4

          new screens use template blueprint crm Workspace version "~1.2"
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_with_only_appearance_deprecations() => _roundtrip.Original!.Diagnostics.Where(diagnostic => diagnostic.Code != DiagnosticCodes.DeprecatedAppearanceMember).ShouldBeEmpty();
    [Fact] void should_preserve_section_order_options_and_separators() => _roundtrip.Printed.ShouldEqual(Source + "\n");
    [Fact] void should_reparse_with_only_appearance_deprecations() => _roundtrip.Reparsed.Diagnostics.Where(diagnostic => diagnostic.Code != DiagnosticCodes.DeprecatedAppearanceMember).ShouldBeEmpty();
    [Fact] void should_print_identically_twice() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
