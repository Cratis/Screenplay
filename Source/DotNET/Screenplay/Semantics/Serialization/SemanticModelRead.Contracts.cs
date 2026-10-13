// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal static SemanticProperty Property(ref Utf8JsonReader reader) => Property(ref reader, 0);

    internal static SemanticProperty Property(ref Utf8JsonReader reader, uint commandSchemaVersion)
    {
        Object(ref reader, "property");
        var state = new PropertyState(commandSchemaVersion);
        PropertyState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticTypeReference TypeReference(ref Utf8JsonReader reader)
    {
        var state = new TypeReferenceState();
        TypeReferenceState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticValidationRule Validation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "validation");
        var state = new ValidationState();
        ValidationState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticEventContract Event(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "event contract");
        var state = new EventContractState(schemaVersion);
        EventContractState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticEventRevision EventRevision(ref Utf8JsonReader reader)
    {
        Object(ref reader, "event revision");
        var state = new EventRevisionState();
        EventRevisionState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static EventContractRevision? NullableUInt32Revision(ref Utf8JsonReader reader, string property)
    {
        RequiredRead(ref reader, property);
        if (reader.TokenType == JsonTokenType.Null) return null;
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetUInt32(out var value))
        {
            throw Malformed(property, "an unsigned 32-bit integer or null");
        }

        return new(value);
    }

    internal static (SemanticId Id, string Name, ImmutableArray<SemanticProperty> Properties) NamedProperties(
        ref Utf8JsonReader reader,
        string description)
    {
        var state = new NamedPropertiesState();
        NamedPropertiesState.Fields.Read(ref reader, state, description);
        Required(state.HasIdentity && !state.Properties.IsDefault, description);
        return (state.Id, state.Name!, state.Properties);
    }

    sealed class NamedPropertiesState() : IdentifiedReadState(0)
    {
        public static readonly FieldTable<NamedPropertiesState> Fields = TableFor<NamedPropertiesState>("named properties")
            .Add("properties", (ref Utf8JsonReader reader, NamedPropertiesState state, string property) => state.Properties = Array(ref reader, Property, property));

        public ImmutableArray<SemanticProperty> Properties { get; set; }
    }

    sealed class PropertyState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<PropertyState> Fields = TableFor<PropertyState>("property")
            .Add("type", (ref Utf8JsonReader reader, PropertyState state, string property) => state.Type = Nested(ref reader, TypeReference, property))
            .Add("identifier", (ref Utf8JsonReader reader, PropertyState state, string property) => state.IsIdentifier = Boolean(ref reader, property))
            .Add("generated", 7, (ref Utf8JsonReader reader, PropertyState state, string property) => state.Generated = OnlyTrue(ref reader, property, "property"));

        public SemanticTypeReference? Type { get; set; }

        public bool? IsIdentifier { get; set; }

        public bool Generated { get; set; }

        public SemanticProperty Build()
        {
            Required(HasIdentity && Type is not null && IsIdentifier is not null, "property");
            return new(Id, Name!, Type!, IsIdentifier!.Value) { IsGenerated = Generated };
        }
    }

    sealed class TypeReferenceState() : ReadState(0)
    {
        public static readonly FieldTable<TypeReferenceState> Fields = new FieldTable<TypeReferenceState>("type reference")
            .Add("kind", (ref Utf8JsonReader reader, TypeReferenceState state, string property) => state.Kind = ParseTypeReferenceKind(String(ref reader, property)))
            .Add("primitive", (ref Utf8JsonReader reader, TypeReferenceState state, string property) => state.Primitive = NullableString(ref reader, property) is { } text ? ParsePrimitive(text) : SemanticPrimitiveType.Unknown)
            .Add("target", (ref Utf8JsonReader reader, TypeReferenceState state, string property) => state.ReadTarget(ref reader, property))
            .Add("collection", (ref Utf8JsonReader reader, TypeReferenceState state, string property) => state.Collection = Boolean(ref reader, property))
            .Add("optional", (ref Utf8JsonReader reader, TypeReferenceState state, string property) => state.Optional = Boolean(ref reader, property));

        public SemanticTypeReferenceKind? Kind { get; set; }

        public SemanticPrimitiveType? Primitive { get; set; }

        public SemanticId Target { get; set; }

        public bool TargetRead { get; set; }

        public bool? Collection { get; set; }

        public bool? Optional { get; set; }

        public void ReadTarget(ref Utf8JsonReader reader, string property)
        {
            TargetRead = true;
            Target = NullableString(ref reader, property) is { } text ? SemanticId.Parse(text) : default;
        }

        public SemanticTypeReference Build()
        {
            Required(Kind is not null && Primitive is not null && TargetRead && Collection is not null && Optional is not null, "type reference");
            return new(Kind!.Value, Primitive!.Value, Target, Collection!.Value, Optional!.Value);
        }
    }

    sealed class ValidationState() : ReadState(0)
    {
        public static readonly FieldTable<ValidationState> Fields = new FieldTable<ValidationState>("validation")
            .Add("property", (ref Utf8JsonReader reader, ValidationState state, string property) => state.ReadProperty(ref reader, property))
            .Add("kind", (ref Utf8JsonReader reader, ValidationState state, string property) => state.Kind = ParseValidationKind(String(ref reader, property)))
            .Add("operand", (ref Utf8JsonReader reader, ValidationState state, string property) => state.ReadOperand(ref reader, property))
            .Add("message", (ref Utf8JsonReader reader, ValidationState state, string property) => state.ReadMessage(ref reader, property))
            .Add("severity", (ref Utf8JsonReader reader, ValidationState state, string property) => state.Severity = ParseSeverity(String(ref reader, property)))
            .Add("name", (ref Utf8JsonReader reader, ValidationState state, string property) => state.PredicateName = String(ref reader, property))
            .Add("requirementId", (ref Utf8JsonReader reader, ValidationState state, string property) => state.RequirementId = String(ref reader, property));

        public SemanticId PropertyId { get; set; }

        public bool PropertyRead { get; set; }

        public SemanticValidationRuleKind? Kind { get; set; }

        public SemanticValue? Operand { get; set; }

        public bool OperandRead { get; set; }

        public string? Message { get; set; }

        public bool MessageRead { get; set; }

        public SemanticValidationSeverity Severity { get; set; } = SemanticValidationSeverity.Error;

        public string? PredicateName { get; set; }

        public string? RequirementId { get; set; }

        public void ReadProperty(ref Utf8JsonReader reader, string property)
        {
            PropertyRead = true;
            PropertyId = NullableString(ref reader, property) is { } text ? SemanticId.Parse(text) : default;
        }

        public void ReadOperand(ref Utf8JsonReader reader, string property)
        {
            OperandRead = true;
            Operand = NullableValue(ref reader, property);
        }

        public void ReadMessage(ref Utf8JsonReader reader, string property)
        {
            MessageRead = true;
            Message = NullableString(ref reader, property);
        }

        public SemanticValidationRule Build()
        {
            Required(PropertyRead && Kind is not null && OperandRead && MessageRead, "validation");
            return new(PropertyId, Kind!.Value, Operand, Message) { Severity = Severity, Name = PredicateName, RequirementId = RequirementId };
        }
    }

    sealed class EventContractState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<EventContractState> Fields = TableFor<EventContractState>("event contract")
            .Add("storedName", LanguageVersion.V16.Major, (ref Utf8JsonReader reader, EventContractState state, string property) => state.StoredName = String(ref reader, property))
            .Add("contractId", (ref Utf8JsonReader reader, EventContractState state, string property) => state.ContractId = EventContractId.Parse(String(ref reader, property)))
            .Add("contractRevision", (ref Utf8JsonReader reader, EventContractState state, string property) => state.Revision = new(UInt32(ref reader, property)))
            .Add("properties", (ref Utf8JsonReader reader, EventContractState state, string property) => state.Properties = Array(ref reader, Property, property))
            .Add("tags", (ref Utf8JsonReader reader, EventContractState state, string property) => state.Tags = StringArray(ref reader, property))
            .Add("visibility", LanguageVersion.V9.Major, (ref Utf8JsonReader reader, EventContractState state, string property) => state.ReadVisibility(ref reader, property))
            .Add("origin", LanguageVersion.V9.Major, (ref Utf8JsonReader reader, EventContractState state, string property) => state.Origin = String(ref reader, property))
            .Add("predecessor", (ref Utf8JsonReader reader, EventContractState state, string property) => state.ReadPredecessor(ref reader, property))
            .Add("priorRevisions", (ref Utf8JsonReader reader, EventContractState state, string property) => state.ReadPriorRevisions(ref reader, property));

        public string? StoredName { get; set; }

        public EventContractId ContractId { get; set; }

        public EventContractRevision Revision { get; set; }

        public ImmutableArray<SemanticProperty> Properties { get; set; }

        public ImmutableArray<string> Tags { get; set; } = [];

        public EventContractRevision? Predecessor { get; set; }

        public ImmutableArray<SemanticEventRevision> PriorRevisions { get; set; } = [];

        public bool PredecessorRead { get; set; }

        public bool PriorRevisionsRead { get; set; }

        public SemanticEventVisibility Visibility { get; set; } = SemanticEventVisibility.Private;

        public string? Origin { get; set; }

        bool HasContract => ContractId.IsSet && Revision.IsValid && !Properties.IsDefault;

        bool HasConsistentRevisionChain => PredecessorRead == PriorRevisionsRead && (!PriorRevisionsRead || !PriorRevisions.IsEmpty);

        bool HasConsistentOrigin => Origin is null || Visibility == SemanticEventVisibility.Public;

        public void ReadVisibility(ref Utf8JsonReader reader, string property) =>
            Visibility = String(ref reader, property) == "public" ? SemanticEventVisibility.Public : throw new InvalidSemanticContract("An event visibility is written only as 'public'.");

        public void ReadPredecessor(ref Utf8JsonReader reader, string property)
        {
            PredecessorRead = true;
            Predecessor = new(UInt32(ref reader, property));
        }

        public void ReadPriorRevisions(ref Utf8JsonReader reader, string property)
        {
            PriorRevisionsRead = true;
            PriorRevisions = Array(ref reader, EventRevision, property);
        }

        public SemanticEventContract Build()
        {
            Required(HasIdentity && HasContract && HasConsistentRevisionChain && HasConsistentOrigin, "event contract");
            return new(Id, ContractId, Revision, Name!, Properties) { StoredName = StoredName, Tags = Tags, Predecessor = Predecessor, PriorRevisions = PriorRevisions, Visibility = Visibility, Origin = Origin };
        }
    }

    sealed class EventRevisionState() : ReadState(0)
    {
        public static readonly FieldTable<EventRevisionState> Fields = new FieldTable<EventRevisionState>("event revision")
            .Add("contractRevision", (ref Utf8JsonReader reader, EventRevisionState state, string property) => state.Revision = new(UInt32(ref reader, property)))
            .Add("predecessor", (ref Utf8JsonReader reader, EventRevisionState state, string property) => state.ReadPredecessor(ref reader, property))
            .Add("properties", (ref Utf8JsonReader reader, EventRevisionState state, string property) => state.Properties = Array(ref reader, Property, property))
            .Add("tags", (ref Utf8JsonReader reader, EventRevisionState state, string property) => state.Tags = StringArray(ref reader, property));

        public EventContractRevision Revision { get; set; }

        public EventContractRevision? Predecessor { get; set; }

        public bool PredecessorRead { get; set; }

        public ImmutableArray<SemanticProperty> Properties { get; set; }

        public ImmutableArray<string> Tags { get; set; } = [];

        public void ReadPredecessor(ref Utf8JsonReader reader, string property)
        {
            PredecessorRead = true;
            Predecessor = NullableUInt32Revision(ref reader, property);
        }

        public SemanticEventRevision Build()
        {
            Required(Revision.IsValid && PredecessorRead && !Properties.IsDefault, "event revision");
            return new(Revision, Predecessor, Properties) { Tags = Tags };
        }
    }
}
#pragma warning restore IDE0350
