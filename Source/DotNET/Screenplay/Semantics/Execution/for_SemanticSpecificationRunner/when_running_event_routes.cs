// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_resolving_event_routes.given;

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticSpecificationRunner;

public class when_running_event_routes : event_route_models
{
    [Fact]
    void should_compare_routes_and_keys_only_when_stated_and_require_unrouted_when_stated()
    {
        var expected = Specification().ThenEvents[0];
        Run(expected).Passed.ShouldBeTrue();
        Run(expected with { Route = Fixture(SemanticValue.Text("2026-10")) }).Passed.ShouldBeTrue();
        Run(expected with { Route = Fixture(SemanticValue.Text("other")) }).Passed.ShouldBeFalse();
        Run(expected with { Unrouted = true }).Passed.ShouldBeFalse();
        Run(expected with { Unrouted = true }, Command with { Route = null }).Passed.ShouldBeTrue();
        Run(expected with { Route = Fixture(SemanticValue.Text("2026-10")) }, Command with { Route = null }).Passed.ShouldBeFalse();
        var otherStream = Stream with { Id = SemanticId.Create(SemanticAddress.ForEventStream(SourceAddress, "Notes")), Name = "Notes", StreamKind = "Notes" };
        var specification = Specification() with { ThenEvents = [expected with { Route = new(Source.Id, otherStream.Id) { StreamId = SemanticValue.Text("2026-10") } }] };
        new SemanticSpecificationRunner().Run(Plan(source: Source with { Streams = [Stream, otherStream] }, specifications: [specification]), specification.Id).Passed.ShouldBeFalse();
    }

    [Fact]
    void should_place_given_and_when_append_routes_and_preserve_unrouted_append_behavior()
    {
        var specification = Specification() with
        {
            GivenEvents = [new(Event.Id, []) { EventSource = new(Text, SemanticValue.Text("history")), Route = Fixture(SemanticValue.Text("previous")) }]
        };
        var run = new SemanticSpecificationRunner().Run(Plan(specifications: [specification]), specification.Id);
        run.Passed.ShouldBeTrue();
        run.Execution.World.Facts[0].Route.ShouldEqual(new SemanticEventRoute("stored-project", "stored-ledger", "previous"));
        foreach (var routed in new[] { false, true })
        {
            var appended = specification with
            {
                When = null,
                WhenAppended = new(Event.Id, []) { EventSource = new(Text, SemanticValue.Text("project-1")), Route = routed ? Fixture(SemanticValue.Text("append")) : null },
                ThenEvents = []
            };
            // A direct reaction supplies the success outcome, without inheriting the append's route.
            var reaction = new SemanticReaction(SemanticId.Create(SemanticAddress.ForReaction(AutomationAddress, "Follow")), "Follow",
                [new(SemanticReactionTriggerKind.Event) { Source = Event.Id, Produces = [new(Before.Id, null, null, [])] }]);
            appended = appended with { ThenEvents = [new(Before.Id, []) { Unrouted = true }] };
            var performed = new SemanticSpecificationRunner().Run(Plan(specifications: [appended], reactions: [reaction]), appended.Id);
            performed.Passed.ShouldBeTrue();
            ((SemanticAccepted)performed.Execution).Facts[0].Route.ShouldEqual(routed ? new SemanticEventRoute("stored-project", "stored-ledger", "append") : null);
        }
    }

    [Fact]
    void should_place_routed_history_without_a_producer_or_despite_a_conflicting_producer()
    {
        var given = new SemanticSpecificationEvent(Event.Id, [])
        {
            EventSource = new(Text, SemanticValue.Text("history")),
            Route = Fixture(SemanticValue.Text("previous"))
        };
        var withoutProducer = Specification() with
        {
            GivenEvents = [given],
            ThenEvents = [],
            ThenReturns = new SemanticScalarSpecificationResponse(SemanticValue.Text("project-1"))
        };
        var noProduction = Command with { Produces = [] };
        var historyOnly = new SemanticSpecificationRunner().Run(Plan(noProduction, specifications: [withoutProducer]), withoutProducer.Id);
        historyOnly.Passed.ShouldBeTrue();
        historyOnly.Execution.World.Facts[0].Context!.EventSource.Type.ShouldEqual(Text);

        var identifier = Identity with { Type = Uuid };
        var conflicting = Command with
        {
            Properties = [identifier, Key],
            Destination = new(Uuid, Property(identifier)),
            Response = new SemanticScalarCommandResponse(identifier.Id, Uuid),
            Route = null
        };
        var specification = Specification() with
        {
            GivenEvents = [given],
            When = new(Command.Id, [new(Identity.Id, SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6")), new(Key.Id, SemanticValue.Text("period"))])
        };
        var run = new SemanticSpecificationRunner().Run(Plan(conflicting, specifications: [specification]), specification.Id);
        run.Passed.ShouldBeTrue();
        run.Execution.World.Facts[0].Context!.EventSource.Type.ShouldEqual(Text);
        run.Execution.World.Facts[1].Context!.EventSource.Type.ShouldEqual(Uuid);
    }

    [Fact]
    void should_compare_composites_canonically_and_fail_when_one_part_differs()
    {
        var stream = Stream with { StreamIdType = null, StreamIdParts = [new("project", Uuid), new("period", Text)] };
        var command = Command with
        {
            Route = new(Source.Id, Stream.Id) { StreamIdParts = [new("project", SemanticExpression.FromValue(SemanticValue.Text("3fa85f64-5717-4562-b3fc-2c963f66afa6"))), new("period", Property(Key))] }
        };
        foreach (var period in new[] { "2026-10", "2026-11" })
        {
            var specification = Specification() with
            {
                ThenEvents = [new(Event.Id, []) { Route = new(Source.Id, Stream.Id) { StreamIdParts = [new("project", SemanticValue.Text("3FA85F64-5717-4562-B3FC-2C963F66AFA6")), new("period", SemanticValue.Text(period))] } }]
            };
            var run = new SemanticSpecificationRunner().Run(Plan(command, Source with { Streams = [stream] }, [specification]), specification.Id);
            run.Passed.ShouldEqual(period == "2026-10");
        }
    }

    [Fact]
    void should_find_an_assignment_for_overlapping_route_assertions()
    {
        var specification = Specification() with
        {
            ThenEventsInAnyOrder = true,
            ThenEvents = [new(Event.Id, []), new(Event.Id, []) { Route = Fixture(SemanticValue.Text("exact")) }]
        };
        var facts = ImmutableArray.Create(Fact(new("stored-project", "stored-ledger", "exact")), Fact(new("stored-project", "stored-ledger", "other")));
        var run = new SemanticSpecificationRunner(new supplied_facts(facts)).Run(Plan(specifications: [specification]), specification.Id);
        run.Passed.ShouldBeTrue();
        var ordered = specification with { ThenEventsInAnyOrder = false };
        new SemanticSpecificationRunner(new supplied_facts(facts)).Run(Plan(specifications: [ordered]), ordered.Id).Passed.ShouldBeFalse();
        var impossible = specification with { ThenEvents = [specification.ThenEvents[1], specification.ThenEvents[1]] };
        new SemanticSpecificationRunner(new supplied_facts(facts)).Run(Plan(specifications: [impossible]), impossible.Id).Passed.ShouldBeFalse();
    }

    [Fact]
    void should_gate_assignment_matching_so_v7_keeps_its_greedy_outcome()
    {
        var exact = new SemanticSpecificationEvent(Event.Id, []) { EventSource = new(Text, SemanticValue.Text("exact")) };
        var specification = Specification() with { ThenEventsInAnyOrder = true, ThenEvents = [new(Event.Id, []), exact] };
        var facts = ImmutableArray.Create(Fact(null) with { Destination = SemanticValue.Text("exact"), Context = new(exact.EventSource!) }, Fact(null));
        foreach (var legacy in new[] { false, true })
        {
            var plan = Plan(Command with { Route = null }, specifications: [specification], legacy: legacy);
            new SemanticSpecificationRunner(new supplied_facts(facts)).Run(plan, specification.Id).Passed.ShouldEqual(!legacy);
        }
    }

    static SemanticSpecificationRun Run(SemanticSpecificationEvent expected, SemanticCommand? command = null)
    {
        var specification = Specification() with { ThenEvents = [expected] };

        return new SemanticSpecificationRunner().Run(Plan(command, specifications: [specification]), specification.Id);
    }

    sealed class supplied_facts(ImmutableArray<SemanticFact> facts) : ISemanticEvaluator
    {
        public SemanticExecutionResult Execute(SemanticExecutionPlan plan, SemanticWorld world, SemanticExecutionRequest request) =>
            new SemanticAccepted(SemanticWorld.Create([.. world.Facts, .. facts], []), facts, []);
    }
}
