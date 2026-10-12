// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

#pragma warning disable IDE0350 // Explicit ref reader parameters keep version-aware parsing delegates clear.
internal static partial class SemanticModelRead
{
    static SemanticQueryFilter QueryFilter(ref Utf8JsonReader reader)
    {
        Object(ref reader, "query filter");
        var state = new QueryFilterState();
        QueryFilterState.Fields.Read(ref reader, state);
        Required(state.HasIdentity && state.Type is not null && state.Property.IsSet, "query filter");
        return new(state.Id, state.Name!, state.Type!, state.Property);
    }

    static SemanticQueryFilterValue QueryFilterValue(ref Utf8JsonReader reader)
    {
        Object(ref reader, "query filter value");
        var state = new QueryFilterValueState();
        QueryFilterValueState.Fields.Read(ref reader, state);
        Required(state.Filter.IsSet && state.Value is not null, "query filter value");
        return new(state.Filter, state.Value!);
    }

    sealed class QueryFilterState() : IdentifiedReadState(0)
    {
        public static readonly FieldTable<QueryFilterState> Fields = TableFor<QueryFilterState>("query filter")
            .Add("type", (ref Utf8JsonReader reader, QueryFilterState state, string property) => state.Type = Nested(ref reader, TypeReference, property))
            .Add("property", (ref Utf8JsonReader reader, QueryFilterState state, string property) => state.Property = Identifier(ref reader, property));

        public SemanticTypeReference? Type { get; set; }

        public SemanticId Property { get; set; }
    }

    sealed class QueryFilterValueState() : ReadState(0)
    {
        public static readonly FieldTable<QueryFilterValueState> Fields = new FieldTable<QueryFilterValueState>("query filter value")
            .Add("filter", (ref Utf8JsonReader reader, QueryFilterValueState state, string property) => state.Filter = Identifier(ref reader, property))
            .Add("value", (ref Utf8JsonReader reader, QueryFilterValueState state, string property) => state.Value = Nested(ref reader, SemanticModelRead.Value, property));

        public SemanticId Filter { get; set; }

        public SemanticValue? Value { get; set; }
    }
}
#pragma warning restore IDE0350
