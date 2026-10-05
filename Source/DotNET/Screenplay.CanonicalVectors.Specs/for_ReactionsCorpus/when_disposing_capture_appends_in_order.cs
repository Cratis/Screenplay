// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_ReactionsCorpus;

public class when_disposing_capture_appends_in_order : Specification
{
    const string Source = """
        module Probe
          feature F
            slice Translate T
              capture Records
                key id
                append First
                append Later
                  value = $.payload
              readmodel Total
                id String
                count Decimal
              query TotalById => Total optional
                by id String
              projection Total => Total
                from First key $eventSourceId
                  id = $eventSourceId
                  add count by 1
              event First
              event Later
                value String optional
              constraint Once
                unique event First
              specification Present
                GIVEN
                when capture Records
                  id = "root"
                  RECORD
                EXPECTATION
        """;

    [Fact]
    void should_report_an_earlier_constraint_rejection_before_an_unreached_structured_mapping()
    {
        foreach (var payload in new[] { "{\"value\":1}", "\"ok\"" })
        {
            foreach (var run in Runs(Model("payload = " + payload, "given First\n          for \"root\"")))
            {
                Rejected(run, SemanticRejectionCategory.Constraint, 1);
                ((SemanticRejected)run.Execution).Code.ShouldEqual("Once");
                Count(run, 1);
            }
        }
    }

    [Fact]
    void should_retain_an_accepted_prefix_and_projection_when_a_later_mapping_is_unsupported()
    {
        foreach (var payload in new[] { "{\"value\":1}", "[{\"value\":1}]", "[]" })
        {
            foreach (var run in Runs(Model("payload = " + payload)))
            {
                Unsupported(run, 1);
                ((SemanticUnsupported)run.Execution).Details.ShouldContain("payload");
                Count(run, 1);
            }
        }
    }

    [Fact]
    void should_retain_an_accepted_prefix_and_projection_when_a_later_value_is_rejected()
    {
        foreach (var run in Runs(Model("payload = 1")))
        {
            Rejected(run, SemanticRejectionCategory.Contract, 1);
            Count(run, 1);
        }
    }

    [Fact]
    void should_retain_an_accepted_prefix_when_a_later_append_constraint_rejects()
    {
        var source = Text("payload = \"ok\"", "given Later\n          for \"root\"\n          value = \"before\"")
            .Replace("      specification", "      constraint LaterOnce\n        unique event Later\n      specification", StringComparison.Ordinal);
        foreach (var run in Runs(given.v6_regression_models.Compile(source)))
        {
            Rejected(run, SemanticRejectionCategory.Constraint, 2);
            ((SemanticRejected)run.Execution).Code.ShouldEqual("LaterOnce");
            Count(run, 1);
        }
    }

    [Fact]
    void should_accept_each_valid_append_once_in_authored_order()
    {
        var model = Model("payload = \"ok\"", expectation: "then First\n          for \"root\"\n        then Later\n          for \"root\"\n          value = \"ok\"");
        foreach (var run in Runs(model))
        {
            run.Execution.ShouldBeOfExactType<SemanticAccepted>();
            run.Passed.ShouldBeTrue();
            run.Execution.World.Facts.Length.ShouldEqual(2);
            Count(run, 1);
            run.Execution.World.Facts.Select(fact => EventName(model, fact)).ShouldEqual(["First", "Later"]);
        }
    }

    [Fact]
    void should_accept_missing_and_null_optional_actuals_without_converting_present_objects_to_null()
    {
        foreach (var record in new[] { "", "payload = null" })
        {
            foreach (var run in Runs(Model(record)))
            {
                run.Execution.ShouldBeOfExactType<SemanticAccepted>();
                run.Execution.World.Facts.Length.ShouldEqual(2);
                run.Execution.World.Facts[^1].Values.Single().Value.ShouldEqual(SemanticValue.Null);
            }
        }
    }

    [Fact]
    void should_evaluate_later_guards_only_after_the_earlier_append_disposition()
    {
        foreach (var seed in new[] { "", "given First\n          for \"root\"" })
        {
            var source = Text("payload = {\"value\":1}", seed)
                .Replace("append Later\n          value", "append Later\n          when `payload == null`\n            value", StringComparison.Ordinal);
            foreach (var run in Runs(given.v6_regression_models.Compile(source)))
            {
                if (seed.Length == 0) Unsupported(run, 1);
                else Rejected(run, SemanticRejectionCategory.Constraint, 1);
                Count(run, 1);
            }
        }
    }

    [Fact]
    void should_keep_root_and_earlier_child_facts_before_a_reached_child_map_is_unsupported()
    {
        var source = Text("items = [{\"id\":1,\"payload\":\"ok\"},{\"id\":2,\"payload\":{\"value\":1}}]")
            .Replace("        append Later\n          value = $.payload", "        children items identified by id\n          map\n            value = payload\n          append Later\n            value = $.value", StringComparison.Ordinal);
        foreach (var run in Runs(given.v6_regression_models.Compile(source)))
        {
            Unsupported(run, 2);
            Count(run, 1);
            run.Execution.World.Facts[^1].Values.Single().Value.ShouldEqual(SemanticValue.Text("ok"));
        }
    }

    [Fact]
    void should_keep_root_and_child_facts_before_a_reached_nested_mapping_is_unsupported()
    {
        var source = Text("items = [{\"id\":1,\"payload\":\"ok\"}]\n          contact = {\"payload\":{\"value\":1}}", "given capture Records\n          id = \"root\"\n          items = [{\"id\":2,\"payload\":\"removed\"}]\n          contact = {\"payload\":\"before\"}")
            .Replace("        append Later\n          value = $.payload", "        children items identified by id\n          append Later\n            value = $.payload\n          append Removed\n            when removed\n              value = $.payload\n        nested contact\n          append Later\n            value = $.payload", StringComparison.Ordinal)
            .Replace("      event First", "      event Removed\n        value String optional\n      event First", StringComparison.Ordinal);
        var model = given.v6_regression_models.Compile(source);
        var before = model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().GivenCaptures.Single();
        foreach (var run in Runs(model))
        {
            Unsupported(run, 3);
            Count(run, 1);
            run.Execution.World.Facts.Select(fact => EventName(model, fact)).ShouldEqual(["First", "Later", "Removed"]);

            // The scenario reads immutable supplied last-seen records; it never commits source state or acknowledgement.
            model.Application.Modules.Single().Features.Single().Slices.Single().Specifications.Single().GivenCaptures.Single().ShouldEqual(before);
        }
        foreach (var run in Runs(model)) Unsupported(run, 3);
    }

    [Fact]
    void should_refuse_all_effects_for_invalid_later_child_shapes_identities_or_duplicates()
    {
        foreach (var record in new[] { "items = 1", "items = [{\"payload\":\"ok\"}]", "items = [{\"id\":1},{\"id\":1.00}]", "contact = 1" })
        {
            var source = Text(record).Replace("        append Later\n          value = $.payload", "        children items identified by id\n          append Later\n            value = $.payload\n        nested contact\n          append Later\n            value = $.payload", StringComparison.Ordinal);
            foreach (var run in Runs(given.v6_regression_models.Compile(source)))
            {
                Rejected(run, SemanticRejectionCategory.Contract, 0);
                run.Execution.World.ReadModels.ShouldBeEmpty();
            }
        }
    }

    [Fact]
    void should_settle_an_earlier_append_reaction_before_evaluating_a_later_unsupported_mapping()
    {
        var source = Text("payload = {\"value\":1}") + "\n" + """
            slice Automation Derived
              reaction Follow
                when First
                  produces Followed
              event Followed
        """;
        var model = given.v6_regression_models.Compile(source);
        foreach (var run in Runs(model))
        {
            Unsupported(run, 2);
            run.Execution.World.Facts.Select(fact => EventName(model, fact)).ShouldEqual(["First", "Followed"]);
        }
    }

    [Fact]
    void should_report_an_earlier_reaction_constraint_rejection_before_a_later_capture_mapping()
    {
        var source = Text("payload = {\"value\":1}", "given Followed\n          for \"root\"") + "\n" + """
            slice Automation Derived
              reaction Follow
                when First
                  produces Followed
              event Followed
              constraint DerivedOnce
                unique event Followed
        """;
        foreach (var run in Runs(given.v6_regression_models.Compile(source)))
        {
            Rejected(run, SemanticRejectionCategory.Constraint, 2);
            ((SemanticRejected)run.Execution).Code.ShouldEqual("DerivedOnce");
            Count(run, 1);
        }
    }

    [Fact]
    void should_not_advance_supplied_last_seen_records_on_success_or_terminal_failure()
    {
        foreach (var payload in new[] { "\"ok\"", "{\"value\":1}", "1" })
        {
            var model = Model("payload = " + payload, "given capture Records\n          id = \"root\"\n          payload = \"before\"");
            var bytes = SemanticModelSerializer.Serialize(model);
            var first = Runs(model).ToArray();
            var second = Runs(model).ToArray();
            SemanticModelSerializer.Serialize(model).ShouldEqual(bytes);
            for (var index = 0; index < first.Length; index++)
            {
                second[index].Execution.Kind.ShouldEqual(first[index].Execution.Kind);
                second[index].Execution.World.Facts.Select(fact => fact.EventContract).ShouldEqual(first[index].Execution.World.Facts.Select(fact => fact.EventContract));
                second[index].Execution.World.Facts.SelectMany(fact => fact.Values).ShouldEqual(first[index].Execution.World.Facts.SelectMany(fact => fact.Values));
            }
        }
    }

    [Fact]
    void should_stop_at_the_shared_reaction_fact_limit_before_reaching_a_later_capture_mapping()
    {
        var source = Text("payload = {\"value\":1}") + "\n" + """
            slice Automation Cycle
              reaction Follow
                when First
                  produces Looped
                when Looped
                  produces Looped
              event Looped
        """;
        foreach (var run in Runs(given.v6_regression_models.Compile(source)))
        {
            Unsupported(run, 1000);
            ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Reaction);
            ((SemanticUnsupported)run.Execution).Details.ShouldContain("1000");
            Count(run, 1);
        }
    }

    [Fact]
    void should_report_earlier_projection_overflow_before_later_mapping_unsupported()
    {
        var source = Text("payload = {\"value\":1}", "given First\n          for \"root\"")
            .Replace("      constraint Once\n        unique event First\n", "", StringComparison.Ordinal)
            .Replace("add count by 1", "add count by 70000000000000000000000000000", StringComparison.Ordinal);
        foreach (var run in Runs(given.v6_regression_models.Compile(source)))
        {
            Unsupported(run, 1);
            ((SemanticUnsupported)run.Execution).Capability.ShouldEqual(SemanticExecutionCapability.Projection);
            Count(run, 70000000000000000000000000000m);
        }
    }

    static string EventName(ExecutableSemanticModel model, SemanticFact fact) => model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).SelectMany(slice => slice.Events).Single(contract => contract.Id == fact.EventContract).Name;

    static void Count(SemanticSpecificationRun run, decimal count) => run.Execution.World.ReadModels.Single().Values.Select(value => value.Value).OfType<SemanticNumberValue>().Single().Value.ShouldEqual(count);

    static void Unsupported(SemanticSpecificationRun run, int facts)
    {
        run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        run.Passed.ShouldBeFalse();
        run.Execution.World.Facts.Length.ShouldEqual(facts);
    }

    static void Rejected(SemanticSpecificationRun run, SemanticRejectionCategory category, int facts)
    {
        run.Execution.ShouldBeOfExactType<SemanticRejected>();
        ((SemanticRejected)run.Execution).Category.ShouldEqual(category);
        run.Passed.ShouldBeTrue();
        run.Execution.World.Facts.Length.ShouldEqual(facts);
    }

    static string Text(string record, string given = "", string expectation = "then error") => Source.Replace("GIVEN", given, StringComparison.Ordinal).Replace("RECORD", record, StringComparison.Ordinal).Replace("EXPECTATION", expectation, StringComparison.Ordinal);

    static ExecutableSemanticModel Model(string record, string seed = "", string expectation = "then error") => given.v6_regression_models.Compile(Text(record, seed, expectation));

    static IEnumerable<SemanticSpecificationRun> Runs(ExecutableSemanticModel model) => given.v6_regression_models.Runs(model);
}
