// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_a_dialog_template : given.a_printer
{
    const string Source =
        """
        module Dashboards
          dialog template DashboardCanvas
            arrangement freeform
              variant width regular, height regular
                place header  at 0,0    size fill,64
                place sidebar at 0,64   size 240,fill
                place main    at 240,64 size fill,fill

              variant width compact, height regular
                place header at 0,0  size fill,48
                place main   at 0,48 size fill,fill
                place sidebar hidden
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_the_arrangement() => _roundtrip.Printed.ShouldContain("arrangement freeform");
    [Fact] void should_print_the_regular_variant_header() => _roundtrip.Printed.ShouldContain("variant width regular, height regular");
    [Fact] void should_print_a_placed_slot() => _roundtrip.Printed.ShouldContain("place sidebar at 0,64 size 240,fill");
    [Fact] void should_print_a_hidden_slot() => _roundtrip.Printed.ShouldContain("place sidebar hidden");
    [Fact] void should_print_the_slots_the_variants_place() => _roundtrip.Printed.ShouldContain("dialog template DashboardCanvas\n    header\n    sidebar\n    main");
    [Fact] void should_preserve_the_declared_slots() => Template.Slots.Select(slot => slot.Name).ShouldContainOnly("header", "sidebar", "main");

    DialogTemplateSyntax Template => _roundtrip.Reparsed.Value!.Modules.Single().DialogTemplates!.Single();
}
