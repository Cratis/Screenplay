// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_the_domain_canvases : given.a_printer
{
    const string Source =
        """
        eventsource Product
          description "One product in the catalogue"
          identifier ProductId
          stream Lifecycle
            description "Drafting, pricing and approval of one product"
          position 110 -150

        eventsource Customer
          identifier CustomerId

        concept ProductId : Uuid

        concept CustomerId : Uuid

        sticky note on personas
          description "Persona question"
          position 0 0

        sticky note on eventsources
          description "Open question: one stream per market?"
          colour pink
          position 480 150
          size 240 140

        sticky note on givencontexts
          description
            ```text
            A paid order is the common start,
            reuse it in every specification
            ```
          colour green
          position -40 80

        sticky note on seeddata
          description "Seed the catalogue before the first sale"
          colour blue
          position 20 400
          size 300 160

        module Shop

          feature Browse
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    ApplicationSyntax Application => _roundtrip.Reparsed.Value!;
    EventSourceSyntax Product => Application.EventSources.First();
    EventSourceSyntax Customer => Application.EventSources.Last();
    IEnumerable<StickyNoteSyntax> Notes => Application.StickyNotes;

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_exactly_what_was_written() => _roundtrip.Printed.TrimEnd().ShouldEqual(Source.Replace("\r\n", "\n").TrimEnd());
    [Fact] void should_keep_the_position_of_an_event_source() => (Product.X, Product.Y).ShouldEqual((110, -150));
    [Fact] void should_leave_an_unplaced_event_source_without_a_position() => Customer.X.ShouldBeNull();
    [Fact] void should_keep_every_note() => Notes.Count().ShouldEqual(4);
    [Fact] void should_keep_the_canvas_of_each_note() => Notes.Select(note => note.Canvas).ShouldContainOnly("personas", "eventsources", "givencontexts", "seeddata");
    [Fact] void should_keep_the_colour_and_size_of_a_note() => (Notes.ElementAt(1).Colour, Notes.ElementAt(1).Width, Notes.ElementAt(1).Height).ShouldEqual(("pink", 240, 140));
    [Fact] void should_keep_a_multiline_note() => Notes.ElementAt(2).Text.ShouldEqual("A paid order is the common start,\nreuse it every specification".Replace("reuse it every", "reuse it in every"));
    [Fact] void should_keep_a_negative_note_position() => (Notes.ElementAt(2).X, Notes.ElementAt(2).Y).ShouldEqual((-40, 80));
}
