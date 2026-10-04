// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_enforcing_atomic_command_fact_budgets : Specification
{
    const string Source = """
        module Billing
          feature Budget
            slice Automation Budget
              command Batch
                id String identifier
                produces Added
                  for id
              reaction Invoke
                when Started
                  produces Prior
                  invokes Batch
                    id = "new-source"
              readmodel Total
                id String
                count Decimal
              query TotalById => Total optional
                by id String
              projection Total => Total
                from Added key $eventSourceId
                  id = $eventSourceId
                  add count by 1
              event Added
              event Started
              event Prior
              specification Budget
                given clock "2026-10-02T09:00:00Z"
                given Added
                  for "existing"
                when Batch
                  id = "new-source"
                then error
        """;

    [Fact]
    void should_accept_exactly_one_thousand_initiating_command_facts_without_counting_established_history() =>
        Check(1000, false, false);

    [Fact]
    void should_refuse_an_entire_initiating_command_batch_before_adopting_any_fact_or_projection() =>
        Check(1001, false, false);

    [Fact]
    void should_not_leak_an_allocated_event_source_from_an_over_budget_initiating_command() =>
        Check(1001, false, true);

    [Fact]
    void should_keep_prior_cascade_facts_when_an_invoked_command_exceeds_the_remaining_budget() =>
        Check(999, true, false);

    [Fact]
    void should_accept_an_invoked_command_that_exactly_fills_the_remaining_budget() =>
        Check(998, true, false);

    [Fact]
    void should_keep_capture_appends_individually_accepted_up_to_the_same_budget()
    {
        var source = Source.Replace("slice Automation", "slice Translate", StringComparison.Ordinal);
        var reactionStart = source.IndexOf("      reaction", StringComparison.Ordinal);
        var reactionEnd = source.IndexOf("      readmodel", StringComparison.Ordinal);
        source = source[..reactionStart] + "      capture Records\n        key id\n        append Added\n" + source[reactionEnd..];
        var model = given.v6_regression_models.Compile(source);
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var capture = slice.Captures.Single();
        var specification = slice.Specifications.Single() with
        {
            When = null,
            WhenCapture = new(capture.Id, new([new("id", SemanticCaptureFieldKind.Value) { Value = SemanticValue.Text("new-source") }]))
        };
        model = Replace(model, slice with
        {
            Captures = [capture with { Appends = [.. Enumerable.Repeat(capture.Appends.Single(), 1001)] }],
            Specifications = [specification]
        });
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            run.Passed.ShouldBeFalse();
            run.Execution.World.Facts.Length.ShouldEqual(1001);
            run.Execution.World.ReadModels.Single(instance => instance.Key == SemanticValue.Text("new-source")).Values.Single(value => value.Value is SemanticNumberValue).Value.ShouldEqual(SemanticValue.Number(1000));
        }
    }

    [Fact]
    void should_preserve_the_legacy_command_profile_without_introducing_a_fact_budget()
    {
        var legacySource = Source[..Source.IndexOf("      reaction", StringComparison.Ordinal)].Replace("slice Automation", "slice StateChange", StringComparison.Ordinal) + """
              event Added
              specification Legacy
                when Batch
                  id = "new-source"
                then Added
                  for "new-source"
        """;
        var model = given.v6_regression_models.Compile(legacySource);
        model.SemanticVersion.ShouldEqual(SemanticVersion.V2);
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        model = Replace(model, slice with { Commands = [command with { Produces = [.. Enumerable.Repeat(command.Produces.Single(), 1001)] }] });
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticAccepted>();
            run.Execution.World.Facts.Length.ShouldEqual(1001);
        }
    }

    static void Check(int count, bool invoked, bool allocated)
    {
        var model = Model(count, invoked, allocated);
        var overBudget = count + (invoked ? 2 : 0) > 1000;
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            if (overBudget)
            {
                var unsupported = (SemanticUnsupported)run.Execution;
                unsupported.Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
                run.Passed.ShouldBeFalse();
                unsupported.World.Facts.Length.ShouldEqual(invoked ? 3 : 1);
                unsupported.World.Facts.Any(fact => fact.Destination == SemanticValue.Text("new-source")).ShouldBeFalse();
                unsupported.World.ReadModels.Length.ShouldEqual(1);
            }
            else
            {
                run.Execution.ShouldBeOfExactType<SemanticAccepted>();
                run.Execution.World.Facts.Length.ShouldEqual(1001);
            }

            run.Execution.World.ReadModels.Single(instance => instance.Key == SemanticValue.Text("existing")).Values.Single(value => value.Value is SemanticNumberValue).Value.ShouldEqual(SemanticValue.Number(1));
        }
    }

    static ExecutableSemanticModel Model(int count, bool invoked, bool allocated)
    {
        var source = Source;
        if (allocated)
        {
            source = source.Replace("produces Added\n          for id", "produces Added", StringComparison.Ordinal)
                .Replace("when Batch\n          id = \"new-source\"", "when Batch\n          for \"new-source\"\n          id = \"identifier-not-allocation\"", StringComparison.Ordinal);
        }

        if (invoked)
        {
            source = source.Replace("when Batch\n          id = \"new-source\"", "when append Started", StringComparison.Ordinal);
        }

        var model = given.v6_regression_models.Compile(source);
        var slice = model.Application.Modules.Single().Features.Single().Slices.Single();
        var command = slice.Commands.Single();
        return Replace(model, slice with { Commands = [command with { Produces = [.. Enumerable.Repeat(command.Produces.Single(), count)] }] });
    }

    static ExecutableSemanticModel Replace(ExecutableSemanticModel model, SemanticSlice slice)
    {
        var module = model.Application.Modules.Single();
        var feature = module.Features.Single();
        return ExecutableSemanticModel.Create(model.LanguageVersion, model.SemanticVersion, model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice] }] }]
        });
    }
}
