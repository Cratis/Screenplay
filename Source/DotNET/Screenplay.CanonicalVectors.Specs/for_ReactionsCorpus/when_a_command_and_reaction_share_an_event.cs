// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_a_command_and_reaction_share_an_event : Specification
{
    const string Source = """
        module Billing
          feature Steps
            slice StateChange Start
              command Start
                id String identifier
                produces Changed
                  for id
                  step = 0
              event Changed
                step Int
              specification Cascade
                when Start
                  id = "root"
                then Changed
                  for "root"
                  step = 0
                then Changed
                  for "root"
                  step = 1
            slice Automation Next
              reaction Next
                where step == 0
                when Changed
                  step
                  produces Changed
                    step = 1
        """;

    [Fact]
    void should_accept_both_occurrences_and_terminate_before_and_after_canonical_reading()
    {
        var model = given.v6_regression_models.Compile(Source);
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Passed.ShouldBeTrue();
            var facts = ((SemanticAccepted)run.Execution).Facts;
            facts.Length.ShouldEqual(2);
            facts[0].EventContract.ShouldEqual(facts[1].EventContract);
            facts.Select(fact => fact.Values.Single().Value).ShouldEqual([Semantics.SemanticValue.Number(0), Semantics.SemanticValue.Number(1)]);
        }
    }

    [Fact]
    void should_defer_the_same_event_type_when_a_reachable_reaction_invokes_its_producer()
    {
        var source = Source.Replace("      event Changed", "      command Echo\n        id String identifier\n        step Int\n        produces Changed\n          for id\n          step = step\n      event Changed", StringComparison.Ordinal)
            .Replace("          produces Changed\n            step = 1", "          invokes Echo\n            id = \"root\"\n            step = 1", StringComparison.Ordinal);
        foreach (var run in given.v6_regression_models.Runs(given.v6_regression_models.Compile(source))) run.Passed.ShouldBeTrue();
    }

    [Fact]
    void should_retain_a_provable_contradiction_when_the_reaction_is_unreachable()
    {
        var unreachable = Source.Replace("when Changed\n", "when Unrelated\n", StringComparison.Ordinal)
            .Replace("      reaction Next", "      event Unrelated\n        step Int\n      reaction Next", StringComparison.Ordinal);
        var result = new ScreenplayCompiler().Compile(unreachable);
        result.Diagnostics.Any(diagnostic => diagnostic.Code == Diagnostics.DiagnosticCodes.UnreachableSpecificationOutcome).ShouldBeTrue();
    }

    [Fact]
    void should_bound_a_reachable_cycle_at_runtime_rather_than_guessing_termination()
    {
        var model = given.v6_regression_models.Compile(Source.Replace("where step == 0", "where step >= 0", StringComparison.Ordinal));
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Passed.ShouldBeFalse();
            ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
            run.Execution.World.Facts.Length.ShouldEqual(1_000);
        }
    }
}
