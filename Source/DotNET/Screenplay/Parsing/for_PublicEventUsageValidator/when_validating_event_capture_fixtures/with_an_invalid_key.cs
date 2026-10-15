// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_event_capture_fixtures;

public class with_an_invalid_key : Specification
{
    [Theory]
    [InlineData("other String", "id")]
    [InlineData("id String optional", "id")]
    [InlineData("id String", "$eventSourceId")]
    void should_require_the_key_on_every_source_event(string property, string key)
    {
        var application = new ScreenplayCompiler().Parse($$"""
            module M
              feature F
                slice Translate Receive
                  direction inbound
                  event Arrived from "store"
                    {{property}}
                  capture Feed
                    source events
                      from Arrived
                    key {{key}}
            """).Value!;
        var context = ParserContext.ForDiagnostics();
        PublicEventUsageValidator.Validate(application, context);
        context.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.CaptureEventsKeyNotRequired);
        context.Diagnostics.Single().Location.Line.ShouldEqual(7);
    }
}
