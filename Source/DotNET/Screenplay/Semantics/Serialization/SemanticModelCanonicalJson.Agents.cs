// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

public static partial class SemanticModelCanonicalJson
{
    static void WriteAgents(Utf8JsonWriter writer, SemanticAgents? agents)
    {
        if (agents is null) return;
        writer.WriteStartObject("agents");
        writer.WriteString("tokenKind", "personal");
        WriteArray(writer, "operations", agents.Operations.OrderBy(operation => operation.Kind).ThenBy(operation => operation.Operation.ToString(), StringComparer.Ordinal), WriteAgentOperation);
        writer.WriteEndObject();
    }

    static void WriteAgentOperation(Utf8JsonWriter writer, SemanticAgentOperation operation)
    {
        writer.WriteStartObject();
        writer.WriteString("kind", operation.Kind == SemanticAgentOperationKind.Query ? "query" : "command");
        writer.WriteString("operation", operation.Operation.ToString());
        writer.WriteString("scope", operation.Scope == SemanticAgentScope.Read ? "read" : "write");
        CanonicalJson.WriteString(writer, "description", operation.Description);
        writer.WriteEndObject();
    }
}
