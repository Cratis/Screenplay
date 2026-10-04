// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_projection_arithmetic_exceeds_the_range : Specification
{
    const decimal Amount = 70000000000000000000000000000m;
    const string Source = """
        module Billing
          feature Totals
            slice StateView Total
              readmodel Total
                id String
                total Decimal
              query TotalById => Total optional
                by id String
              command Start
                id String identifier
                produces Started
                  for id
              command Add
                id String identifier
                amount Decimal
                produces Marker
                  for id
                produces Added
                  for id
                  amount = amount
              projection Total => Total
                from Added key $eventSourceId
                  id = $eventSourceId
                  add total by amount
              event Added
                amount Decimal
              event Marker
              event Started
              specification Overflow
                given clock "2026-10-02T09:00:00Z"
                given Added
                  for "root"
                  amount = 70000000000000000000000000000
                ACTION
                then error
            slice Automation Cascade
              reaction Cascade
                when Started
                  produces Marker
                  produces Added
                    amount = 70000000000000000000000000000
        """;

    [Fact]
    void should_refuse_a_direct_append_without_mutating_the_established_projection() =>
        Check("when append Added\n          for \"root\"\n          amount = 70000000000000000000000000000", 1);

    [Fact]
    void should_retain_the_initiating_and_prior_cascade_facts_but_not_the_overflowing_append() =>
        Check("when append Started\n          for \"root\"", 3);

    [Fact]
    void should_keep_an_overflowing_initiating_command_atomic() =>
        Check("when Add\n          id = \"root\"\n          amount = 70000000000000000000000000000", 1);

    [Fact]
    void should_keep_an_overflowing_invoked_command_atomic()
    {
        var source = Source.Replace("produces Marker\n          produces Added\n            amount = 70000000000000000000000000000", "invokes Add\n            id = \"root\"\n            amount = 70000000000000000000000000000", StringComparison.Ordinal);
        Check("when append Started\n          for \"root\"", 2, source);
    }

    static void Check(string action, int acceptedFacts, string source = Source)
    {
        var model = given.v6_regression_models.Compile(source.Replace("ACTION", action, StringComparison.Ordinal));
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Passed.ShouldBeFalse();
            var unsupported = (SemanticUnsupported)run.Execution;
            unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Projection);
            unsupported.Details.Contains("range", StringComparison.Ordinal).ShouldBeTrue();
            unsupported.World.Facts.Length.ShouldEqual(acceptedFacts);
            unsupported.World.Facts.Count(fact => fact.EventContract == model.Application.Modules.Single().Features.Single().Slices[0].Events.Single(value => value.Name == "Added").Id).ShouldEqual(1);
            unsupported.World.ReadModels.Single().Values.Select(value => value.Value).OfType<SemanticNumberValue>().Single().Value.ShouldEqual(Amount);
        }
    }
}
