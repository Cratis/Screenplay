// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using System.Text.Json;

namespace Cratis.Screenplay.Semantics.Serialization;

internal static partial class SemanticModelRead
{
    internal static SemanticAgents AgentExposure(ref Utf8JsonReader reader)
    {
        Object(ref reader, "agents");
        var seen = NewSeen();
        string? tokenKind = null;
        ImmutableArray<SemanticAgentOperation> operations = default;
        while (NextProperty(ref reader, seen, "agents") is { } property)
        {
            switch (property)
            {
                case "tokenKind": tokenKind = String(ref reader, property); break;
                case "operations": operations = Array(ref reader, AgentOperation, property); break;
                default: throw Unknown(property, "agents");
            }
        }

        Required(tokenKind == "personal" && !operations.IsDefault, "agents with personal tokens and explicit operations");
        return new(SemanticAgentTokenKind.Personal, operations);
    }

    static SemanticAgentOperation AgentOperation(ref Utf8JsonReader reader)
    {
        Object(ref reader, "agent operation");
        var seen = NewSeen();
        string? kind = null;
        SemanticId operation = default;
        string? scope = null;
        string? description = null;
        while (NextProperty(ref reader, seen, "agent operation") is { } property)
        {
            switch (property)
            {
                case "kind": kind = String(ref reader, property); break;
                case "operation": operation = SemanticId.Parse(String(ref reader, property)); break;
                case "scope": scope = String(ref reader, property); break;
                case "description": description = String(ref reader, property); break;
                default: throw Unknown(property, "agent operation");
            }
        }

        Required(operation.IsSet && !string.IsNullOrWhiteSpace(description), "agent operation");
        return new(AgentKind(kind), operation, AgentScope(scope), description!);
    }

    static SemanticAgentOperationKind AgentKind(string? kind) => kind switch
    {
        "query" => SemanticAgentOperationKind.Query,
        "command" => SemanticAgentOperationKind.Command,
        _ => throw new InvalidSemanticContract("An agent operation requires kind 'query' or 'command'.")
    };

    static SemanticAgentScope AgentScope(string? scope) => scope switch
    {
        "read" => SemanticAgentScope.Read,
        "write" => SemanticAgentScope.Write,
        _ => throw new InvalidSemanticContract("An agent operation requires scope 'read' or 'write'.")
    };
}
