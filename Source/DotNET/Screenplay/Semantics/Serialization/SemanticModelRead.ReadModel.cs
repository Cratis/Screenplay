// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    sealed class ReadModelState() : IdentifiedReadState(0)
    {
        public static readonly FieldTable<ReadModelState> Fields = TableFor<ReadModelState>("read model")
            .Add("properties", (ref reader, state, property) => state.Properties = Array(ref reader, Property, property))
            .Add("propertyOrder", (ref reader, state, property) => state.PropertyOrder = Array(ref reader, PropertyId, property));

        public ImmutableArray<SemanticProperty> Properties { get; set; }

        public ImmutableArray<SemanticId> PropertyOrder { get; set; } = [];

        static SemanticId PropertyId(ref Utf8JsonReader reader) => SemanticId.Parse(StringToken(ref reader, "property order identity", "a string"));
    }
}
