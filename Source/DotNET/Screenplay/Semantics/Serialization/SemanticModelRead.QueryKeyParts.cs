// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    static SemanticQueryKeyPart QueryKeyPart(ref Utf8JsonReader reader)
    {
        var state = new QueryKeyPartState();
        QueryKeyPartState.Fields.Read(ref reader, state);

        return state.Build();
    }

    sealed class QueryKeyPartState() : IdentifiedReadState(CompositeReadModelKeyVersion.Major)
    {
        public static readonly FieldTable<QueryKeyPartState> Fields = TableFor<QueryKeyPartState>("query key part")
            .Add("type", ReadType)
            .Add("keyProperty", ReadKeyProperty);

        public SemanticTypeReference? Type { get; set; }

        public SemanticId KeyProperty { get; set; }

        public SemanticQueryKeyPart Build()
        {
            Required(HasIdentity && Type is not null && KeyProperty.IsSet, "query key part");

            return new(Id, Name!, Type!, KeyProperty);
        }

        static void ReadType(ref Utf8JsonReader reader, QueryKeyPartState state, string property) => state.Type = Nested(ref reader, TypeReference, property);

        static void ReadKeyProperty(ref Utf8JsonReader reader, QueryKeyPartState state, string property) => state.KeyProperty = Identifier(ref reader, property);
    }
}
