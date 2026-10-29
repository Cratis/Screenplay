// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_possible_events;

public class with_routes_and_protected_reads : given.a_possible_events_binder
{
    const string EventSource = "eventsource Store\n  identifier String\n  stream Main\n";
    const string Protected = for_SemanticModelBinder.when_binding_protected_reads.given.a_protected_read_model.Source;
    const string LegacyProduction = "        produces Recorded\n          for id\n          value = View.value";
    const string PossibleProduction = "        handler\n          may produce\n            Recorded for id\n          implementation";

    [Fact]
    void should_preserve_entry_routes_and_tags()
    {
        var source = "eventsource Store\n  identifier Uuid\n  stream Main\n" + Source.Replace("            Created\n", "            Created\n              stream Store.Main\n              tag \"audit\"\n", StringComparison.Ordinal);
        var result = BindPossible(source);
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
        var entry = Command(result.Value!).Handler!.PossibleEvents.First();
        entry.Route.ShouldNotBeNull();
        entry.Tags.ShouldNotBeEmpty();
        SemanticDecisionReadAdmission.Producers(result.Value!.Application.Modules.Single().Features.Single().Slices).ShouldContain(value => value.EventContract == entry.EventContract && value.Routed);
    }

    [Fact]
    void should_keep_unrouted_protected_reads_admissible()
    {
        var result = BindPossible(Protected.Replace(LegacyProduction, PossibleProduction, StringComparison.Ordinal));
        Assert.True(result.Success, string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Message)));
    }

    [Fact]
    void should_refuse_a_protected_read_on_a_command_with_an_entry_route()
    {
        var possible = PossibleProduction.Replace("            Recorded for id", "            Recorded for id\n              stream Store.Main", StringComparison.Ordinal);
        Refusal(BindPossible(EventSource + Protected.Replace(LegacyProduction, possible, StringComparison.Ordinal)));
    }

    [Fact]
    void should_refuse_a_protected_read_when_another_handler_routes_its_input_event()
    {
        var source = EventSource + Protected.Replace("    slice StateView Lookup", "      command Routed\n        id String identifier\n        handler\n          may produce\n            Recorded for id\n              stream Store.Main\n          implementation\n    slice StateView Lookup", StringComparison.Ordinal);
        Refusal(BindPossible(source));
    }

    static void Refusal(CompilationResult<PossibleEventsBinding> result)
    {
        result.Success.ShouldBeFalse();
        result.Diagnostics.ShouldContain(diagnostic => diagnostic.Code == DiagnosticCodes.UnprotectableDecisionRead && diagnostic.Message.Contains(nameof(SemanticDecisionReadRefusal.NamedSourceOrStream), StringComparison.Ordinal));
    }
}
