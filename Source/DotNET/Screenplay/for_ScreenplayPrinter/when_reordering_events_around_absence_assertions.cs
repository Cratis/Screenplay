// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Specifications;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_reordering_events_around_absence_assertions : given.a_printer
{
    const string Source =
        """
        specification NoInvoice
          then FirstEvent
          then no readmodel InvoiceView for "first"
          then SecondEvent
          then readmodel InvoiceView
          then query FindInvoice
          then error "missing"
        """;

    string _printed = null!;
    string _printedAgain = null!;
    SpecificationSyntax _reparsed = null!;

    void Because()
    {
        var parsed = _compiler.CompileSpecification(Source).Value!;
        _printed = _printer.Print(parsed with { ThenEvents = [.. parsed.ThenEvents.Reverse()] });
        _reparsed = _compiler.CompileSpecification(_printed).Value!;
        _printedAgain = _printer.Print(_reparsed);
    }

    [Fact] void should_print_the_requested_event_order() => _printed.IndexOf("then SecondEvent", StringComparison.Ordinal).ShouldBeLessThan(_printed.IndexOf("then FirstEvent", StringComparison.Ordinal));
    [Fact] void should_preserve_the_order_within_each_kind() => _reparsed.ThenEvents.Select(@event => @event.EventType).ToArray().SequenceEqual(["SecondEvent", "FirstEvent"]).ShouldBeTrue();
    [Fact] void should_stably_interleave_different_kinds() => _printed.IndexOf("then no readmodel", StringComparison.Ordinal).ShouldBeLessThan(_printed.IndexOf("then SecondEvent", StringComparison.Ordinal));
    [Fact] void should_keep_the_other_kinds_in_source_order() => _printed.IndexOf("then readmodel", StringComparison.Ordinal).ShouldBeLessThan(_printed.IndexOf("then query", StringComparison.Ordinal));
    [Fact] void should_print_identically_twice() => _printedAgain.ShouldEqual(_printed);
}
