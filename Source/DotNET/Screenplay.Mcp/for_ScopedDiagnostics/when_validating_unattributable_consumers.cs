// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Completeness;

namespace Cratis.Screenplay.Mcp.for_ScopedDiagnostics;

public class when_validating_unattributable_consumers : Specification
{
    ScopedDiagnosticResult _result;

    void Because()
    {
        var sources = new Dictionary<string, string>
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
                        value MissingType
                        produces CleanEvent
                          for id
                          value = value
                """
        };
        ScopedDiagnostics.TryValidate(sources, "M.F.Clean", CompletenessChecks.None, out var result, out _).ShouldBeTrue();
        _result = result!;
    }

    [Fact] void should_not_attribute_the_consumer_to_the_scope() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_not_claim_a_direct_dependent() => _result.DependentDeclarationCount.ShouldEqual(0);
    [Fact] void should_not_claim_an_affected_scope() => _result.AffectedScopes.ShouldBeEmpty();
    [Fact] void should_count_the_unresolved_event_consumer() => _result.UnresolvedEventConsumers.ReferenceCount.ShouldEqual(1);
    [Fact] void should_name_the_unresolved_consumer_scope() => _result.UnresolvedEventConsumers.Scopes.ShouldContainOnly("Other.F.Use");
    [Fact] void should_count_other_unresolved_references_separately() => _result.PossiblyAffectedReferenceCount.ShouldEqual(1);
    [Fact] void should_retain_whole_application_warnings() => _result.WholeApplicationWarningCount.ShouldBeGreaterThan(0);
}
