// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_evaluating_unsupported_capture_sources : Specification
{
    [Fact]
    void should_never_satisfy_an_error_assertion_with_an_unsupported_guard()
    {
        foreach (var record in new[] { "payload = {\"amount\":1}", "payload = [{\"amount\":1}]", "payload = []" })
        {
            foreach (var expression in new[] { "payload.amount > 0", "payload > 0", "!(payload > 0)", "true && payload > 0", "false || payload > 0", "payload.amount > 0 && false", "payload.amount > 0 || true", "payload == null" })
            {
                foreach (var run in given.v6_regression_models.Runs(Capture(record, $"when `{expression}`")))
                {
                    run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
                    run.Passed.ShouldBeFalse();
                    run.Execution.World.Facts.ShouldBeEmpty();
                }
            }
        }
    }

    [Fact]
    void should_short_circuit_without_reading_an_unreachable_unsupported_source()
    {
        foreach (var expression in new[] { "false && payload.amount > 0", "true || payload.amount > 0", "!(true || payload > 0)" })
        {
            foreach (var run in given.v6_regression_models.Runs(Capture("payload = {\"amount\":1}", $"when `{expression}`")))
            {
                run.Execution.ShouldBeOfExactType<SemanticAccepted>();
                run.Execution.World.Facts.Length.ShouldEqual(expression.StartsWith("true", StringComparison.Ordinal) ? 1 : 0);
            }
        }
    }

    [Fact]
    void should_distinguish_missing_numeric_operands_from_unsupported_sources()
    {
        foreach (var run in given.v6_regression_models.Runs(Capture("", "when `payload > 0`")))
        {
            ((SemanticRejected)run.Execution).Category.ShouldEqual(SemanticRejectionCategory.Contract);
            run.Passed.ShouldBeTrue();
        }
    }

    [Fact]
    void should_propagate_structured_sources_through_all_capture_map_operations()
    {
        foreach (var map in new[] { "value = payload", "value = `${payload}`", "split payload by \",\"\n  value", "value = payload.amount" })
        {
            foreach (var record in new[] { "payload = {\"amount\":1}", "payload = [{\"amount\":1}]" })
            {
                var model = given.v6_regression_models.Compile($$"""
                    module Billing
                      feature Import
                        slice Translate Records
                          capture Records
                            key id
                            map
                    {{string.Join('\n', map.Split('\n').Select(line => "          " + line))}}
                            append Seen
                              value = $.value
                          event Seen
                            value String optional
                          specification Present
                            when capture Records
                              id = "root"
                              {{record}}
                            then error
                    """);
                foreach (var run in given.v6_regression_models.Runs(model))
                {
                    run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
                    run.Passed.ShouldBeFalse();
                }
            }
        }
    }

    [Fact]
    void should_propagate_unsupported_guards_from_nested_and_child_records()
    {
        foreach (var (scope, record) in new[]
        {
            ("nested contact", "contact = {\"payload\":{\"amount\":1}}"),
            ("children items identified by id", "items = [{\"id\":1,\"payload\":[{\"amount\":1}]}]")
        })
        {
            var model = given.v6_regression_models.Compile($$"""
                module Billing
                  feature Import
                    slice Translate Records
                      capture Records
                        key id
                        {{scope}}
                          append Seen
                            when `payload.amount > 0`
                      event Seen
                      specification Present
                        when capture Records
                          id = "root"
                          {{record}}
                        then error
                """);
            foreach (var run in given.v6_regression_models.Runs(model))
            {
                run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
                run.Passed.ShouldBeFalse();
            }
        }
    }

    [Fact]
    void should_preserve_already_accepted_capture_facts_after_a_later_reaction_failure()
    {
        var model = given.v6_regression_models.Compile("""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
                    append First
                    append Second
                  reaction FollowFirst
                    when First
                      produces Earlier
                  reaction CannotFollowSecond
                    when Second
                      produces Later
                        user = $context.causedBy.userName
                  event First
                  event Second
                  event Earlier
                  event Later
                    user String
                  specification Present
                    given clock "2026-10-02T09:00:00Z"
                    when capture Records
                      id = "root"
                    then error
            """);
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            run.Passed.ShouldBeFalse();
            var plan = SemanticExecutionPlan.Compile(model).Plan!;
            run.Execution.World.Facts.Select(fact => plan.Events[fact.EventContract].Name).ShouldContainOnly("First", "Second", "Earlier");
        }
    }

    [Fact]
    void should_keep_nested_and_child_mapping_failures_unsupported_after_canonical_reading()
    {
        foreach (var (scope, record) in new[]
        {
            ("nested contact", "contact = {\"payload\":{\"amount\":1}}"),
            ("children items identified by id", "items = [{\"id\":1,\"payload\":[{\"amount\":1}]}]")
        })
        {
            var model = given.v6_regression_models.Compile($$"""
                module Billing
                  feature Import
                    slice Translate Records
                      capture Records
                        key id
                        {{scope}}
                          append Seen
                            value = $.payload
                      event Seen
                        value String optional
                      specification Present
                        when capture Records
                          id = "root"
                          {{record}}
                        then error
                """);
            foreach (var run in given.v6_regression_models.Runs(model))
            {
                run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
                run.Passed.ShouldBeFalse();
                run.Execution.World.Facts.ShouldBeEmpty();
            }
        }
    }

    [Fact]
    void should_stop_the_append_chain_before_a_later_guard_can_mask_unsupported_as_rejection()
    {
        var model = given.v6_regression_models.Compile("""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
                    append First
                      when `payload > 0`
                    append Second
                      when `missing > 0`
                  event First
                  event Second
                  specification Present
                    when capture Records
                      id = "root"
                      payload = {"amount":1}
                    then error
            """);
        foreach (var run in given.v6_regression_models.Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            run.Passed.ShouldBeFalse();
            run.Execution.World.Facts.ShouldBeEmpty();
        }
    }

    static ExecutableSemanticModel Capture(string record, string guard) => given.v6_regression_models.Compile($$"""
        module Billing
          feature Import
            slice Translate Records
              capture Records
                key id
                append Seen
                  {{guard}}
              event Seen
              specification Present
                when capture Records
                  id = "root"
                  {{record}}
                then error
        """);
}
