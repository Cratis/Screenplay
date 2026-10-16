// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticSlice Slice(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "slice");
        var state = new SliceState(schemaVersion);
        SliceState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticReducer Reducer(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "reducer");
        var state = new ReducerState(schemaVersion);
        ReducerState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticReducerTransition ReducerTransition(ref Utf8JsonReader reader)
    {
        Object(ref reader, "reducer transition");
        var state = new ReducerTransitionState();
        ReducerTransitionState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticReadModel ReadModel(ref Utf8JsonReader reader)
    {
        Object(ref reader, "read model");
        var (id, name, properties) = NamedProperties(ref reader, "read model");
        return new(id, name, properties);
    }

    internal static SemanticProjection Projection(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "projection");
        var state = new ProjectionState(schemaVersion);
        ProjectionState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticProjectionTransition Transition(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "projection transition");
        var state = new TransitionState(schemaVersion);
        TransitionState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticAffectedInstance AffectedInstance(ref Utf8JsonReader reader, uint schemaVersion)
    {
        var state = new AffectedInstanceState(schemaVersion);
        AffectedInstanceState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticKeyedQuery Query(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "query");
        var state = new QueryState(schemaVersion);
        QueryState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticReadModelQueryArgument QueryArgument(ref Utf8JsonReader reader)
    {
        var state = new QueryArgumentState();
        QueryArgumentState.Fields.Read(ref reader, state);
        return state.Build();
    }

    sealed class SliceState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<SliceState> Fields = TableFor<SliceState>("slice")
            .Add("kind", (ref Utf8JsonReader reader, SliceState state, string property) => state.Kind = ParseSliceKind(String(ref reader, property)))
            .Add("events", (ref Utf8JsonReader reader, SliceState state, string property) => state.Events = Array(ref reader, (ref Utf8JsonReader item) => Event(ref item, state.SchemaVersion), property))
            .Add("commands", (ref Utf8JsonReader reader, SliceState state, string property) => state.Commands = Array(ref reader, (ref Utf8JsonReader item) => Command(ref item, state.SchemaVersion), property))
            .Add("readModels", (ref Utf8JsonReader reader, SliceState state, string property) => state.ReadModels = Array(ref reader, ReadModel, property))
            .Add("projections", (ref Utf8JsonReader reader, SliceState state, string property) => state.Projections = Array(ref reader, (ref Utf8JsonReader item) => Projection(ref item, state.SchemaVersion), property))
            .Add("reducers", (ref Utf8JsonReader reader, SliceState state, string property) => state.Reducers = Array(ref reader, (ref Utf8JsonReader item) => Reducer(ref item, state.SchemaVersion), property))
            .Add("queries", (ref Utf8JsonReader reader, SliceState state, string property) => state.Queries = Array(ref reader, (ref Utf8JsonReader item) => Query(ref item, state.SchemaVersion), property))
            .Add("specifications", (ref Utf8JsonReader reader, SliceState state, string property) => state.Specifications = Array(ref reader, (ref Utf8JsonReader item) => Specification(ref item, state.SchemaVersion), property))
            .Add("constraints", (ref Utf8JsonReader reader, SliceState state, string property) => state.Constraints = Array(ref reader, Constraint, property))
            .Add("reactions", 6, (ref Utf8JsonReader reader, SliceState state, string property) => state.Reactions = Array(ref reader, (ref Utf8JsonReader item) => Reaction(ref item, state.SchemaVersion), property))
            .Add("captures", 6, (ref Utf8JsonReader reader, SliceState state, string property) => state.Captures = Array(ref reader, (ref Utf8JsonReader item) => Capture(ref item, state.SchemaVersion), property))
            .Add("direction", LanguageVersion.V9.Major, (ref Utf8JsonReader reader, SliceState state, string property) => state.Direction = ParseDirection(String(ref reader, property)));

        public SemanticSliceKind? Kind { get; set; }

        public ImmutableArray<SemanticEventContract> Events { get; set; }

        public ImmutableArray<SemanticCommand> Commands { get; set; }

        public ImmutableArray<SemanticReadModel> ReadModels { get; set; }

        public ImmutableArray<SemanticProjection> Projections { get; set; }

        public ImmutableArray<SemanticReducer> Reducers { get; set; } = [];

        public ImmutableArray<SemanticKeyedQuery> Queries { get; set; }

        public ImmutableArray<SemanticSpecification> Specifications { get; set; }

        public ImmutableArray<SemanticConstraint> Constraints { get; set; } = [];

        public ImmutableArray<SemanticReaction> Reactions { get; set; } = [];

        public ImmutableArray<SemanticCapture> Captures { get; set; } = [];

        public SemanticTranslationDirection? Direction { get; set; }

        bool HasContracts => !Events.IsDefault && !Commands.IsDefault && !ReadModels.IsDefault;

        bool HasBehaviors => !Projections.IsDefault && !Queries.IsDefault && !Specifications.IsDefault;

        public SemanticSlice Build()
        {
            Required(HasIdentity && Kind is not null && HasContracts && HasBehaviors, "slice");
            return new(Id, Name!, Kind!.Value, Events, Commands, ReadModels, Projections, Queries, Specifications)
            {
                Constraints = Constraints,
                Reducers = Reducers,
                Reactions = Reactions,
                Captures = Captures,
                Direction = Direction
            };
        }
    }

    sealed class ReducerState(uint schemaVersion) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<ReducerState> Fields = new FieldTable<ReducerState>("reducer")
            .Add("name", (ref Utf8JsonReader reader, ReducerState state, string property) => state.Name = String(ref reader, property))
            .Add("readModel", (ref Utf8JsonReader reader, ReducerState state, string property) => state.ReadModel = Identifier(ref reader, property))
            .Add("event", LanguageVersion.V9.Major, (ref Utf8JsonReader reader, ReducerState state, string property) => state.ReadEventTarget(ref reader, property))
            .Add("key", (ref Utf8JsonReader reader, ReducerState state, string property) => state.Key = String(ref reader, property))
            .Add("initialState", (ref Utf8JsonReader reader, ReducerState state, string property) => state.ReadInitialState(ref reader, property))
            .Add("result", (ref Utf8JsonReader reader, ReducerState state, string property) => state.Result = String(ref reader, property))
            .Add("transitions", (ref Utf8JsonReader reader, ReducerState state, string property) => state.Transitions = Array(ref reader, ReducerTransition, property))
            .Add("from", 10, (ref Utf8JsonReader reader, ReducerState state, string property) => state.From = Nested(ref reader, ObserverFilter, property));

        public string? Name { get; set; }

        public SemanticId ReadModel { get; set; }

        public string? Key { get; set; }

        public string? Result { get; set; }

        public bool TargetsEvent { get; set; }

        public bool InitialStateRead { get; set; }

        public SemanticObserverFilter? From { get; set; }

        public ImmutableArray<SemanticReducerTransition> Transitions { get; set; }

        public void ReadEventTarget(ref Utf8JsonReader reader, string property)
        {
            ReadModel = Identifier(ref reader, property);
            TargetsEvent = true;
        }

        public void ReadInitialState(ref Utf8JsonReader reader, string property)
        {
            RequiredToken(ref reader, JsonTokenType.Null, property);
            InitialStateRead = true;
        }

        public SemanticReducer Build()
        {
            Required(Name is not null && ReadModel.IsSet && Key == "eventSourceId" && InitialStateRead && Result == "stateOrDelete" && !Transitions.IsDefault, "reducer");
            return new(Name!, ReadModel, Transitions) { From = From, Target = TargetsEvent ? SemanticProjectionTargetKind.Event : SemanticProjectionTargetKind.ReadModel };
        }
    }

    sealed class ReducerTransitionState() : ReadState(0)
    {
        public static readonly FieldTable<ReducerTransitionState> Fields = new FieldTable<ReducerTransitionState>("reducer transition")
            .Add("eventContract", (ref Utf8JsonReader reader, ReducerTransitionState state, string property) => state.EventContract = Identifier(ref reader, property))
            .Add("requirementId", (ref Utf8JsonReader reader, ReducerTransitionState state, string property) => state.RequirementId = String(ref reader, property));

        public SemanticId EventContract { get; set; }

        public string? RequirementId { get; set; }

        public SemanticReducerTransition Build()
        {
            Required(EventContract.IsSet && RequirementId is not null, "reducer transition");
            return new(EventContract, RequirementId!);
        }
    }

    sealed class ProjectionState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<ProjectionState> Fields = TableFor<ProjectionState>("projection")
            .Add("readModel", (ref Utf8JsonReader reader, ProjectionState state, string property) => state.ReadModel = Identifier(ref reader, property))
            .Add("event", LanguageVersion.V9.Major, (ref Utf8JsonReader reader, ProjectionState state, string property) => state.ReadEventTarget(ref reader, property))
            .Add("transitions", (ref Utf8JsonReader reader, ProjectionState state, string property) => state.Transitions = Array(ref reader, (ref Utf8JsonReader item) => Transition(ref item, state.SchemaVersion), property))
            .Add("scope", (ref Utf8JsonReader reader, ProjectionState state, string property) => state.Scope = Nested(ref reader, ProjectionScope, property));

        public SemanticId ReadModel { get; set; }

        public bool TargetsEvent { get; set; }

        public ImmutableArray<SemanticProjectionTransition> Transitions { get; set; }

        public SemanticProjectionScope? Scope { get; set; }

        public void ReadEventTarget(ref Utf8JsonReader reader, string property)
        {
            ReadModel = Identifier(ref reader, property);
            TargetsEvent = true;
        }

        public SemanticProjection Build()
        {
            Required(HasIdentity && ReadModel.IsSet && !Transitions.IsDefault, "projection");
            return new(Id, Name!, ReadModel, Transitions) { Scope = Scope, Target = TargetsEvent ? SemanticProjectionTargetKind.Event : SemanticProjectionTargetKind.ReadModel };
        }
    }

    sealed class TransitionState(uint schemaVersion) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<TransitionState> Fields = new FieldTable<TransitionState>("projection transition")
            .Add("eventContract", (ref Utf8JsonReader reader, TransitionState state, string property) => state.EventContract = Identifier(ref reader, property))
            .Add("affectedInstance", (ref Utf8JsonReader reader, TransitionState state, string property) => state.Affected = Nested(ref reader, (ref Utf8JsonReader item) => AffectedInstance(ref item, state.SchemaVersion), property))
            .Add("mappings", (ref Utf8JsonReader reader, TransitionState state, string property) => state.Mappings = Array(ref reader, Mapping, property));

        public SemanticId EventContract { get; set; }

        public SemanticAffectedInstance? Affected { get; set; }

        public ImmutableArray<SemanticPropertyMapping> Mappings { get; set; }

        public SemanticProjectionTransition Build()
        {
            Required(EventContract.IsSet && Affected is not null && !Mappings.IsDefault, "projection transition");
            return new(EventContract, Affected!, Mappings);
        }
    }

    sealed class AffectedInstanceState(uint schemaVersion) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<AffectedInstanceState> Fields = new FieldTable<AffectedInstanceState>("affected instance")
            .Add("cardinality", (ref Utf8JsonReader reader, AffectedInstanceState state, string property) => state.Cardinality = ParseAffectedCardinality(String(ref reader, property)))
            .Add("key", (ref Utf8JsonReader reader, AffectedInstanceState state, string property) => state.Key = Nested(ref reader, Expression, property));

        public AffectedInstanceCardinality? Cardinality { get; set; }

        public SemanticExpression? Key { get; set; }

        public SemanticAffectedInstance Build()
        {
            if (SchemaVersion is >= 1 and <= 3 && Cardinality is null)
            {
                throw new InvalidSemanticContract($"An ESM v{SchemaVersion} affected instance requires 'cardinality'.");
            }

            Required(Key is not null, "affected instance");
            return new(Cardinality ?? AffectedInstanceCardinality.One, Key!);
        }
    }

    sealed class QueryState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<QueryState> Fields = TableFor<QueryState>("query")
            .Add("readModel", (ref Utf8JsonReader reader, QueryState state, string property) => state.ReadModel = Identifier(ref reader, property))
            .Add("argument", (ref Utf8JsonReader reader, QueryState state, string property) => state.Argument = Nested(ref reader, QueryArgument, property))
            .Add("keyProperty", (ref Utf8JsonReader reader, QueryState state, string property) => state.KeyProperty = Identifier(ref reader, property))
            .Add("keyParts", CompositeReadModelKeyVersion.Major, (ref Utf8JsonReader reader, QueryState state, string property) => state.KeyParts = Array(ref reader, QueryKeyPart, property))
            .Add("filters", LanguageVersion.V14.Major, (ref Utf8JsonReader reader, QueryState state, string property) => state.Filters = Array(ref reader, QueryFilter, property))
            .Add("cardinality", (ref Utf8JsonReader reader, QueryState state, string property) => state.Cardinality = ParseQueryCardinality(String(ref reader, property)))
            .Add("delivery", (ref Utf8JsonReader reader, QueryState state, string property) => state.Delivery = ParseQueryDelivery(String(ref reader, property)))
            .Add("authorization", (ref Utf8JsonReader reader, QueryState state, string property) => state.Authorization = Nested(ref reader, SemanticModelRead.Authorization, property));

        public SemanticId ReadModel { get; set; }

        public SemanticReadModelQueryArgument? Argument { get; set; }

        public SemanticId? KeyProperty { get; set; }

        public ImmutableArray<SemanticQueryFilter> Filters { get; set; } = [];

        public ImmutableArray<SemanticQueryKeyPart> KeyParts { get; set; } = [];

        public SemanticQueryCardinality? Cardinality { get; set; }

        public SemanticQueryDelivery? Delivery { get; set; }

        public SemanticAuthorization? Authorization { get; set; }

        public SemanticKeyedQuery Build()
        {
            Required(HasIdentity && ReadModel.IsSet && Cardinality is not null && Delivery is not null, "query");
            Required((Argument is null) == (KeyProperty is null), "query");
            return new(Id, Name!, Argument, ReadModel, KeyProperty, Cardinality!.Value, Delivery!.Value) { Authorization = Authorization, Filters = Filters, KeyParts = KeyParts };
        }
    }

    sealed class QueryArgumentState() : IdentifiedReadState(0)
    {
        public static readonly FieldTable<QueryArgumentState> Fields = TableFor<QueryArgumentState>("query argument")
            .Add("type", (ref Utf8JsonReader reader, QueryArgumentState state, string property) => state.Type = Nested(ref reader, TypeReference, property));

        public SemanticTypeReference? Type { get; set; }

        public SemanticReadModelQueryArgument Build()
        {
            Required(HasIdentity && Type is not null, "query argument");
            return new(Id, Name!, Type!);
        }
    }
}
#pragma warning restore IDE0350
