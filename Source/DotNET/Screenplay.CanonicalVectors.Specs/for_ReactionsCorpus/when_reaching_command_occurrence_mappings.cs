// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_reaching_command_occurrence_mappings : Specification
{
    [Fact]
    void should_accept_an_excluded_production_without_a_clock()
    {
        foreach (var run in given.v6_regression_models.Runs(Model(false, null)))
        {
            run.Passed.ShouldBeTrue();
            var accepted = (SemanticAccepted)run.Execution;
            accepted.World.Facts.Length.ShouldEqual(2);
            accepted.Facts.Length.ShouldEqual(2);
            accepted.Facts.All(fact => fact.Occurred is null).ShouldBeTrue();
        }
    }

    [Fact]
    void should_reject_a_reached_occurrence_without_a_clock_and_retain_prior_facts()
    {
        foreach (var run in given.v6_regression_models.Runs(Model(true, null)))
        {
            run.Passed.ShouldBeTrue();
            ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
            run.Execution.World.Facts.Length.ShouldEqual(2);
        }
    }

    [Fact]
    void should_supply_the_exact_clock_to_a_reached_occurrence()
    {
        foreach (var run in given.v6_regression_models.Runs(Model(true, "2026-10-02T09:00:00Z")))
        {
            run.Passed.ShouldBeTrue();
            var accepted = (SemanticAccepted)run.Execution;
            accepted.World.Facts.Length.ShouldEqual(3);
            accepted.Facts[^1].Values.Single().Value.ShouldEqual(SemanticValue.Text("2026-10-02T09:00:00.0000000Z"));
            accepted.Facts.Select(fact => fact.Occurred).Distinct().Single().ShouldEqual(DateTimeOffset.Parse("2026-10-02T09:00:00Z", System.Globalization.CultureInfo.InvariantCulture));
            accepted.Facts[^1].ReactionOrigin.ShouldNotBeNull();
        }
    }

    [Fact]
    void should_not_commit_an_earlier_production_of_a_command_rejected_for_missing_occurrence()
    {
        var model = Model(true, null, true);
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticRejected>();
            run.Execution.World.Facts.Length.ShouldEqual(2);
            run.Passed.ShouldBeTrue();
        }
    }

    [Fact]
    void should_preserve_the_legacy_occurrence_precondition_in_the_v2_context_profile()
    {
        var model = given.v6_regression_models.Compile("""
            module Billing
              feature Flow
                slice StateChange Flow
                  command Finish
                    id String identifier
                    enabled Bool
                    produces when enabled == true
                      Finished
                        for id
                        at = $context.occurred
                  event Finished
                    at DateTime
                  specification Excluded
                    when Finish
                      id = "destination"
                      enabled = false
                    then error
            """);
        model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
            run.Passed.ShouldBeTrue();
            run.Execution.World.Facts.ShouldBeEmpty();
        }
    }

    static ExecutableSemanticModel Model(bool enabled, string? clock, bool earlierProduction = false) => given.v6_regression_models.Compile($$"""
        module Billing
          feature Flow
            slice Automation Flow
              command Finish
                id String identifier
                enabled Bool
                {{(earlierProduction ? "produces Partial\n          for id" : "")}}
                produces when enabled == true
                  Finished
                    for id
                    at = $context.occurred
              reaction Invoke
                when Started
                  enabled
                  produces Prior
                  invokes Finish
                    id = "destination"
                    enabled = enabled
              event Started
                enabled Bool
              event Prior
              event Partial
              event Finished
                at DateTime
              specification Reach
                {{(clock is null ? "" : $"given clock \"{clock}\"")}}
                when append Started
                  enabled = {{(enabled ? "true" : "false")}}
                {{(enabled && clock is null ? "then error" : "then Prior")}}
                {{(enabled && clock is not null ? "then Finished\n          for \"destination\"\n          at = \"2026-10-02T09:00:00Z\"" : "")}}
        """);
}
