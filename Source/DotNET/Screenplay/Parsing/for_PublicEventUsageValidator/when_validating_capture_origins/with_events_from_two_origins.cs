// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_capture_origins;

public class with_events_from_two_origins : Specification
{
    Syntax.ApplicationSyntax _application;
    ParserContext _context;

    void Establish()
    {
        _application = new ScreenplayCompiler().Parse("import Outside.First from \"z/store\"\nimport Outside.Second from \"a/store\"\nmodule M\n  feature F\n    slice Translate Receive\n      direction inbound\n      capture Feed\n        source events\n          from First\n          from Second\n        key id\n").Value!;
        _context = ParserContext.ForDiagnostics();
    }

    void Because() => PublicEventUsageValidator.Validate(_application, _context);

    [Fact] void should_report_one_diagnostic() => _context.Diagnostics.Count.ShouldEqual(1);
    [Fact] void should_report_the_capture_origins_code() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.CaptureEventsFromSeveralOrigins);
    [Fact] void should_report_an_error() => _context.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Error);
    [Fact] void should_name_the_capture_and_sorted_origins() => _context.Diagnostics.Single().Message.ShouldEqual("The 'from' events of capture 'Feed' originate from several event stores (a/store, z/store); a capture reads one event store - split it into one capture per origin.");
    [Fact] void should_locate_the_source_events_block() => _context.Diagnostics.Single().Location.Line.ShouldEqual(8);
}
