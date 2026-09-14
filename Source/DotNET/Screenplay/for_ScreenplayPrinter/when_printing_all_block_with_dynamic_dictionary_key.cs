// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_all_block_with_dynamic_dictionary_key : given.a_printer
{
    const string Source =
        """
        module Statistics
          feature EventTracking
            slice StateView EventStatistics
              projection EventStatistics => EventStatisticsReadModel
                all
                  count eventCountByType.$eventContext.eventType.id
                  increment processingAttempts.$eventContext.causationId
                  decrement pendingItems.$eventContext.correlationId
                  lastOccurred = $eventContext.occurred
        """;

    given.a_printer.RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_compile_without_diagnostics() => _roundtrip.Original!.Diagnostics.ShouldBeEmpty();
    [Fact] void should_reparse_without_diagnostics() => _roundtrip.Reparsed.Diagnostics.ShouldBeEmpty();
    [Fact] void should_print_the_same_text_on_a_second_pass() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    [Fact] void should_preserve_count_with_dynamic_key() => _roundtrip.Printed.ShouldContain("count eventCountByType.$eventContext.eventType.id");
    [Fact] void should_preserve_increment_with_dynamic_key() => _roundtrip.Printed.ShouldContain("increment processingAttempts.$eventContext.causationId");
    [Fact] void should_preserve_decrement_with_dynamic_key() => _roundtrip.Printed.ShouldContain("decrement pendingItems.$eventContext.correlationId");
    [Fact] void should_not_escape_mid_path_keywords() => _roundtrip.Printed.ShouldNotContain("@id");
    [Fact] void should_preserve_the_all_block() => _roundtrip.Printed.ShouldContain("all");
}
