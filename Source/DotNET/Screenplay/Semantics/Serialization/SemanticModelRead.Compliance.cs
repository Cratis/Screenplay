// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep parsing delegates clear.
internal static partial class SemanticModelRead
{
    static SemanticConceptCompliance Compliance(ref Utf8JsonReader reader)
    {
        Object(ref reader, "compliance");
        var state = new ComplianceState();
        ComplianceState.Fields.Read(ref reader, state);
        Required(state.Personal is not null || state.Secret is not null, "compliance");

        return new(state.Personal, state.Secret);
    }

    static SemanticPersonalData Personal(ref Utf8JsonReader reader)
    {
        Object(ref reader, "personal");
        var state = new PersonalState();
        PersonalState.Fields.Read(ref reader, state);

        return new(state.Special, state.Criminal);
    }

    static SemanticSecret Secret(ref Utf8JsonReader reader)
    {
        Object(ref reader, "secret");
        var state = new SecretState();
        SecretState.Fields.Read(ref reader, state);

        return new(state.Scope);
    }

    sealed class ComplianceState() : ReadState(0)
    {
        public static readonly FieldTable<ComplianceState> Fields = new FieldTable<ComplianceState>("compliance")
            .Add("personal", (ref Utf8JsonReader reader, ComplianceState state, string property) => state.Personal = Nested(ref reader, SemanticModelRead.Personal, property))
            .Add("secret", (ref Utf8JsonReader reader, ComplianceState state, string property) => state.Secret = Nested(ref reader, SemanticModelRead.Secret, property));

        public SemanticPersonalData? Personal { get; set; }

        public SemanticSecret? Secret { get; set; }
    }

    sealed class PersonalState() : ReadState(0)
    {
        public static readonly FieldTable<PersonalState> Fields = new FieldTable<PersonalState>("personal")
            .Add("special", (ref Utf8JsonReader reader, PersonalState state, string property) => state.Special = String(ref reader, property))
            .Add("criminal", (ref Utf8JsonReader reader, PersonalState state, string property) => state.Criminal = OnlyTrue(ref reader, property, "personal"));

        public string? Special { get; set; }

        public bool Criminal { get; set; }
    }

    sealed class SecretState() : ReadState(0)
    {
        public static readonly FieldTable<SecretState> Fields = new FieldTable<SecretState>("secret")
            .Add("scope", (ref Utf8JsonReader reader, SecretState state, string property) => state.Scope = String(ref reader, property));

        public string? Scope { get; set; }
    }
}
