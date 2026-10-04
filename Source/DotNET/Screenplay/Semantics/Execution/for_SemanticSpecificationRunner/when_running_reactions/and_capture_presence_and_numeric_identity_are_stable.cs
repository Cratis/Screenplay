// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_capture_presence_and_numeric_identity_are_stable : given.a_v6_scenario
{
    [Fact]
    void should_remove_an_explicitly_null_nested_record_once_just_like_an_omitted_record()
    {
        var omitted = Present("", "[]", "[]");
        var explicitNull = Present("contact = null", "[]", "[]");
        omitted.Passed.ShouldBeTrue();
        explicitNull.Passed.ShouldBeTrue();
        ((SemanticAccepted)explicitNull.Execution).Facts.Length.ShouldEqual(1);
        explicitNull.Execution.World.Facts.Select(fact => fact.EventContract).ShouldEqual(omitted.Execution.World.Facts.Select(fact => fact.EventContract));
    }

    [Fact]
    void should_still_reject_a_non_null_scalar_nested_record() =>
        Present("contact = \"not a record\"", "[]", "[]").Execution.ShouldBeOfExactType<SemanticRejected>();

    [Fact]
    void should_equate_numeric_child_identity_across_scale_and_signed_zero()
    {
        foreach (var (previous, current) in new[] { ("1", "1.0"), ("1.00", "1.000"), ("0", "-0.0"), ("-2.0", "-2.00") })
        {
            var run = Present("", $"[{{\"id\":{current}}}]", $"[{{\"id\":{previous}}}]");
            run.Passed.ShouldBeTrue();
            ((SemanticAccepted)run.Execution).Facts.Length.ShouldEqual(1);
            var bytes = SemanticModelSerializer.Serialize(_plan.Model);
            var roundTrip = SemanticModelSerializer.Deserialize(bytes);
            roundTrip.Revision.ShouldEqual(_plan.Model.Revision);
            _plan = SemanticExecutionPlan.Compile(roundTrip).Plan!;
            var restored = Run("PresentRecord");
            restored.Passed.ShouldBeTrue();
            restored.Execution.World.Facts.Select(fact => fact.EventContract).ShouldEqual(run.Execution.World.Facts.Select(fact => fact.EventContract));
        }
    }

    [Fact]
    void should_reject_duplicate_numeric_values_at_different_scales_before_and_after_round_trip()
    {
        foreach (var (current, previous) in new[] { ("[{\"id\":1},{\"id\":1.0}]", "[]"), ("[]", "[{\"id\":0},{\"id\":-0.0}]") })
        {
            var run = Present("", current, previous);
            run.Execution.ShouldBeOfExactType<SemanticRejected>();
            run.Execution.World.Facts.Length.ShouldEqual(0);
            _plan = SemanticExecutionPlan.Compile(SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(_plan.Model))).Plan!;
            Run("PresentRecord").Execution.ShouldBeOfExactType<SemanticRejected>();
        }
    }

    [Fact]
    void should_keep_numeric_and_text_children_separate_while_reordering_scaled_values()
    {
        var run = Present("", "[{\"id\":\"1\"},{\"id\":1.00}]", "[{\"id\":1},{\"id\":\"1\"}]");
        run.Passed.ShouldBeTrue();
        ((SemanticAccepted)run.Execution).Facts.Length.ShouldEqual(1);
    }

    SemanticSpecificationRun Present(string contact, string current, string previous)
    {
        Compile($$"""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
                    children items identified by id
                      append Added
                        when added
                      append Removed
                        when removed
                    nested contact
                      append NestedRemoved
                        when removed
                  event Added
                  event Removed
                  event NestedRemoved
                  specification PresentRecord
                    given capture Records
                      id = "root"
                      items = {{previous}}
                      contact = {"name":"before"}
                    when capture Records
                      id = "root"
                      items = {{current}}
                      {{contact}}
                    then NestedRemoved
            """);

        // Preserve the caller's decimal scale in the programmatic graph; DSL numeric parsing may normalize it.
        var module = _plan.Model.Application.Modules.Single();
        var feature = module.Features.Single();
        var slice = feature.Slices.Single();
        var specification = slice.Specifications.Single();
        var application = _plan.Model.Application with
        {
            Modules = [module with { Features = [feature with { Slices = [slice with { Specifications = [specification with
            {
                GivenCaptures = [specification.GivenCaptures.Single() with { Record = Scaled(specification.GivenCaptures.Single().Record, previous) }],
                WhenCapture = specification.WhenCapture! with { Record = Scaled(specification.WhenCapture.Record, current) }
            }] }] }] }]
        };
        _plan = SemanticExecutionPlan.Compile(ExecutableSemanticModel.Create(LanguageVersion.V6, SemanticVersion.V6, application)).Plan!;
        return Run("PresentRecord");
    }

    static SemanticCaptureRecord Scaled(SemanticCaptureRecord record, string children)
    {
        using var document = JsonDocument.Parse(children);
        var values = document.RootElement.EnumerateArray().ToArray();
        var items = record.Fields.Single(field => field.Name == "items");
        return record with
        {
            Fields = [.. record.Fields.Select(field => field.Name != "items" ? field : items with
        {
            Records = [.. items.Records.Select((child, index) => child with { Fields = [.. child.Fields.Select(value =>
                value.Name == "id" && values[index].GetProperty("id").ValueKind == JsonValueKind.Number
                    ? value with { Value = SemanticValue.Number(values[index].GetProperty("id").GetDecimal()) }
                    : value)] })]
        })]
        };
    }
}
