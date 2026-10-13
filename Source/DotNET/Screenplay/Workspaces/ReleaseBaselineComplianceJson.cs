// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using System.Text.Json.Serialization;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// Reads optional compliance markers without making the ESM record's constructor parameters required JSON members.
/// </summary>
sealed class ReleaseBaselineComplianceJson : JsonConverter<SemanticConceptCompliance>
{
    public override SemanticConceptCompliance Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options)
    {
        var marker = JsonSerializer.Deserialize<Marker>(ref reader, options) ?? throw new InvalidReleaseBaseline("Empty compliance marker.");

        return new(marker.Personal, marker.Secret);
    }

    public override void Write(Utf8JsonWriter writer, SemanticConceptCompliance value, JsonSerializerOptions options) =>
        JsonSerializer.Serialize(writer, new Marker(value.Personal, value.Secret), options);

    sealed record Marker(SemanticPersonalData? Personal = null, SemanticSecret? Secret = null);
}
