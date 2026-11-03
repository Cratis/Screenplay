// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticEventRouting;

public class when_typing_published_fixtures : Specification
{
    static readonly ApplicationIdentity Application = ApplicationIdentity.Create("Published");
    static readonly SemanticTypeReference Text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
    static readonly SemanticTypeReference Uuid = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
    static readonly SemanticEventStream Stream = new(Id("Stream"), "Ledger", "stored-ledger");
    static readonly SemanticEventSource Source = new(Id("Source"), "Project", "stored-project", [Stream]);
    static readonly SemanticFixtureRoute Route = new(Source.Id, Stream.Id);
    static readonly SemanticId Before = Id("Before");
    static readonly SemanticId Other = Id("Other");
    static readonly SemanticId Published = Id("Published");
    static readonly SemanticId Second = Id("Second");

    readonly Dictionary<string, (SemanticTypeReference? Type, string Failure)> _results = [];

    void Because()
    {
        Resolve("from", Model([Command(Before, Uuid)], [Publisher(Published, From(Before))]));
        Resolve("every", Model([Command(Before, Uuid), Command(Other, Uuid)], [Publisher(Published, null, Every())]));
        Resolve("ambiguous every", Model([Command(Before, Uuid), Command(Other, Text)], [Publisher(Published, null, Every())]));
        Resolve("ambiguous from every", Model([Command(Before, Uuid), Command(Other, Text)], [Publisher(Published, From(Before), Every())]));
        Resolve("from every", Model([Command(Before, Uuid), Command(Other, Uuid)], [Publisher(Published, From(Before), Every())]));
        Resolve("none", Model([], []));
        Resolve("unknown", Model([Command(Other, Uuid)], [Publisher(Published, From(Before))]));
        var cycle = Model([Command(Before, Uuid)],
            [Publisher(Published, From(Second, Before)), Publisher(Second, From(Published)), Publisher(Before, null, Every())]);
        Resolve("cycle", cycle);
        _results["second"] = (SemanticEventRouting.FixtureSourceType(cycle, Route, Second, out var failure), failure);
    }

    void Resolve(string name, SemanticApplication app)
    {
        var type = SemanticEventRouting.FixtureSourceType(app, Route, Published, out var failure);
        _results[name] = (type, failure);
    }

    [Fact]
    void should_type_a_published_event_by_the_facts_its_from_scope_folds()
    {
        _results["from"].Type.ShouldEqual(Uuid);
        _results["from"].Failure.ShouldEqual(string.Empty);
    }

    [Fact]
    void should_type_an_every_only_publisher_by_the_one_type_all_producible_facts_share()
    {
        _results["every"].Type.ShouldEqual(Uuid);
        _results["every"].Failure.ShouldEqual(string.Empty);
    }

    [Fact]
    void should_refuse_an_every_only_publisher_as_ambiguous_when_producible_facts_differ()
    {
        _results["ambiguous every"].Type.ShouldBeNull();
        _results["ambiguous every"].Failure.ShouldEqual("ambiguous");
    }

    [Fact]
    void should_refuse_a_from_and_every_publisher_as_ambiguous_when_another_fact_differs()
    {
        _results["ambiguous from every"].Type.ShouldBeNull();
        _results["ambiguous from every"].Failure.ShouldEqual("ambiguous");
        _results["from every"].Type.ShouldEqual(Uuid);
    }

    [Fact]
    void should_keep_none_for_a_published_event_nothing_produces()
    {
        _results["none"].Type.ShouldBeNull();
        _results["none"].Failure.ShouldEqual("none");
        _results["unknown"].Type.ShouldBeNull();
        _results["unknown"].Failure.ShouldEqual("none");
    }

    [Fact]
    void should_terminate_for_event_projections_that_fold_each_others_targets()
    {
        _results["cycle"].Type.ShouldEqual(Uuid);
        _results["cycle"].Failure.ShouldEqual(string.Empty);
        _results["second"].Type.ShouldEqual(Uuid);
    }

    static SemanticProjectionFrom[] From(params SemanticId[] events) =>
        [.. events.Select(contract => new SemanticProjectionFrom(contract, SemanticProjectionKey.EventSourceIdentity, null, []))];

    static SemanticProjectionEvery Every() => new(true, false, []);

    static SemanticProjection Publisher(SemanticId target, SemanticProjectionFrom[]? from, SemanticProjectionEvery? every = null) =>
        new(Id($"Publisher{target}"), "Publisher", target, [])
        {
            Target = SemanticProjectionTargetKind.Event,
            Scope = SemanticProjectionScope.Empty with { From = from is null ? [] : [.. from], Every = every }
        };

    static SemanticCommand Command(SemanticId produced, SemanticTypeReference destination) =>
        new(Id($"Command{produced}"), "Command", [], [], [new(produced, null, null, [])]) { Destination = new(destination, null) };

    static SemanticApplication Model(ImmutableArray<SemanticCommand> commands, ImmutableArray<SemanticProjection> projections) =>
        new SemanticApplication(Id("Application"), "Published", [], [], [new(Id("Module"), "Module", [new(Id("Feature"), "Feature", [new(Id("Nested"), "Nested", [], [new(Id("Slice"), "Slice", SemanticSliceKind.StateChange, [], commands, [], projections, [], [])])], [])])]) with { EventSources = [Source] };

    static SemanticId Id(string name) => SemanticId.Create(SemanticAddress.ForConcept(Application, name));
}
