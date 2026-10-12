// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticSpecification Specification(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "specification");
        var state = new SpecificationState(schemaVersion);
        SpecificationState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticSpecificationAppend SpecificationAppend(ref Utf8JsonReader reader, uint schemaVersion)
    {
        var value = SpecificationEvent(ref reader, schemaVersion, false);
        return new(value.EventContract, value.Values) { EventSource = value.EventSource, Route = value.Route };
    }

    internal static SemanticSpecificationEvent SpecificationEvent(ref Utf8JsonReader reader, uint schemaVersion, bool then)
    {
        Object(ref reader, "specification event");
        var state = new SpecificationEventState(schemaVersion, then);
        SpecificationEventState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticSpecificationCommand SpecificationCommand(ref Utf8JsonReader reader, uint schemaVersion)
    {
        var state = new SpecificationCommandState(schemaVersion);
        SpecificationCommandState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticSpecificationReadModel SpecificationReadModel(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification read model");
        var state = new SpecificationReadModelState();
        SpecificationReadModelState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticSpecificationAbsentReadModel SpecificationAbsentReadModel(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification absent read model");
        var state = new SpecificationAbsentReadModelState();
        SpecificationAbsentReadModelState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticSpecificationQueryResult SpecificationQuery(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "specification query result");
        var state = new SpecificationQueryState(schemaVersion);
        SpecificationQueryState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticSpecificationError SpecificationError(ref Utf8JsonReader reader)
    {
        Object(ref reader, "specification error");
        var state = new SpecificationErrorState();
        SpecificationErrorState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticPropertyValue PropertyValue(ref Utf8JsonReader reader)
    {
        Object(ref reader, "property value");
        var state = new PropertyValueState();
        PropertyValueState.Fields.Read(ref reader, state);
        return state.Build();
    }

    sealed class SpecificationState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<SpecificationState> Fields = TableFor<SpecificationState>("specification")
            .Add("givenEvents", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.GivenEvents = Array(ref reader, (ref Utf8JsonReader item) => SpecificationEvent(ref item, state.SchemaVersion, false), property))
            .Add("givenReadModels", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.GivenReadModels = Array(ref reader, SpecificationReadModel, property))
            .Add("givenCaller", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.GivenCaller = Nested(ref reader, Caller, property))
            .Add("when", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.When = Nested(ref reader, (ref Utf8JsonReader item) => SpecificationCommand(ref item, state.SchemaVersion), property))
            .Add("whenAppended", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.WhenAppended = Nested(ref reader, (ref Utf8JsonReader item) => SpecificationAppend(ref item, state.SchemaVersion), property))
            .Add("thenEventsInAnyOrder", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenEventsInAnyOrder = OnlyTrue(ref reader, property, "specification"))
            .Add("thenEvents", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenEvents = Array(ref reader, (ref Utf8JsonReader item) => SpecificationEvent(ref item, state.SchemaVersion, true), property))
            .Add("thenReadModels", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenReadModels = Array(ref reader, SpecificationReadModel, property))
            .Add("thenAbsentReadModels", 5, (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenAbsentReadModels = Array(ref reader, SpecificationAbsentReadModel, property))
            .Add("thenQueries", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenQueries = Array(ref reader, (ref Utf8JsonReader item) => SpecificationQuery(ref item, state.SchemaVersion), property))
            .Add("thenErrors", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenErrors = Array(ref reader, SpecificationError, property))
            .Add("thenDenied", (ref Utf8JsonReader reader, SpecificationState state, string property) => state.ThenDenied = OnlyTrue(ref reader, property, "specification"))
            .Add("thenReturns", 7, (ref Utf8JsonReader reader, SpecificationState state, string property) => state.Returns = Nested(ref reader, ThenReturns, property))
            .AddAll(["givenClock", "givenCaptures", "whenClock", "whenTrigger", "whenCapture"], 6, (ref Utf8JsonReader reader, SpecificationState state, string property) => state.Automation.Read(ref reader, property));

        public ImmutableArray<SemanticSpecificationEvent> GivenEvents { get; set; }

        public ImmutableArray<SemanticSpecificationReadModel> GivenReadModels { get; set; }

        public SemanticSpecificationCommand? When { get; set; }

        public SemanticSpecificationAppend? WhenAppended { get; set; }

        public bool ThenEventsInAnyOrder { get; set; }

        public ImmutableArray<SemanticSpecificationEvent> ThenEvents { get; set; }

        public ImmutableArray<SemanticSpecificationReadModel> ThenReadModels { get; set; }

        public ImmutableArray<SemanticSpecificationAbsentReadModel> ThenAbsentReadModels { get; set; }

        public ImmutableArray<SemanticSpecificationQueryResult> ThenQueries { get; set; }

        public ImmutableArray<SemanticSpecificationError> ThenErrors { get; set; }

        public SemanticCaller? GivenCaller { get; set; }

        public bool ThenDenied { get; set; }

        public SemanticSpecificationResponse? Returns { get; set; }

        public AutomationSpecification Automation { get; } = new();

        bool HasGiven => !GivenEvents.IsDefault && !GivenReadModels.IsDefault;

        bool HasThen => !ThenEvents.IsDefault && !ThenReadModels.IsDefault && !ThenQueries.IsDefault && !ThenErrors.IsDefault;

        bool HasAbsentReadModels => SchemaVersion < 5 || !ThenAbsentReadModels.IsDefault;

        public SemanticSpecification Build()
        {
            Required(HasIdentity && HasGiven && HasThen && HasAbsentReadModels, "specification");
            return new(Id, Name!, GivenEvents, GivenReadModels, When, ThenEvents, ThenReadModels, ThenQueries, ThenErrors)
            {
                GivenCaller = GivenCaller,
                ThenDenied = ThenDenied,
                ThenReturns = Returns,
                WhenAppended = WhenAppended,
                ThenEventsInAnyOrder = ThenEventsInAnyOrder,
                ThenAbsentReadModels = SchemaVersion >= 5 ? ThenAbsentReadModels : [],
                GivenClock = Automation.GivenClock,
                GivenCaptures = Automation.GivenCaptures,
                WhenClock = Automation.WhenClock,
                WhenTrigger = Automation.WhenTrigger,
                WhenCapture = Automation.WhenCapture
            };
        }
    }

    sealed class SpecificationEventState(uint schemaVersion, bool then) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<SpecificationEventState> Fields = new FieldTable<SpecificationEventState>("specification event")
            .Add("eventContract", (ref Utf8JsonReader reader, SpecificationEventState state, string property) => state.EventContract = Identifier(ref reader, property))
            .Add("values", (ref Utf8JsonReader reader, SpecificationEventState state, string property) => state.Values = Array(ref reader, PropertyValue, property))
            .Add("eventSource", (ref Utf8JsonReader reader, SpecificationEventState state, string property) => state.Source = Nested(ref reader, EventSource, property))
            .Add("route", LanguageVersion.V8.Major, (ref Utf8JsonReader reader, SpecificationEventState state, string property) => state.Route = Nested(ref reader, FixtureRoute, property))
            .Add("unrouted", LanguageVersion.V8.Major, (ref Utf8JsonReader reader, SpecificationEventState state, string property) => state.ReadUnrouted(ref reader, property), state => state.Then);

        public bool Then { get; } = then;

        public SemanticId EventContract { get; set; }

        public ImmutableArray<SemanticPropertyValue> Values { get; set; }

        public SemanticEventSourceIdentity? Source { get; set; }

        public SemanticFixtureRoute? Route { get; set; }

        public bool Unrouted { get; set; }

        public void ReadUnrouted(ref Utf8JsonReader reader, string property)
        {
            Unrouted = Boolean(ref reader, property);
            Required(Unrouted, "unrouted assertion");
        }

        public SemanticSpecificationEvent Build()
        {
            Required(EventContract.IsSet && !Values.IsDefault, "specification event");
            return new(EventContract, Values) { EventSource = Source, Route = Route, Unrouted = Unrouted };
        }
    }

    sealed class SpecificationCommandState(uint schemaVersion) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<SpecificationCommandState> Fields = new FieldTable<SpecificationCommandState>("specification command")
            .Add("command", (ref Utf8JsonReader reader, SpecificationCommandState state, string property) => state.Command = Identifier(ref reader, property))
            .Add("values", (ref Utf8JsonReader reader, SpecificationCommandState state, string property) => state.Values = Array(ref reader, PropertyValue, property))
            .Add("eventSource", (ref Utf8JsonReader reader, SpecificationCommandState state, string property) => state.Source = Nested(ref reader, EventSource, property))
            .Add("generatedValues", 7, (ref Utf8JsonReader reader, SpecificationCommandState state, string property) => state.ReadGeneratedValues(ref reader, property));

        public SemanticId Command { get; set; }

        public ImmutableArray<SemanticPropertyValue> Values { get; set; }

        public ImmutableArray<SemanticPropertyValue> GeneratedValues { get; set; } = [];

        public SemanticEventSourceIdentity? Source { get; set; }

        public void ReadGeneratedValues(ref Utf8JsonReader reader, string property)
        {
            GeneratedValues = Array(ref reader, PropertyValue, property);
            Required(!GeneratedValues.IsEmpty, "generated values");
        }

        public SemanticSpecificationCommand Build()
        {
            Required(Command.IsSet && !Values.IsDefault, "specification command");
            return new(Command, Values) { EventSource = Source, GeneratedValues = GeneratedValues };
        }
    }

    sealed class SpecificationReadModelState() : ReadState(0)
    {
        public static readonly FieldTable<SpecificationReadModelState> Fields = new FieldTable<SpecificationReadModelState>("specification read model")
            .Add("readModel", (ref Utf8JsonReader reader, SpecificationReadModelState state, string property) => state.ReadModel = Identifier(ref reader, property))
            .Add("key", (ref Utf8JsonReader reader, SpecificationReadModelState state, string property) => state.Key = Nested(ref reader, Value, property))
            .Add("values", (ref Utf8JsonReader reader, SpecificationReadModelState state, string property) => state.Values = Array(ref reader, PropertyValue, property))
            .Add("exactly", (ref Utf8JsonReader reader, SpecificationReadModelState state, string property) => state.Exactly = OnlyTrue(ref reader, property, "specification read model"));

        public SemanticId ReadModel { get; set; }

        public SemanticValue? Key { get; set; }

        public ImmutableArray<SemanticPropertyValue> Values { get; set; }

        public bool Exactly { get; set; }

        public SemanticSpecificationReadModel Build()
        {
            Required(ReadModel.IsSet && Key is not null && !Values.IsDefault, "specification read model");
            return new(ReadModel, Key!, Values) { Exactly = Exactly };
        }
    }

    sealed class SpecificationAbsentReadModelState() : ReadState(0)
    {
        public static readonly FieldTable<SpecificationAbsentReadModelState> Fields = new FieldTable<SpecificationAbsentReadModelState>("specification absent read model")
            .Add("readModel", (ref Utf8JsonReader reader, SpecificationAbsentReadModelState state, string property) => state.ReadModel = Identifier(ref reader, property))
            .Add("key", (ref Utf8JsonReader reader, SpecificationAbsentReadModelState state, string property) => state.Key = Nested(ref reader, Value, property));

        public SemanticId ReadModel { get; set; }

        public SemanticValue? Key { get; set; }

        public SemanticSpecificationAbsentReadModel Build()
        {
            Required(ReadModel.IsSet && Key is not null, "specification absent read model");
            return new(ReadModel, Key!);
        }
    }

    sealed class SpecificationQueryState(uint schemaVersion) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<SpecificationQueryState> Fields = new FieldTable<SpecificationQueryState>("specification query result")
            .Add("query", (ref Utf8JsonReader reader, SpecificationQueryState state, string property) => state.Query = Identifier(ref reader, property))
            .Add("key", (ref Utf8JsonReader reader, SpecificationQueryState state, string property) => state.Key = Nested(ref reader, Value, property))
            .Add("filters", LanguageVersion.V14.Major, (ref Utf8JsonReader reader, SpecificationQueryState state, string property) => state.Filters = Array(ref reader, QueryFilterValue, property))
            .Add("results", (ref Utf8JsonReader reader, SpecificationQueryState state, string property) => state.Results = Array(ref reader, SpecificationReadModel, property))
            .Add("exactly", (ref Utf8JsonReader reader, SpecificationQueryState state, string property) => state.Exactly = OnlyTrue(ref reader, property, "specification query result"));

        public SemanticId Query { get; set; }

        public SemanticValue? Key { get; set; }

        public ImmutableArray<SemanticQueryFilterValue> Filters { get; set; } = [];

        public ImmutableArray<SemanticSpecificationReadModel> Results { get; set; }

        public bool Exactly { get; set; }

        public SemanticSpecificationQueryResult Build()
        {
            Required(Query.IsSet && Key is not null && !Results.IsDefault, "specification query result");
            return new(Query, Key!, Results) { Exactly = Exactly, Filters = Filters };
        }
    }

    sealed class SpecificationErrorState() : ReadState(0)
    {
        public static readonly FieldTable<SpecificationErrorState> Fields = new FieldTable<SpecificationErrorState>("specification error")
            .Add("code", (ref Utf8JsonReader reader, SpecificationErrorState state, string property) => state.ReadCode(ref reader, property))
            .Add("message", (ref Utf8JsonReader reader, SpecificationErrorState state, string property) => state.ReadMessage(ref reader, property));

        public string? Code { get; set; }

        public bool CodeRead { get; set; }

        public string? Message { get; set; }

        public bool MessageRead { get; set; }

        public void ReadCode(ref Utf8JsonReader reader, string property)
        {
            CodeRead = true;
            Code = NullableString(ref reader, property);
        }

        public void ReadMessage(ref Utf8JsonReader reader, string property)
        {
            MessageRead = true;
            Message = NullableString(ref reader, property);
        }

        public SemanticSpecificationError Build()
        {
            Required(CodeRead && MessageRead, "specification error");
            return new(Code, Message);
        }
    }

    sealed class PropertyValueState() : ReadState(0)
    {
        public static readonly FieldTable<PropertyValueState> Fields = new FieldTable<PropertyValueState>("property value")
            .Add("targetProperty", (ref Utf8JsonReader reader, PropertyValueState state, string property) => state.Target = Identifier(ref reader, property))
            .Add("value", (ref Utf8JsonReader reader, PropertyValueState state, string property) => state.Content = Nested(ref reader, Value, property));

        public SemanticId Target { get; set; }

        public SemanticValue? Content { get; set; }

        public SemanticPropertyValue Build()
        {
            Required(Target.IsSet && Content is not null, "property value");
            return new(Target, Content!);
        }
    }
}
#pragma warning restore IDE0350
