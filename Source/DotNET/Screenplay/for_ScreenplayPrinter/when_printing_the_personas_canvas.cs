// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_the_personas_canvas : given.a_printer
{
    const string Source =
        """
        policy CanShop
          require role "Shopper"

        persona Shopper
          description "Browses the catalog and buys"
          goal "Find the right product quickly"
          goal "Check out in \"under\" a minute"
          policy CanShop
          position 40 -20
          size 320 180

        persona Buyer
          description "Orders stock"

        sticky note on personas
          description "Pain point: the checkout asks for too much"
          colour pink
          position 400 40
          size 220 140

        sticky note on personas
          description
            ```text
            Context: works mostly on a phone,
            often in a hurry
            ```
          position 400 220

        module Shop

          feature Browse
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    PersonaSyntax Shopper => _roundtrip.Reparsed.Value!.Personas!.First();
    PersonaSyntax Buyer => _roundtrip.Reparsed.Value!.Personas!.Last();
    IEnumerable<StickyNoteSyntax> Notes => _roundtrip.Reparsed.Value!.StickyNotes;

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_exactly_what_was_written() => _roundtrip.Printed.TrimEnd().ShouldEqual(Source.Replace("\r\n", "\n").TrimEnd());
    [Fact] void should_keep_the_goals_in_order() => Shopper.Goals.ShouldContainOnly("Find the right product quickly", "Check out in \"under\" a minute");
    [Fact] void should_keep_the_position() => (Shopper.X, Shopper.Y).ShouldEqual((40, -20));
    [Fact] void should_keep_the_size() => (Shopper.Width, Shopper.Height).ShouldEqual((320, 180));
    [Fact] void should_leave_an_unplaced_persona_without_a_position() => Buyer.X.ShouldBeNull();
    [Fact] void should_leave_an_unsized_persona_without_a_size() => Buyer.Width.ShouldBeNull();
    [Fact] void should_keep_both_notes() => Notes.Count().ShouldEqual(2);
    [Fact] void should_keep_the_text_of_a_note() => Notes.First().Text.ShouldEqual("Pain point: the checkout asks for too much");
    [Fact] void should_keep_the_colour_of_a_note() => Notes.First().Colour.ShouldEqual("pink");
    [Fact] void should_keep_the_size_of_a_note() => (Notes.First().Width, Notes.First().Height).ShouldEqual((220, 140));
    [Fact] void should_keep_a_multiline_note() => Notes.Last().Text.ShouldEqual("Context: works mostly on a phone,\noften in a hurry");
    [Fact] void should_leave_a_note_without_a_colour_uncoloured() => Notes.Last().Colour.ShouldBeNull();
}
