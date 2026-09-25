// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax.Serialization;

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_reaction_trigger_reads : given.a_printer
{
    const string Source =
        """
        trigger Signal
          key Uuid
          @reads String

        module Orders
          feature Handling
            slice StateView Status
              readmodel Status
                value String
            slice Automation Handle
              reaction Handle
                when Signal
                  key
                  @reads String
                  reads Status as current by key
                every 15 minutes
                  reads Status
                at 08:00
                  reads Status
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_print_the_read_with_alias_and_key() => _roundtrip.Printed.ShouldContain("reads Status as current by key");
    [Fact] void should_print_the_clock_reads() => _roundtrip.Printed.Split("reads Status\n").Length.ShouldEqual(3);
    [Fact] void should_escape_a_value_named_reads() => _roundtrip.Printed.ShouldContain("@reads String");
    [Fact] void should_reparse_every_read() => _roundtrip.Reparsed.Value!.Modules.Single().Features.Single().Slices.Last()
        .Reactions.Single().Triggers.Sum(trigger => (trigger.Reads ?? []).Count()).ShouldEqual(3);
    [Fact] void should_preserve_syntax() => SyntaxJson.StructurallyEqual(_roundtrip.Original!.Value!, _roundtrip.Reparsed.Value!).ShouldBeTrue();
    [Fact] void should_stabilize_on_second_print() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
}
