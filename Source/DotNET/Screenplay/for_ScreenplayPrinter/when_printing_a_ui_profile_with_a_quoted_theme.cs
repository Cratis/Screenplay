// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_ui_profile_with_a_quoted_theme : given.a_printer
{
    const string Source =
        """
        ui profile Desktop
          target platform web

          theme "Scene Default Light"

        ui profile Phone
          target platform web

          theme "Say \"Hi\""

        ui profile Plain
          target platform web

          theme Corporate
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_reparse_with_only_the_unknown_identifier_theme_warning() => _roundtrip.Reparsed.Diagnostics.Select(diagnostic => diagnostic.Code).Single().ShouldEqual(Diagnostics.DiagnosticCodes.UnknownTheme);
    [Fact] void should_print_byte_identical_canonical_source() => _roundtrip.Printed.TrimEnd().ShouldEqual(Source.TrimEnd());
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_the_exact_name() => Profile("Desktop").Theme.ShouldEqual("Scene Default Light");
    [Fact] void should_unescape_the_name() => Profile("Phone").Theme.ShouldEqual("Say \"Hi\"");
    [Fact] void should_keep_an_identifier_name_bare() => Profile("Plain").Theme.ShouldEqual("Corporate");

    UiProfileSyntax Profile(string name) => _roundtrip.Reparsed.Value!.UiProfiles!.First(profile => profile.Name == name);
}
