// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.for_ScreenplayPrinter;

public class when_printing_sublanguage_scalar_comments : given.a_printer
{
    [Fact]
    void should_keep_capture_key_comments()
    {
        const string source = """
            capture Legacy
              source webhook
                path /orders
              // identity of the captured record
              key id // key note
              append OrderCaptured
            """;
        var parsed = _compiler.CompileCapture(source);
        var printed = _printer.Print(parsed.Value!);
        AssertLine(printed, "// identity of the captured record", "key id // key note");
        _printer.Print(_compiler.CompileCapture(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_projection_sequence_comments()
    {
        const string source = """
            projection Orders => OrderList
              // source of events
              sequence orders // sequence note
              from OrderPlaced
            """;
        var parsed = _compiler.CompileProjection(source);
        var printed = _printer.Print(parsed.Value!);
        AssertLine(printed, "// source of events", "sequence orders // sequence note");
        _printer.Print(_compiler.CompileProjection(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_specification_event_order_comments()
    {
        const string source = """
            specification Appending
              when append OrderPlaced
              // compare regardless of order
              then events in any order // ordering note
              then OrderPlaced
            """;
        var parsed = _compiler.CompileSpecification(source);
        var printed = _printer.Print(parsed.Value!);
        AssertLine(printed, "// compare regardless of order", "then events in any order // ordering note");
        _printer.Print(_compiler.CompileSpecification(printed).Value!).ShouldEqual(printed);
    }

    [Fact]
    void should_keep_command_concurrency_dimension_comments()
    {
        const string source = """
            module Shop
              feature Ordering
                slice StateChange Place
                  command Place
                    concurrency
                      // protect the source
                      eventSource // source note
                      // use this stream type
                      sourceType Order // source type note
                      // use this event stream
                      streamType Orders // stream type note
                      // protect this stream
                      streamId Main // stream id note
                      // track these facts
                      events OrderPlaced // events note
            """;
        var roundtrip = RoundTrip(source);
        AssertLine(roundtrip.Printed, "// protect the source", "eventSource // source note");
        AssertLine(roundtrip.Printed, "// use this stream type", "sourceType Order // source type note");
        AssertLine(roundtrip.Printed, "// use this event stream", "streamType Orders // stream type note");
        AssertLine(roundtrip.Printed, "// protect this stream", "streamId Main // stream id note");
        AssertLine(roundtrip.Printed, "// track these facts", "events OrderPlaced // events note");
        roundtrip.PrintedAgain.ShouldEqual(roundtrip.Printed);
    }

    static void AssertLine(string printed, string comment, string directive)
    {
        var lines = printed.Split('\n');
        lines.Count(line => line.Trim() == comment).ShouldEqual(1);
        lines.Count(line => line.Trim() == directive).ShouldEqual(1);
        var index = Array.FindIndex(lines, line => line.Trim() == directive);
        lines[index - 1].Trim().ShouldEqual(comment);
    }
}
