// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_event_capture_fixtures;

public class with_capture_records : Specification
{
    [Theory]
    [InlineData("given")]
    [InlineData("when")]
    void should_require_an_event_fixture(string step)
    {
        var application = new ScreenplayCompiler().Parse($$"""
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Arrived from "store"
                    id String
                  capture Feed
                    source events
                      from Arrived
                    key id
                  specification S
                    {{step}} capture Feed
                      id = "one"
            """).Value!;
        var context = ParserContext.ForDiagnostics();
        PublicEventUsageValidator.Validate(application, context);
        context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.CaptureEventsRecordFixture);
        context.Diagnostics.Single().Location.Line.ShouldEqual(12);
    }
}
