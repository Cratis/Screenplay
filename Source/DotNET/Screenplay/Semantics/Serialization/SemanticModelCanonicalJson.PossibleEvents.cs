// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteCommandHandler(Utf8JsonWriter writer, SemanticCommandHandler handler, SemanticVersion version)
    {
        var v2 = handler.ResultVersion == 2 && !handler.PossibleEvents.IsDefaultOrEmpty;
        if (RequiresUnpublishedHandlerContract(handler, version))
            throw new InvalidSemanticContract("Handler possible events and result contract v2 require ESM v35 or later.");
        writer.WriteStartObject();
        writer.WriteString("requirementId", handler.RequirementId);
        writer.WriteNumber("resultVersion", handler.ResultVersion);
        if (v2) WriteArray(writer, "possibleEvents", handler.PossibleEvents, (json, entry) => WriteProducedEvent(json, entry, new ProducedEventWriteContext(version, false, true)));
        writer.WriteEndObject();
    }

    static bool RequiresUnpublishedHandlerContract(SemanticCommandHandler handler, SemanticVersion version)
    {
        if (handler.ResultVersion == 1 && handler.PossibleEvents.IsDefaultOrEmpty) return false;
        return handler.ResultVersion != 2 || handler.PossibleEvents.IsDefaultOrEmpty || !version.IsAtLeast(SemanticVersion.V35);
    }

    sealed record ProducedEventWriteContext(SemanticVersion Version, bool Reaction, bool PossibleEvent);
}
