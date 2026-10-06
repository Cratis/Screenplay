// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_an_event_is_renamed : Specification
{
    ScopedDiagnosticResult _result;
    Dictionary<string, string> _sources;

    void Establish() => _sources = new()
    {
        ["application.play"] = """
            module M
              feature F
                slice StateChange Clean
                  event CleanedEvent
                    value String
            module Other
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    value String
                    produces CleanEvent
                      for id
                      value = value
            """
    };

    void Because() => _result = ScopedDiagnostics.Select(_sources, "M.F.Clean")!;

    [Fact] void should_include_the_broken_consumer() => _result.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("CleanEvent", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_report_the_consumer_scope() => _result.AffectedScopes.ShouldContainOnly("Other.F.Use");
    [Fact] void should_disclose_the_uncertain_former_target() => _result.PossiblyAffectedReferenceCount.ShouldEqual(1);
}
