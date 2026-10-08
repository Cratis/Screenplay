// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_an_event_is_renamed : Specification
{
    ScopedDiagnosticResult _result;
    Dictionary<string, string> _sources;
    McpSnapshot _snapshot;

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

    void Because()
    {
        _snapshot = new(_sources);
        _result = ScopedDiagnostics.Select(_snapshot, "M.F.Clean")!;
    }

    [Fact] void should_not_include_the_broken_consumer_in_scoped_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_claim_a_directly_affected_scope() => _result.AffectedScopes.ShouldBeEmpty();
    [Fact] void should_not_count_the_consumer_as_a_dependent() => _result.DependentDeclarationCount.ShouldEqual(0);
    [Fact] void should_report_the_unresolved_event_reference() => _result.UnresolvedEventConsumers.ReferenceCount.ShouldEqual(1);
    [Fact] void should_report_the_unresolved_consumer_scope() => _result.UnresolvedEventConsumers.Scopes.ShouldContainOnly("Other.F.Use");
    [Fact] void should_not_count_the_event_consumer_again_as_possibly_affected() => _result.PossiblyAffectedReferenceCount.ShouldEqual(0);
    [Fact] void should_preserve_the_whole_application_diagnostic() => _snapshot.Compilation.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("CleanEvent", StringComparison.Ordinal)).ShouldBeTrue();
}
