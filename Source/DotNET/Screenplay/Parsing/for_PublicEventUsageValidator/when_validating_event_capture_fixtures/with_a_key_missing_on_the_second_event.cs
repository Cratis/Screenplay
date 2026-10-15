// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_event_capture_fixtures;

public class with_a_key_missing_on_the_second_event : Specification
{
    ParserContext _context;

    void Because()
    {
        var application = new ScreenplayCompiler().Parse("""
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Arrived from "store"
                    id String
                  event Updated from "store"
                    other String
                  capture Feed
                    source events
                      from Arrived
                      from Updated
                    key id
            """).Value!;
        _context = ParserContext.ForDiagnostics();
        PublicEventUsageValidator.Validate(application, _context);
    }

    [Fact] void should_check_every_source_event() => _context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.CaptureEventsKeyNotRequired);
    [Fact] void should_name_the_event_missing_the_key() => _context.Diagnostics.Single().Message.ShouldContain("Updated");
}
