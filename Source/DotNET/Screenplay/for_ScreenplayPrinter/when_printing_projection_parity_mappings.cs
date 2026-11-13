// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_projection_parity_mappings : given.a_printer
{
    const string Source =
        """
        module Models
          feature Slots
            slice StateView Models
              projection Models => ModelReadModel
                from SlotAssigned
                  slots.$eventContent.slot.name = seriesId
                  set once createdBy to $causedBy.subject
                  set once createdAt = $eventContext.occurred
                  increment assignments.$causedBy.userName
                from SlotDropped
                  remove slots.$eventContent.slot.name
                  remove @with.$eventContent.slot.name
                children entries identified by entryId
                  no automap
                  sort by addedAt ascending
                  from EntryAdded
                    name = name
                children history identified by entryId
                  insert first
                  from EntryAdded
                    name = name
                children ranking identified by entryId
                  sort by score descending
                  from EntryAdded
                    name = name
                children log identified by entryId
                  insert last
                  from EntryAdded
                    name = name
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_print_an_event_content_key() => _roundtrip.Printed.ShouldContain("slots.$eventContent.slot.name = seriesId");
    [Fact] void should_print_set_once_in_canonical_form() => _roundtrip.Printed.ShouldContain("set once createdBy = $causedBy.subject");
    [Fact] void should_print_set_once_from_the_event_context() => _roundtrip.Printed.ShouldContain("set once createdAt = $eventContext.occurred");
    [Fact] void should_print_a_caused_by_key() => _roundtrip.Printed.ShouldContain("increment assignments.$causedBy.userName");
    [Fact] void should_print_the_remove_line() => _roundtrip.Printed.ShouldContain("remove slots.$eventContent.slot.name");
    [Fact] void should_print_a_dotted_with_target_without_its_escape() => _roundtrip.Printed.ShouldContain("remove with.$eventContent.slot.name");
    [Fact] void should_print_an_ascending_sort_without_its_default() => _roundtrip.Printed.ShouldContain("sort by addedAt\n");
    [Fact] void should_print_a_descending_sort() => _roundtrip.Printed.ShouldContain("sort by score descending");
    [Fact] void should_print_insert_first() => _roundtrip.Printed.ShouldContain("insert first");
    [Fact] void should_print_insert_last() => _roundtrip.Printed.ShouldContain("insert last");
}
