// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticDecisionRead DecisionRead(ref Utf8JsonReader reader)
    {
        Object(ref reader, "decision read");
        var seen = NewSeen();
        string? name = null;
        SemanticId model = default;
        SemanticId key = default;
        string? presence = null;
        while (NextProperty(ref reader, seen, "decision read") is { } property)
        {
            switch (property)
            {
                case "name": name = String(ref reader, property); break;
                case "readModel": model = Identifier(ref reader, property); break;
                case "key": key = Identifier(ref reader, property); break;
                case "presence": presence = String(ref reader, property); break;
                default: throw Unknown(property, "decision read");
            }
        }
        Required(name is not null && model.IsSet && key.IsSet && presence == "required", "decision read");
        return new(name!, model, key, SemanticDecisionReadPresence.Required);
    }
}
