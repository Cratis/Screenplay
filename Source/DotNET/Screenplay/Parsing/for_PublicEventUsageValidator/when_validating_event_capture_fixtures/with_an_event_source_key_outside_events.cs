// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Parsing.for_PublicEventUsageValidator.when_validating_event_capture_fixtures;

public class with_an_event_source_key_outside_events : Specification
{
    [Fact]
    void should_refuse_the_marker_on_a_capture_without_source_events()
    {
        var application = new ScreenplayCompiler().Parse("""
            module M
              feature F
                slice Translate Receive
                  capture Feed
                    source command Place
                    key id is event source
            """).Value!;
        var context = ParserContext.ForDiagnostics();
        PublicEventUsageValidator.Validate(application, context);
        context.Diagnostics.Any(value => value.Code == DiagnosticCodes.CaptureEventSourceKeyOutsideEvents).ShouldBeTrue();
    }
}
