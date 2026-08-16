// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_screen_template : given.a_printer
{
    const string Source =
        """
        module Invoicing
          screen template MasterDetail
            fits slot content

            sidebar contributes Navigation
            main

            arrangement flow
              row gap 16
                sidebar width 280
                main grow

              when width compact
                column
                  main
                  sidebar
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_arrangement() => _roundtrip.Printed.ShouldContain("arrangement flow");
    [Fact] void should_print_the_slot_it_fits_into() => _roundtrip.Printed.ShouldContain("fits slot content");
    [Fact] void should_print_the_declared_slot_with_its_contribution_point() => _roundtrip.Printed.ShouldContain("sidebar contributes Navigation");
    [Fact] void should_preserve_the_slot_it_fits_into() => Template.FitsSlot.ShouldEqual("content");
    [Fact] void should_preserve_the_contribution_point() => Template.Slots.First().Contributes.ShouldEqual("Navigation");
    [Fact] void should_print_the_row_gap() => _roundtrip.Printed.ShouldContain("row gap 16");
    [Fact] void should_print_the_sidebar_width() => _roundtrip.Printed.ShouldContain("sidebar width 280");
    [Fact] void should_print_the_main_grow() => _roundtrip.Printed.ShouldContain("main grow");
    [Fact] void should_print_the_override_condition() => _roundtrip.Printed.ShouldContain("when width compact");
    [Fact] void should_print_the_override_column() => _roundtrip.Printed.ShouldContain("column");

    ScreenTemplateSyntax Template => _roundtrip.Reparsed.Value!.Modules.Single().ScreenTemplates.Single();
}
