// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Printing;

public sealed partial class ScreenplayPrinter
{
    static void WriteAgents(ScreenplayWriter writer, AgentsSyntax agents)
    {
        writer.Line("agents", agents);
        using (writer.Indent())
        {
            if (agents.Tokens is { } tokens)
            {
                var kind = tokens.Kind switch
                {
                    AgentTokenKind.Personal => "personal",
                    AgentTokenKind.OAuth => "oauth",
                    _ => throw new UnsupportedSyntaxForPrinting("agent tokens", tokens.Kind.ToString())
                };
                writer.Line($"tokens {kind}", tokens);
            }

            foreach (var operation in agents.Operations) WriteAgentOperation(writer, operation);
        }
    }

    static void WriteAgentOperation(ScreenplayWriter writer, AgentOperationSyntax operation)
    {
        var kind = operation.Kind switch
        {
            AgentOperationKind.Query => "query",
            AgentOperationKind.Command => "command",
            _ => throw new UnsupportedSyntaxForPrinting("agent operation", operation.Kind.ToString())
        };
        writer.Line($"{kind} {operation.Reference}", operation);
        using (writer.Indent())
        {
            WriteAgentScope(writer, operation.Scope);
            if (operation.Description is { } description) writer.Line($"description {StringLiteral.Quote(description)}");
        }
    }

    static void WriteAgentScope(ScreenplayWriter writer, AgentScope? scope)
    {
        if (scope is null) return;
        var value = scope switch
        {
            AgentScope.Read => "read",
            AgentScope.Write => "write",
            _ => throw new UnsupportedSyntaxForPrinting("agent scope", scope.ToString()!)
        };
        writer.Line($"scope {value}");
    }
}
