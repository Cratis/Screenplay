// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_source_stream_authoring_coexists_with_v6 : given.a_semantic_binder
{
    const string Automation = "module Projects\n  feature Registration\n    slice Automation Reminders\n      event Tick\n        tickedAt DateTime\n      reaction Timer\n        every 1 day\n          produces Tick\n            for \"timer\"\n            tickedAt = $context.occurred\n      specification Tomorrow\n        given clock \"2026-10-02T00:00:00Z\"\n        when clock \"2026-10-03T00:00:00Z\"\n        then Tick\n          for \"timer\"\n          tickedAt = \"2026-10-03T00:00:00Z\"\n";

    [Fact]
    void should_admit_plain_v6_automation_and_clocks_without_source_authoring()
    {
        var result = Bind(Automation);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V6);
    }

    [Fact]
    void should_select_event_routes_for_a_declaration_only_source_beside_automation()
    {
        var result = Bind("eventsource Account\n  stream Transactions\n" + Automation);
        result.Success.ShouldBeTrue();
        result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V8);
        result.Value.Model.Application.EventSources.Single().Streams.Single().Name.ShouldEqual("Transactions");
    }

    [Fact]
    void should_not_admit_a_routed_handler_as_v6()
    {
        const string source = "eventsource Account\n  stream Transactions\n" + Automation + "    slice StateChange Deposit\n      command Deposit\n        stream Account.Transactions\n        handler\n          implementation\n            hint \"Record the deposit\"\n";
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268" && diagnostic.Message.Contains("handler", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    }

    [Fact]
    void should_retain_blocking_stream_property_candidates_beside_v6()
    {
        const string source = "import Account.Transactions\ntype Transactions\n  value String\neventsource Account\n  stream Transactions\n" + Automation + "    slice StateChange Deposit\n      command Deposit\n        stream Account.Transactions\n";
        var parsed = new ScreenplayCompiler().Parse(source);
        parsed.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0505").ShouldBeTrue();
        var command = parsed.Value!.Modules.Single().Features.Single().Slices.Last().Commands.Single();
        command.Stream.ShouldBeNull();
        command.StreamCandidates.Single().PropertyCandidate.ShouldNotBeNull();
        var result = Bind(source);
        result.Success.ShouldBeFalse();
        result.Value.ShouldBeNull();
        Assert.True(result.Diagnostics.Any(diagnostic => diagnostic.Code == "PLAY0268"), string.Join('\n', result.Diagnostics.Select(diagnostic => diagnostic.Code + " " + diagnostic.Message)));
    }
}
