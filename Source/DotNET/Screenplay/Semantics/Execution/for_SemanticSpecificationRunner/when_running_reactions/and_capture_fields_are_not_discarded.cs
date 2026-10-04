// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner.when_running_reactions;

public class and_capture_fields_are_not_discarded : given.a_v6_scenario
{
    [Fact]
    void should_refuse_whole_structured_fields_instead_of_accepting_optional_null()
    {
        foreach (var value in new[] { "{\"name\":\"present\"}", "[{\"name\":\"present\"}]", "[]" })
        {
            var run = Present($"payload = {value}", "String optional");
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            run.Passed.ShouldBeFalse();
            run.Execution.World.Facts.Length.ShouldEqual(0);
        }
    }

    [Fact]
    void should_refuse_structured_sources_in_all_map_operations()
    {
        foreach (var map in new[] { "value = payload", "value = `${payload}`", "split payload by \",\"\n    value" })
        {
            var run = Present("payload = {\"name\":\"present\"}", "String optional", "value", map);
            run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
            run.Passed.ShouldBeFalse();
        }
    }

    [Fact]
    void should_refuse_unknown_dotted_traversal_instead_of_treating_it_as_absent() =>
        Present("payload = {\"name\":\"present\"}", "String optional", "payload.name").Execution.ShouldBeOfExactType<SemanticUnsupported>();

    [Fact]
    void should_accept_genuinely_missing_and_explicit_null_optional_fields()
    {
        foreach (var record in new[] { "", "payload = null" })
        {
            var accepted = (SemanticAccepted)Present(record, "String optional").Execution;
            accepted.Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Null);
        }
    }

    [Fact]
    void should_reject_genuinely_missing_and_explicit_null_required_fields()
    {
        Present("", "String").Execution.ShouldBeOfExactType<SemanticRejected>();
        Present("payload = null", "String").Execution.ShouldBeOfExactType<SemanticRejected>();
    }

    [Fact]
    void should_not_fall_back_to_the_enclosing_value_when_a_child_explicitly_states_null()
    {
        Compile("""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
                    children items identified by id
                      append Recorded
                        value = $.payload
                  event Recorded
                    value String optional
                  specification PresentRecord
                    when capture Records
                      id = "root"
                      payload = "enclosing"
                      items = [{"id":1,"payload":null}]
                    then Recorded
                      value = "expected"
            """);
        ((SemanticAccepted)Run("PresentRecord").Execution).Facts.Single().Values.Single().Value.ShouldEqual(SemanticValue.Null);
    }

    [Fact]
    void should_refuse_structured_nested_map_sources_without_committing()
    {
        Compile("""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
                    nested contact
                      map
                        value = payload
                      append Recorded
                        value = $.value
                  event Recorded
                    value String optional
                  specification PresentRecord
                    when capture Records
                      id = "root"
                      contact = {"payload":{"name":"present"}}
                    then Recorded
                      value = "expected"
            """);
        var run = Run("PresentRecord");
        run.Execution.ShouldBeOfExactType<SemanticUnsupported>();
        run.Passed.ShouldBeFalse();
        run.Execution.World.Facts.Length.ShouldEqual(0);
    }

    [Fact]
    void should_reject_incomplete_or_unzoned_capture_instants()
    {
        foreach (var value in new[] { "12:00", "12:00Z", "2026-10", "2026-10-02", "2026-10-02T12:00:00", "10-02T12:00:00Z", "2026-02-30T12:00:00Z" })
        {
            var run = Present($"payload = \"{value}\"", "DateTime optional");
            run.Execution.ShouldBeOfExactType<SemanticRejected>();
            run.Execution.World.Facts.Length.ShouldEqual(0);
        }
    }

    [Fact]
    void should_normalize_complete_zoned_capture_instants_independently_of_the_scenario_clock()
    {
        foreach (var value in new[] { "2026-10-02T12:00:00Z", "2026-10-02T14:00:00+02:00", "2026-10-02T07:00:00-05:00" })
        {
            var run = Present($"payload = \"{value}\"", "DateTime", expected: "\"2026-10-02T12:00:00Z\"");
            run.Passed.ShouldBeTrue();
            ((SemanticTextValue)((SemanticAccepted)run.Execution).Facts.Single().Values.Single().Value).Value.ShouldEqual("2026-10-02T12:00:00.0000000Z");
        }
    }

    SemanticSpecificationRun Present(string record, string type, string field = "payload", string? map = null, string? expected = null)
    {
        var mapBlock = map is null ? "" : "        map\n          " + map.Replace("\n", "\n          ", StringComparison.Ordinal) + "\n";
        var assertion = "          value = " + (expected ?? (type.StartsWith("DateTime", StringComparison.Ordinal) ? "\"2026-10-02T12:00:00Z\"" : "\"expected\""));
        Compile($$"""
            module Billing
              feature Import
                slice Translate Records
                  capture Records
                    key id
            {{mapBlock}}        append Recorded
                      value = $.{{field}}
                  event Recorded
                    value {{type}}
                  specification PresentRecord
                    given clock "1980-01-01T00:00:00Z"
                    when capture Records
                      id = "root"
                      {{record}}
                    then Recorded
            {{assertion}}
            """);
        return Run("PresentRecord");
    }
}
