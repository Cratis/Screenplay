// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_classifying_other_unresolved_references : Specification
{
    Dictionary<string, string> _sources;
    ScopedDiagnosticResult _result;

    void Establish() => _sources = new()
    {
        ["application.play"] = """
            module M
              feature F
                slice StateChange Clean
                  event Added
                    value String
            module Other
              feature F
                slice StateChange Use
                  command Consume
                    id String identifier
                    value String
                    reads MissingView
                    produces Added
                      for id
                      value = value
                  command Unrelated
                    reads AnotherMissingView
                  command UnresolvedEventConsumer
                    id String identifier
                    produces MissingEvent
                      for id
            """
    };

    void Because() => _result = ScopedDiagnostics.Select(_sources, "M.F.Clean")!;

    [Fact] void should_report_only_the_direct_consumer_as_a_dependent() => _result.DependentDeclarationCount.ShouldEqual(1);
    [Fact] void should_count_only_the_other_unresolved_read_model_reference_as_possibly_affected() => _result.PossiblyAffectedReferenceCount.ShouldEqual(1);
    [Fact] void should_report_the_unresolved_event_separately() => _result.UnresolvedEventConsumers.ReferenceCount.ShouldEqual(1);
}
