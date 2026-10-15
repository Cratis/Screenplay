// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    internal delegate T ValueReader<T>(ref Utf8JsonReader reader);

    internal static SemanticApplication Application(ref Utf8JsonReader reader, uint schemaVersion)
    {
        var state = new ApplicationState(schemaVersion);
        ApplicationState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticConcept Concept(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "concept");
        var state = new ConceptState(schemaVersion);
        ConceptState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticCompositeType CompositeType(ref Utf8JsonReader reader)
    {
        Object(ref reader, "composite type");
        var (id, name, properties) = NamedProperties(ref reader, "composite type");
        return new(id, name, properties);
    }

    internal static SemanticModule Module(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "module");
        var state = new ModuleState(schemaVersion);
        ModuleState.Fields.Read(ref reader, state);
        return state.Build();
    }

    internal static SemanticFeature Feature(ref Utf8JsonReader reader, uint schemaVersion)
    {
        Object(ref reader, "feature");
        var state = new FeatureState(schemaVersion);
        FeatureState.Fields.Read(ref reader, state);
        return state.Build();
    }

    static int EnumMemberNumber(ref Utf8JsonReader reader)
    {
        if (reader.TokenType != JsonTokenType.Number || !reader.TryGetInt32(out var value))
        {
            throw Malformed("numbers", "a 32-bit integer");
        }

        return value;
    }

    sealed class ApplicationState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<ApplicationState> Fields = TableFor<ApplicationState>("application")
            .Add("concepts", (ref Utf8JsonReader reader, ApplicationState state, string property) => state.Concepts = Array(ref reader, (ref Utf8JsonReader item) => Concept(ref item, state.SchemaVersion), property))
            .Add("types", (ref Utf8JsonReader reader, ApplicationState state, string property) => state.Types = Array(ref reader, CompositeType, property))
            .Add("modules", (ref Utf8JsonReader reader, ApplicationState state, string property) => state.Modules = Array(ref reader, (ref Utf8JsonReader item) => Module(ref item, state.SchemaVersion), property))
            .Add("policies", (ref Utf8JsonReader reader, ApplicationState state, string property) => state.Policies = Array(ref reader, Policy, property))
            .Add("triggers", 6, (ref Utf8JsonReader reader, ApplicationState state, string property) => state.Triggers = Array(ref reader, Trigger, property))
            .Add("eventSources", LanguageVersion.V8.Major, (ref Utf8JsonReader reader, ApplicationState state, string property) => state.ReadEventSources(ref reader, property))
            .Add("reactionProductionMode", LanguageVersion.V22.Major, (ref Utf8JsonReader reader, ApplicationState state, string property) => state.ReactionProductionMode = String(ref reader, property) == "atomic" ? SemanticReactionProductionMode.Atomic : throw new InvalidSemanticContract("Unknown reaction production mode."));

        public ImmutableArray<SemanticConcept> Concepts { get; set; }

        public ImmutableArray<SemanticCompositeType> Types { get; set; }

        public ImmutableArray<SemanticModule> Modules { get; set; }

        public ImmutableArray<SemanticPolicy> Policies { get; set; } = [];

        public ImmutableArray<SemanticApplicationTrigger> Triggers { get; set; } = [];

        public ImmutableArray<SemanticEventSource> EventSources { get; set; } = [];

        public SemanticReactionProductionMode? ReactionProductionMode { get; set; }

        bool HasCollections => !Concepts.IsDefault && !Types.IsDefault && !Modules.IsDefault;

        public void ReadEventSources(ref Utf8JsonReader reader, string property)
        {
            EventSources = Array(ref reader, EventSourceDeclaration, property);
            Required(!EventSources.IsEmpty, "event sources");
        }

        public SemanticApplication Build()
        {
            Required(HasIdentity && HasCollections, "application");
            return new(Id, Name!, Concepts, Types, Modules) { Policies = Policies, Triggers = Triggers, EventSources = EventSources, ReactionProductionMode = ReactionProductionMode };
        }
    }

    sealed class ConceptState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<ConceptState> Fields = TableFor<ConceptState>("concept")
            .Add("primitive", (ref Utf8JsonReader reader, ConceptState state, string property) => state.Primitive = ParsePrimitive(String(ref reader, property)))
            .Add("values", (ref Utf8JsonReader reader, ConceptState state, string property) => state.Values = StringArray(ref reader, property))
            .Add("validations", (ref Utf8JsonReader reader, ConceptState state, string property) => state.Validations = Array(ref reader, Validation, property))
            .Add("numbers", LanguageVersion.V12.Major, (ref Utf8JsonReader reader, ConceptState state, string property) => state.Numbers = Array(ref reader, EnumMemberNumber, property))
            .Add("compliance", LanguageVersion.V13.Major, (ref Utf8JsonReader reader, ConceptState state, string property) => state.Compliance = Nested(ref reader, SemanticModelRead.Compliance, property))
            .Add("generation", LanguageVersion.V15.Major, (ref Utf8JsonReader reader, ConceptState state, string property) => state.Generation = Nested(ref reader, SemanticModelRead.Generation, property));

        public SemanticPrimitiveType? Primitive { get; set; }

        public ImmutableArray<string> Values { get; set; }

        public ImmutableArray<SemanticValidationRule> Validations { get; set; }

        public ImmutableArray<int>? Numbers { get; set; }

        public SemanticConceptCompliance? Compliance { get; set; }

        public SemanticGeneration? Generation { get; set; }

        public SemanticConcept Build()
        {
            Required(HasIdentity && Primitive is not null && !Values.IsDefault && !Validations.IsDefault, "concept");
            return new(Id, Name!, Primitive!.Value, Values, Validations) { Numbers = Numbers, Compliance = Compliance, Generation = Generation };
        }
    }

    sealed class ModuleState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<ModuleState> Fields = TableFor<ModuleState>("module")
            .Add("features", (ref Utf8JsonReader reader, ModuleState state, string property) => state.Features = Array(ref reader, (ref Utf8JsonReader item) => Feature(ref item, state.SchemaVersion), property));

        public ImmutableArray<SemanticFeature> Features { get; set; }

        public SemanticModule Build()
        {
            Required(HasIdentity && !Features.IsDefault, "module");
            return new(Id, Name!, Features);
        }
    }

    sealed class FeatureState(uint schemaVersion) : IdentifiedReadState(schemaVersion)
    {
        public static readonly FieldTable<FeatureState> Fields = TableFor<FeatureState>("feature")
            .Add("features", (ref Utf8JsonReader reader, FeatureState state, string property) => state.Features = Array(ref reader, (ref Utf8JsonReader item) => Feature(ref item, state.SchemaVersion), property))
            .Add("slices", (ref Utf8JsonReader reader, FeatureState state, string property) => state.Slices = Array(ref reader, (ref Utf8JsonReader item) => Slice(ref item, state.SchemaVersion), property));

        public ImmutableArray<SemanticFeature> Features { get; set; }

        public ImmutableArray<SemanticSlice> Slices { get; set; }

        public SemanticFeature Build()
        {
            Required(HasIdentity && !Features.IsDefault && !Slices.IsDefault, "feature");
            return new(Id, Name!, Features, Slices);
        }
    }
}
#pragma warning restore IDE0350
