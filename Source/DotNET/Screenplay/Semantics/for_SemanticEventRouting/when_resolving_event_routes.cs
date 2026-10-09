// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics.for_SemanticEventRouting;

public class when_resolving_event_routes : Specification
{
    static readonly ApplicationIdentity Application = ApplicationIdentity.Create("Routes");
    static readonly SemanticTypeReference Text = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Text);
    static readonly SemanticTypeReference Uuid = SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.Uuid);
    static readonly SemanticId IntegerId = Id("Integer");
    static readonly SemanticTypeReference Integer = SemanticTypeReference.ForConcept(IntegerId);
    static readonly ImmutableArray<SemanticConcept> Concepts = [new(IntegerId, "Integer", SemanticPrimitiveType.WholeNumber, [], [])];
    static readonly SemanticEventStream Stream = new(Id("Stream"), "Ledger", "stored-ledger");
    static readonly SemanticEventSource Source = new(Id("Source"), "Project", "stored-project", [Stream]);
    static readonly SemanticFixtureRoute Route = new(Source.Id, Stream.Id);

    [Fact]
    void should_activate_only_for_sources_command_routes_or_fixture_route_assertions()
    {
        var app = Model();
        SemanticEventRouting.Uses(app).ShouldBeFalse();
        SemanticEventRouting.Uses(app with { EventSources = [Source] }).ShouldBeTrue();
        SemanticEventRouting.Uses(Model([Command(Text) with { Route = new(Source.Id, Stream.Id) }])).ShouldBeTrue();
        foreach (var specification in new[]
        {
            Specification() with { GivenEvents = [new(Id("Event"), []) { Route = Route }] },
            Specification() with { ThenEvents = [new(Id("Event"), []) { Route = Route }] },
            Specification() with { ThenEvents = [new(Id("Event"), []) { Unrouted = true }] },
            Specification() with { WhenAppended = new(Id("Event"), []) { Route = Route } }
        })
        {
            SemanticEventRouting.Uses(Model(specifications: [specification])).ShouldBeTrue();
        }
    }

    [Fact]
    void should_resolve_a_stream_only_under_its_source_and_keep_stored_names()
    {
        var app = Model() with { EventSources = [Source] };
        var resolved = SemanticEventRouting.Resolve(app, Source.Id, Stream.Id);
        resolved.Source.ShouldEqual(Source);
        resolved.Stream.ShouldEqual(Stream);
        Catch.Exception(() => SemanticEventRouting.Resolve(app, Id("Foreign"), Stream.Id)).ShouldBeOfExactType<InvalidSemanticContract>();
        Catch.Exception(() => SemanticEventRouting.Resolve(app, Source.Id, Id("Foreign"))).ShouldBeOfExactType<InvalidSemanticContract>();
        SemanticEventRouting.StoredName(null, "Name").ShouldEqual("Name");
        SemanticEventRouting.StoredName("pin", "Name").ShouldEqual("pin");
    }

    [Fact]
    void should_use_only_the_portable_scalar_subset()
    {
        SemanticEventRouting.ScalarKind(Text, Concepts).ShouldEqual(StreamIdScalarKind.Text);
        SemanticEventRouting.ScalarKind(Uuid, Concepts).ShouldEqual(StreamIdScalarKind.Uuid);
        SemanticEventRouting.ScalarKind(Integer, Concepts).ShouldEqual(StreamIdScalarKind.WholeNumber);
        foreach (var type in new[] { Text with { IsOptional = true }, Text with { IsCollection = true }, SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.WholeNumber), SemanticTypeReference.ForPrimitive(SemanticPrimitiveType.DecimalNumber), SemanticTypeReference.ForConcept(Id("Missing")), SemanticTypeReference.ForCompositeType(Id("Composite")) })
        {
            Catch.Exception(() => SemanticEventRouting.ScalarKind(type, Concepts)).ShouldBeOfExactType<InvalidSemanticContract>();
        }
    }

    [Fact]
    void should_format_unkeyed_routes_without_materializing_defaults()
    {
        Format(Stream, null, [], out var route, out var failure).ShouldBeTrue();
        route.ShouldEqual(new SemanticEventRoute(Source.SourceKind, Stream.StreamKind, null));
        failure.ShouldEqual(StreamIdFormatFailure.None);
        Format(Stream, SemanticValue.Text("unexpected"), [], out route, out _).ShouldBeFalse();
        route.ShouldBeNull();
    }

    [Fact]
    void should_preserve_adjacent_integer_keys_at_both_double_bounds()
    {
        var stream = Stream with { StreamIdType = Integer };
        foreach (var integer in new[] { -9007199254740991m, -9007199254740990m, 0m, 9007199254740990m, 9007199254740991m })
        {
            Format(stream, SemanticValue.Number(integer), [], out var route, out _).ShouldBeTrue();
            route!.StreamId.ShouldEqual(integer.ToString(System.Globalization.CultureInfo.InvariantCulture));
            SemanticEventRouting.TryDecode(stream, route.StreamId!, Concepts, out var values, out _).ShouldBeTrue();
            values![0].ShouldEqual(route.StreamId);
        }
        Format(stream, SemanticValue.Number(9007199254740992m), [], out var refused, out var failure).ShouldBeFalse();
        refused.ShouldBeNull();
        failure.ShouldEqual(StreamIdFormatFailure.OutOfRange);
        Format(stream, SemanticValue.Number(1.5m), [], out _, out failure).ShouldBeFalse();
        failure.ShouldEqual(StreamIdFormatFailure.NotIntegral);
    }

    [Fact]
    void should_format_composites_in_declaration_order_and_decode_strictly()
    {
        var stream = Stream with { StreamIdParts = [new("project", Uuid), new("period", Text), new("number", Integer)] };
        Format(stream, null, [new("number", SemanticValue.Number(9007199254740991m)), new("period", SemanticValue.Text("a|b%7C")), new("project", SemanticValue.Text("3FA85F6457174562B3FC2C963F66AFA6"))], out var route, out _).ShouldBeTrue();
        route!.StreamId.ShouldEqual("3fa85f64-5717-4562-b3fc-2c963f66afa6|a%7Cb%257C|9007199254740991");
        SemanticEventRouting.TryDecode(stream, route.StreamId!, Concepts, out var parts, out _).ShouldBeTrue();
        parts!.ToArray().ShouldEqual(["3fa85f64-5717-4562-b3fc-2c963f66afa6", "a|b%7C", "9007199254740991"]);
        foreach (var malformed in new[] { "x|y", route.StreamId!.Replace("%7C", "%7c", StringComparison.Ordinal), route.StreamId!.Replace("3fa85f64", "3FA85F64", StringComparison.Ordinal) })
        {
            SemanticEventRouting.TryDecode(stream, malformed, Concepts, out _, out _).ShouldBeFalse();
        }
        Format(stream, null, [new("project", SemanticValue.Text("x"))], out _, out _).ShouldBeFalse();
        Format(stream, null, [new("project", SemanticValue.Text("x")), new("project", SemanticValue.Text("x")), new("number", SemanticValue.Number(1))], out _, out _).ShouldBeFalse();
    }

    [Fact]
    void should_refuse_text_without_rewriting_it_and_scalar_noncanonical_ids()
    {
        var stream = Stream with { StreamIdType = Text };
        foreach (var text in new[] { string.Empty, "e\u0301", "\ud800" })
        {
            Format(stream, SemanticValue.Text(text), [], out var route, out _).ShouldBeFalse();
            route.ShouldBeNull();
        }
        Format(stream, SemanticValue.Text("p-1:2026-10"), [], out var opaque, out _).ShouldBeTrue();
        opaque!.StreamId.ShouldEqual("p-1:2026-10");
        SemanticEventRouting.TryDecode(Stream with { StreamIdType = Integer }, "01", Concepts, out _, out _).ShouldBeFalse();
        SemanticEventRouting.TryDecode(Stream with { StreamIdType = Integer }, "-0", Concepts, out _, out _).ShouldBeFalse();
        SemanticEventRouting.TryDecode(Stream with { StreamIdType = Uuid }, "3FA85F64-5717-4562-B3FC-2C963F66AFA6", Concepts, out _, out _).ShouldBeFalse();
    }

    [Fact]
    void should_type_fixtures_by_the_source_before_considering_producers()
    {
        var app = Model([Command(Uuid)]) with { EventSources = [Source with { IdentifierType = Text }] };
        SemanticEventRouting.FixtureSourceType(app, Route, Id("Event"), out var failure).ShouldEqual(Text);
        failure.ShouldEqual(string.Empty);
        SemanticEventRouting.FixtureSourceType(app with { Modules = [] }, Route, Id("Event"), out _).ShouldEqual(Text);
        app = Model([Command(Uuid), Command(Uuid)]) with { EventSources = [Source] };
        SemanticEventRouting.FixtureSourceType(app, Route, Id("Event"), out _).ShouldEqual(Uuid);
        app = Model([Command(Uuid), Command(Text)]) with { EventSources = [Source] };
        SemanticEventRouting.FixtureSourceType(app, Route, Id("Event"), out failure).ShouldBeNull();
        failure.ShouldEqual("ambiguous");
        SemanticEventRouting.FixtureSourceType(Model() with { EventSources = [Source] }, Route, Id("Event"), out failure).ShouldBeNull();
        failure.ShouldEqual("none");
    }

    [Fact]
    void should_refuse_mixed_key_shapes_and_return_a_failure_for_missing_later_parts()
    {
        var stream = Stream with { StreamIdParts = [new("first", Text), new("second", Text)] };
        Format(stream, null, [new("first", SemanticValue.Text("a")), new("foreign", SemanticValue.Text("b"))], out var route, out var failure).ShouldBeFalse();
        route.ShouldBeNull();
        failure.ShouldEqual(StreamIdFormatFailure.Arity);
        Format(stream with { StreamIdType = Text }, SemanticValue.Text("a"), [], out _, out _).ShouldBeFalse();
        Format(stream, SemanticValue.Text("a"), [new("first", SemanticValue.Text("a")), new("second", SemanticValue.Text("b"))], out _, out _).ShouldBeFalse();
        Format(stream, null, [new("first", SemanticValue.Text("a")), new("second", SemanticValue.Text(string.Empty))], out route, out failure).ShouldBeFalse();
        route.ShouldBeNull();
        failure.ShouldEqual(StreamIdFormatFailure.Empty);
        var enumeration = new SemanticConcept(Id("Enum"), "Enum", SemanticPrimitiveType.Text, ["open", "closed"], []);
        Catch.Exception(() => SemanticEventRouting.ScalarKind(SemanticTypeReference.ForConcept(enumeration.Id), [enumeration])).ShouldBeOfExactType<InvalidSemanticContract>();
    }

    [Fact]
    void should_include_reaction_and_capture_producers_in_fixture_type_fallback()
    {
        var triggered = new SemanticReactionTrigger(SemanticReactionTriggerKind.Event)
        {
            Source = Id("Before"),
            Produces = [new(Id("Event"), null, null, [])]
        };
        var reaction = new SemanticReaction(Id("Reaction"), "Reaction", [triggered]);
        var producer = new SemanticCommand(Id("Producer"), "Producer", [], [], [new(Id("Before"), null, null, [])]) { Destination = new(Uuid, null) };
        var app = WithAutomation(Model([producer]), [reaction], []);
        SemanticEventRouting.FixtureSourceType(app, Route, Id("Event"), out _).ShouldEqual(Uuid);
        var cycle = new SemanticReaction(Id("Cycle"), "Cycle", [new(SemanticReactionTriggerKind.Event) { Source = Id("Event"), Produces = [new(Id("Before"), null, null, [])] }]);
        SemanticEventRouting.FixtureSourceType(WithAutomation(Model(), [reaction, cycle], []), Route, Id("Event"), out var failure).ShouldBeNull();
        failure.ShouldEqual("none");
        var append = new SemanticCaptureAppend(Id("Event"), Text, null, []);
        foreach (var capture in new[]
        {
            new SemanticCapture(Id("Capture"), "Capture", "id", [], [append]),
            new SemanticCapture(Id("Capture"), "Capture", "id", [], []) { Children = [new("children", "id", [], [append])] },
            new SemanticCapture(Id("Capture"), "Capture", "id", [], []) { Nested = [new("nested", [], [append])] }
        })
        {
            SemanticEventRouting.FixtureSourceType(WithAutomation(Model(), [], [capture]), Route, Id("Event"), out _).ShouldEqual(Text);
            SemanticEventRouting.FixtureSourceType(WithAutomation(Model([producer]), [reaction], [capture]), Route, Id("Event"), out failure).ShouldBeNull();
            failure.ShouldEqual("ambiguous");
        }
    }

    [Fact]
    void should_keep_released_version_pairs_and_support_only_the_exact_event_routes_pair()
    {
        foreach (var language in EsmSchemaV7Support.LanguageVersions)
        {
            var semantic = new SemanticVersion(language.Major, language.Minor);
            EsmSchemaV8Support.Supports(language, semantic).ShouldBeTrue();
        }
        EsmSchemaV8Support.Supports(LanguageVersion.V8, SemanticVersion.V8).ShouldBeTrue();
        LanguageVersion.Parse(LanguageVersion.V8.ToString()).ShouldEqual(LanguageVersion.V8);
        SemanticVersion.Parse(SemanticVersion.V8.ToString()).ShouldEqual(SemanticVersion.V8);
        EsmSchemaV8Support.Supports(LanguageVersion.V7, SemanticVersion.V8).ShouldBeFalse();
        EsmSchemaV7Support.Supports(LanguageVersion.V8, SemanticVersion.V8).ShouldBeFalse();
    }

    static SemanticApplication WithAutomation(SemanticApplication app, ImmutableArray<SemanticReaction> reactions, ImmutableArray<SemanticCapture> captures)
    {
        var module = app.Modules[0];
        var feature = module.Features[0];
        var nested = feature.Features[0];
        var slice = nested.Slices[0] with { Reactions = reactions, Captures = captures };

        return app with { EventSources = [Source], Modules = [module with { Features = [feature with { Features = [nested with { Slices = [slice] }] }] }] };
    }

    static bool Format(SemanticEventStream stream, SemanticValue? scalar, ImmutableArray<SemanticFixtureRoutePart> parts, out SemanticEventRoute? route, out StreamIdFormatFailure failure) =>
        SemanticEventRouting.TryFormat(Source, stream, scalar, parts, Concepts, out route, out failure);

    static SemanticCommand Command(SemanticTypeReference destination) => new(Id("Command"), "Command", [], [], [new(Id("Event"), null, null, [])]) { Destination = new(destination, null) };
    static SemanticSpecification Specification() => new(Id("Specification"), "Specification", [], [], null, [], [], [], []);
    static SemanticApplication Model(ImmutableArray<SemanticCommand> commands = default, ImmutableArray<SemanticSpecification> specifications = default) =>
        new(Id("Application"), "Routes", Concepts, [], [new(Id("Module"), "Module", [new(Id("Feature"), "Feature", [new(Id("Nested"), "Nested", [], [new(Id("Slice"), "Slice", SemanticSliceKind.StateChange, [], commands.IsDefault ? [] : commands, [], [], [], specifications.IsDefault ? [] : specifications)])], [])])]);
    static SemanticId Id(string name) => SemanticId.Create(SemanticAddress.ForConcept(Application, name));
}
