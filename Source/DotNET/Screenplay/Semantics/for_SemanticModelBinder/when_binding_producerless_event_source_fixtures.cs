// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.CompilerServices;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Execution;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_producerless_event_source_fixtures : given.a_semantic_binder
{
    static string CaseSource(string type) => Source()
        .Replace("specification CountingSlots", $"specification CountingSlots\n        parameter day {type}\n        case First day = \"2026-11-10\"")
        .Replace("for \"2026-11-10\"", "for case.day");

    static string Command(string name, string type) =>
        $"\n    slice StateChange {name}\n      command {name}\n        id {type} identifier\n        dayId String\n        produces SlotTaken\n          for id\n          dayId = dayId\n";

    static string Source([CallerFilePath] string path = "") => File.ReadAllText(Path.GetFullPath(
        Path.Combine(Path.GetDirectoryName(path)!, "../../../../Screenplay/Compiler/Conformance/producerless-event-source.play")));

    [Fact]
    void should_bind_and_run_history_keyed_by_the_projected_read_model() => Run(Source());

    [Fact]
    void should_use_the_explicit_identity_instead_of_an_event_payload_property() =>
        Run(Source().Replace("given SlotTaken\n          for \"2026-11-10\"\n          dayId = \"2026-11-10\"", "given SlotTaken\n          for \"2026-11-10\"\n          dayId = \"a-different-payload\""));

    [Fact]
    void should_replay_an_append_keyed_by_the_projected_read_model() =>
        Run(Source().Replace("given SlotTaken", "when append SlotTaken"));

    [Fact]
    void should_bind_a_producerless_event_example() =>
        Run(Source().Replace("      specification CountingSlots", "      example SavedSlot : SlotTaken\n        for \"2026-11-10\"\n        dayId = \"2026-11-10\"\n      specification CountingSlots")
            .Replace("given SlotTaken\n          for \"2026-11-10\"\n          dayId = \"2026-11-10\"", "given SavedSlot"));

    [Fact]
    void should_preserve_an_explicit_event_source_projection_key() =>
        Run(Source().Replace("from SlotTaken", "from SlotTaken key $eventSourceId"));

    [Fact]
    void should_refuse_a_fixture_without_an_identity_contract() =>
        Refuse(Source().Replace("from SlotTaken", "from SlotTaken key dayId"));

    [Fact]
    void should_name_the_undecidable_event_and_explain_how_to_supply_its_identity()
    {
        var result = Bind(Source().Replace("from SlotTaken", "from SlotTaken key dayId"));
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding &&
            diagnostic.Message == "Event 'SlotTaken' needs one unambiguous required scalar event-source identity type. Declare a read-model projection keyed on the event source ($eventSourceId) or a producing command with a required scalar destination.");
    }

    [Fact]
    void should_bind_a_case_parameter_as_a_producerless_event_source() => Run(CaseSource("String"));

    [Fact]
    void should_bind_a_case_parameter_overriding_an_event_example_source() =>
        Run(CaseSource("String")
            .Replace("      specification CountingSlots", "      example SavedSlot : SlotTaken\n        for \"2026-11-10\"\n        dayId = \"2026-11-10\"\n      specification CountingSlots")
            .Replace("given SlotTaken", "given SavedSlot"));

    [Fact]
    void should_refuse_an_optional_case_parameter_for_the_required_projected_identity() =>
        Bind(CaseSource("String?")).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.OptionalSpecificationParameterTarget);

    [Fact]
    void should_refuse_an_optional_case_parameter_overriding_an_event_example_source() =>
        Bind(CaseSource("String?")
            .Replace("      specification CountingSlots", "      example SavedSlot : SlotTaken\n        for \"2026-11-10\"\n        dayId = \"2026-11-10\"\n      specification CountingSlots")
            .Replace("given SlotTaken", "given SavedSlot"))
            .Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.OptionalSpecificationParameterTarget);

    [Fact]
    void should_refuse_an_incompatible_case_parameter_for_the_projected_identity() =>
        Bind(CaseSource("Date")).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.IncompatibleSpecificationParameterType);

    [Fact]
    void should_refuse_a_noncanonical_scalar_literal() =>
        Refuse(Source().Replace("for \"2026-11-10\"", "for 42"));

    [Fact]
    void should_refuse_conflicting_projected_identity_types() =>
        Refuse(Source() +
            """

                  readmodel OtherDay
                    id Int key
                  projection OtherProjection => OtherDay
                    from SlotTaken
                      id = 1
            """);

    [Fact]
    void should_refuse_history_without_a_projected_key() =>
        Refuse(Source().Replace("      query DayById => DayView optional\n        by dayId String\n", ""));

    [Theory]
    [InlineData("?")]
    [InlineData("[]")]
    void should_refuse_optional_or_collection_identity_types(string modifier) =>
        Refuse(Source().Replace("dayId String", $"dayId String{modifier}"));

    [Theory]
    [InlineData("pii")]
    [InlineData("secret")]
    void should_refuse_protected_projected_identities(string marker)
    {
        var result = Bind($"concept Protected : String {marker}\n" + Source().Replace("dayId String", "dayId Protected"));
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Message.Contains("compliance", StringComparison.Ordinal));
    }

    [Fact]
    void should_preserve_the_single_producer_identity_type() =>
        Refuse(Source() + Command("TakeSlot", "Int"));

    [Fact]
    void should_accept_distinct_producer_destinations_with_one_projected_identity() =>
        Run(Source() + Command("TakeSlot", "Int") + Command("ReserveSlot", "Uuid"));

    [Fact]
    void should_refuse_distinct_producer_types_without_a_projected_identity() =>
        Refuse(Source().Replace("from SlotTaken", "from SlotTaken key dayId") + Command("TakeSlot", "Int") + Command("ReserveSlot", "Uuid"));

    void Run(string source)
    {
        var result = Bind(source);
        result.Diagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeEmpty();
        result.Success.ShouldBeTrue();
        var model = result.Value!.Model;
        var restored = SemanticModelSerializer.Deserialize(SemanticModelSerializer.Serialize(model));
        var plan = SemanticExecutionPlan.Compile(restored).Plan!;
        var specification = plan.Specifications.Values.Single();
        var run = new SemanticSpecificationRunner().Run(plan, specification.Id);
        run.Failures.ShouldBeEmpty();
        run.Passed.ShouldBeTrue();
    }

    void Refuse(string source) =>
        Bind(source).Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding);
}
