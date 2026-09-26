// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_implementation_file_comments : given.a_printer
{
    const string Source = """
        module Shop
          feature Ordering
            slice StateChange Order
              command Open
                handler
                  // handler implementation
                  file Code/Handler.cs // handler note
              constraint UniqueOrder
                // constraint implementation
                file Code/Constraint.cs // constraint note
            slice StateView Orders
              readmodel OrderList
              reducer Summarize => OrderList
                on OrderOpened
                  // reducer implementation
                  file Code/Reducer.cs // reducer note
              query List => OrderList[]
                performer
                  // performer implementation
                  file Code/Performer.cs // performer note
            slice Automation Synchronize
              reaction Sync
                when OrderOpened
                  // trigger implementation
                  file Code/Reaction.cs // trigger note
        """;

    RoundTripResult _roundtrip;

    void Because() => _roundtrip = RoundTrip(Source);

    [Fact] void should_keep_handler_file_comments() => AssertLine("// handler implementation", "file Code/Handler.cs // handler note");
    [Fact] void should_keep_constraint_file_comments() => AssertLine("// constraint implementation", "file Code/Constraint.cs // constraint note");
    [Fact] void should_keep_reducer_file_comments() => AssertLine("// reducer implementation", "file Code/Reducer.cs // reducer note");
    [Fact] void should_keep_performer_file_comments() => AssertLine("// performer implementation", "file Code/Performer.cs // performer note");
    [Fact] void should_keep_trigger_file_comments() => AssertLine("// trigger implementation", "file Code/Reaction.cs // trigger note");
    [Fact] void should_print_twice_identically() => _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);

    void AssertLine(string comment, string directive)
    {
        var lines = _roundtrip.Printed.Split('\n');
        lines.Count(line => line.Trim() == comment).ShouldEqual(1);
        lines.Count(line => line.Trim() == directive).ShouldEqual(1);
        var index = Array.FindIndex(lines, line => line.Trim() == directive);
        lines[index - 1].Trim().ShouldEqual(comment);
        _roundtrip.PrintedAgain.ShouldEqual(_roundtrip.Printed);
    }
}
