// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>Parses the application-root agent allowlist without inferring any permissions.</summary>
internal static partial class AgentsParser
{
    public static AgentsSyntax Parse(ParserContext context, SourceLine header, AgentsSyntax? existing)
    {
        ValidateHeader(context, header);
        AgentTokensSyntax? tokens = null;
        var operations = new List<AgentOperationSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "tokens":
                    tokens = ParseTokens(context, line, tokens);
                    RejectChildren(context, line);
                    break;
                case "query":
                case "command":
                    operations.Add(ParseOperation(context, line));
                    break;
                case "scope":
                    context.Error(DiagnosticCodes.GlobalAgentScope, "An agents block has no global scope - declare scope on each query or command", line.Location);
                    RejectChildren(context, line);
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidAgentsDeclaration, $"Unexpected agents declaration '{line.Content}'", line.Location);
                    RejectChildren(context, line);
                    break;
            }
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateAgentsDeclaration, "An application can declare only one agents block", header.Location);
            return existing;
        }

        return new(operations, header.Location) { Tokens = tokens };
    }

    static void ValidateHeader(ParserContext context, SourceLine header)
    {
        if (header.Content != "agents")
        {
            context.Error(DiagnosticCodes.InvalidAgentsDeclaration, "Expected 'agents' with no arguments", header.Location);
        }
    }

    static AgentTokensSyntax? ParseTokens(ParserContext context, SourceLine line, AgentTokensSyntax? existing)
    {
        if (existing is not null)
        {
            context.Error(DiagnosticCodes.AgentTokensMissing, "An agents block requires exactly one tokens declaration", line.Location);
            return existing;
        }

        return line.Content switch
        {
            "tokens personal" => new(AgentTokenKind.Personal, line.Location),
            "tokens oauth" => new(AgentTokenKind.OAuth, line.Location),
            _ => UnsupportedTokens(context, line)
        };
    }

    static AgentTokensSyntax? UnsupportedTokens(ParserContext context, SourceLine line)
    {
        context.Error(DiagnosticCodes.UnsupportedAgentTokenKind, "Agents currently support only 'tokens personal'", line.Location);
        return null;
    }

    static AgentOperationSyntax ParseOperation(ParserContext context, SourceLine header)
    {
        var kind = LineText.FirstWord(header.Content) == "query" ? AgentOperationKind.Query : AgentOperationKind.Command;
        var reference = ParseReference(context, header);
        AgentScope? scope = null;
        string? description = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "scope":
                    scope = ParseScope(context, line, scope);
                    break;
                case "description":
                    description = ParseDescription(context, line, description);
                    break;
                case "approval":
                    context.Error(DiagnosticCodes.AgentApprovalNotSupported, "Agent approval is not supported - token scope and model authorization must both permit the call", line.Location);
                    break;
                default:
                    context.Error(DiagnosticCodes.InvalidAgentsDeclaration, $"Unexpected agent operation declaration '{line.Content}'", line.Location);
                    break;
            }

            RejectChildren(context, line);
        }

        return new(kind, reference, header.Location) { Scope = scope, Description = description };
    }

    static string ParseReference(ParserContext context, SourceLine header)
    {
        var match = OperationRegex().Match(header.Content);
        if (match.Success) return match.Groups[2].Value;
        context.Error(DiagnosticCodes.InvalidAgentsDeclaration, $"Invalid agent operation '{header.Content}' - expected 'query <reference>' or 'command <reference>'", header.Location);
        return string.Empty;
    }

    static string? ParseDescription(ParserContext context, SourceLine line, string? existing)
    {
        if (LiteralDescriptionRegex().IsMatch(line.Content)) return DescriptionParser.Parse(context, line, existing, "An agent operation");
        context.Error(DiagnosticCodes.InvalidAgentsDeclaration, "An agent operation description must be literal quoted text", line.Location);
        return existing;
    }

    static void RejectChildren(ParserContext context, SourceLine line)
    {
        if (!context.TryPeekChild(line.Indent, out var child)) return;
        context.Error(DiagnosticCodes.InvalidAgentsDeclaration, "Agent fields cannot have children", child.Location);
        context.SkipBlock(line.Indent);
    }

    static AgentScope? ParseScope(ParserContext context, SourceLine line, AgentScope? existing)
    {
        if (existing is not null || line.Content is not ("scope read" or "scope write"))
        {
            context.Error(DiagnosticCodes.AgentOperationMissingScope, "An agent operation requires exactly one 'scope read' or 'scope write'", line.Location);
            return existing;
        }

        return line.Content == "scope read" ? AgentScope.Read : AgentScope.Write;
    }

    [GeneratedRegex(@"^(query|command)\s+([A-Za-z_][A-Za-z0-9_]*(?:\.[A-Za-z_][A-Za-z0-9_]*)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex OperationRegex();

    [GeneratedRegex(@"^description\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.NonBacktracking)]
    private static partial Regex LiteralDescriptionRegex();
}
