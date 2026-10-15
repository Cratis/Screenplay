// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticCommand Command(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "command");
        var state = new CommandState(schemaVersion);
        CommandState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticCodeValidation CodeValidation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "code validation");
        var state = new CodeValidationState();
        CodeValidationState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticProducedEvent ProducedEvent(ref Utf8JsonReader reader, uint schemaVersion, bool reaction)
    {
        Object(ref reader, "produced event");
        var state = new ProducedEventState(schemaVersion, reaction);
        ProducedEventState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticPropertyMapping Mapping(ref Utf8JsonReader reader)
    {
        Object(ref reader, "mapping");
        var state = new MappingState();
        MappingState.Fields.Read(ref reader, state);
        return state.Build();
    }

    sealed class CommandState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<CommandState> Fields = TableFor<CommandState>("command")
            .Add("properties", (ref Utf8JsonReader reader, CommandState state, string property) => state.Properties = Array(ref reader, (ref Utf8JsonReader item) => Property(ref item, state.SchemaVersion), property))
            .Add("validations", (ref Utf8JsonReader reader, CommandState state, string property) => state.Validations = Array(ref reader, Validation, property))
            .Add("codeValidations", (ref Utf8JsonReader reader, CommandState state, string property) => state.CodeValidations = Array(ref reader, CodeValidation, property))
            .Add("produces", (ref Utf8JsonReader reader, CommandState state, string property) => state.Produces = Array(ref reader, (ref Utf8JsonReader item) => ProducedEvent(ref item, state.SchemaVersion, false), property))
            .Add("requirements", (ref Utf8JsonReader reader, CommandState state, string property) => state.Requirements = Array(ref reader, Requirement, property))
            .Add("authorization", (ref Utf8JsonReader reader, CommandState state, string property) => state.Authorization = Nested(ref reader, SemanticModelRead.Authorization, property))
            .Add("destination", (ref Utf8JsonReader reader, CommandState state, string property) => state.Destination = Nested(ref reader, StateChangeDestination, property))
            .Add("response", 7, (ref Utf8JsonReader reader, CommandState state, string property) => state.Response = Nested(ref reader, SemanticModelRead.Response, property))
            .Add("route", LanguageVersion.V8.Major, (ref Utf8JsonReader reader, CommandState state, string property) => state.Route = Nested(ref reader, CommandRoute, property))
            .Add("reads", LanguageVersion.V24.Major, (ref Utf8JsonReader reader, CommandState state, string property) => state.Reads = Array(ref reader, DecisionRead, property));

        public ImmutableArray<SemanticProperty> Properties { get; set; }

        public ImmutableArray<SemanticValidationRule> Validations { get; set; }

        public ImmutableArray<SemanticCodeValidation> CodeValidations { get; set; } = [];

        public ImmutableArray<SemanticProducedEvent> Produces { get; set; }

        public ImmutableArray<SemanticRequirement> Requirements { get; set; } = [];

        public SemanticStateChangeDestination? Destination { get; set; }

        public SemanticAuthorization? Authorization { get; set; }

        public SemanticCommandResponse? Response { get; set; }

        public ImmutableArray<SemanticDecisionRead> Reads { get; set; } = [];

        public SemanticCommandRoute? Route { get; set; }

        public SemanticCommand Build()
        {
            Required(HasIdentity && !Properties.IsDefault && !Validations.IsDefault && !Produces.IsDefault, "command");
            return new(Id, Name!, Properties, Validations, Produces)
            {
                CodeValidations = CodeValidations,
                Requirements = Requirements,
                Destination = Destination,
                Authorization = Authorization,
                Response = Response,
                Reads = Reads,
                Route = Route
            };
        }
    }

    sealed class CodeValidationState() : ReadState(0)
    {
        public static readonly FieldTable<CodeValidationState> Fields = new FieldTable<CodeValidationState>("code validation")
            .Add("requirementId", (ref Utf8JsonReader reader, CodeValidationState state, string property) => state.RequirementId = String(ref reader, property));

        public string? RequirementId { get; set; }

        public SemanticCodeValidation Build()
        {
            Required(RequirementId is not null, "code validation");
            return new(RequirementId!);
        }
    }

    sealed class ProducedEventState(uint schemaVersion, bool reaction) : ReadState(schemaVersion)
    {
        public static readonly FieldTable<ProducedEventState> Fields = new FieldTable<ProducedEventState>("produced event")
            .Add("eventContract", (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.EventContract = Identifier(ref reader, property))
            .Add("condition", (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.ReadCondition(ref reader, property))
            .Add("destination", (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.ReadDestination(ref reader, property))
            .Add("mappings", (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.Mappings = Array(ref reader, Mapping, property))
            .Add("when", (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.When = Nested(ref reader, Condition, property))
            .Add("tags", (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.Tags = StringArray(ref reader, property))
            .Add("destinationType", 6, (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.DestinationType = Nested(ref reader, TypeReference, property), state => state.Reaction)
            .Add("route", 10, (ref Utf8JsonReader reader, ProducedEventState state, string property) => state.Route = Nested(ref reader, CommandRoute, property), state => !state.Reaction);

        public bool Reaction { get; } = reaction;

        public SemanticId EventContract { get; set; }

        public SemanticExpression? ConditionExpression { get; set; }

        public bool ConditionRead { get; set; }

        public SemanticExpression? DestinationExpression { get; set; }

        public bool DestinationRead { get; set; }

        public ImmutableArray<SemanticPropertyMapping> Mappings { get; set; }

        public SemanticCondition? When { get; set; }

        public ImmutableArray<string> Tags { get; set; } = [];

        public SemanticTypeReference? DestinationType { get; set; }

        public SemanticCommandRoute? Route { get; set; }

        public void ReadCondition(ref Utf8JsonReader reader, string property)
        {
            ConditionRead = true;
            ConditionExpression = NullableExpression(ref reader, property);
        }

        public void ReadDestination(ref Utf8JsonReader reader, string property)
        {
            DestinationRead = true;
            DestinationExpression = NullableExpression(ref reader, property);
        }

        public SemanticProducedEvent Build()
        {
            Required(EventContract.IsSet && ConditionRead && DestinationRead && !Mappings.IsDefault, "produced event");
            return new(EventContract, ConditionExpression, DestinationExpression, Mappings) { When = When, Tags = Tags, DestinationType = DestinationType, Route = Route };
        }
    }

    sealed class MappingState() : ReadState(0)
    {
        public static readonly FieldTable<MappingState> Fields = new FieldTable<MappingState>("mapping")
            .Add("targetProperty", (ref Utf8JsonReader reader, MappingState state, string property) => state.Target = Identifier(ref reader, property))
            .Add("source", (ref Utf8JsonReader reader, MappingState state, string property) => state.Source = Nested(ref reader, Expression, property));

        public SemanticId Target { get; set; }

        public SemanticExpression? Source { get; set; }

        public SemanticPropertyMapping Build()
        {
            Required(Target.IsSet && Source is not null, "mapping");
            return new(Target, Source!);
        }
    }
}
#pragma warning restore IDE0350
