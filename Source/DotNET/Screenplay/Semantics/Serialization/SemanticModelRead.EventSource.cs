// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticStateChangeDestination StateChangeDestination(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticTypeReference? type = null;
        SemanticExpression? value = null;
        var valueRead = false;
        while (NextProperty(ref reader, seen, "state change destination") is { } property)
        {
            switch (property)
            {
                case "type": Object(ref reader, property); type = TypeReference(ref reader); break;
                case "value": valueRead = true; value = NullableExpression(ref reader, property); break;
                default: throw Unknown(property, "state change destination");
            }
        }

        Required(type is not null && valueRead, "state change destination");
        return new(type!, value);
    }

    internal static SemanticEventSourceIdentity EventSource(ref Utf8JsonReader reader)
    {
        var seen = NewSeen();
        SemanticTypeReference? type = null;
        SemanticValue? value = null;
        while (NextProperty(ref reader, seen, "event source") is { } property)
        {
            switch (property)
            {
                case "type": Object(ref reader, property); type = TypeReference(ref reader); break;
                case "value": Object(ref reader, property); value = Value(ref reader); break;
                default: throw Unknown(property, "event source");
            }
        }

        Required(type is not null && value is not null, "event source");
        return new(type!, value!);
    }
}
