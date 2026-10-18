// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Syntax.Projections;

namespace Cratis.Screenplay.Mcp;

// The authored text of a lineage clause and the terminal a value with no member source ends in.
static class McpLineageText
{
    internal static string Mapping(MappingSyntax mapping) => mapping switch
    {
        SetMappingSyntax set => $"{set.Property} = {ScreenplaySyntaxText.Expression(set.Source)}",
        AddMappingSyntax add => $"add {add.Property} by {ScreenplaySyntaxText.Expression(add.Value)}",
        SubtractMappingSyntax subtract => $"subtract {subtract.Property} by {ScreenplaySyntaxText.Expression(subtract.Value)}",
        CountMappingSyntax count => $"count {count.Property}",
        IncrementMappingSyntax increment => $"increment {increment.Property}",
        DecrementMappingSyntax decrement => $"decrement {decrement.Property}",
        ClearMappingSyntax clear => $"clear {clear.Property}",
        _ => mapping.Property
    };

    internal static string Clause(SyntaxNode? node, string fallback) => node switch
    {
        PropertyMappingSyntax mapping => $"{mapping.Property} = {ScreenplaySyntaxText.Expression(mapping.Source)}",
        InteractionArgumentSyntax argument => $"{argument.Name} = {argument.Binding}",
        FormFieldSyntax field => $"field {field.Property}",
        ScreenColumnSyntax column => $"column {column.Property}",
        _ => fallback
    };

    // A projection value that names no event member: an operation, a literal or a context value.
    internal static McpLineageStep? Terminal(MappingSyntax mapping, SourceLocation location) => mapping switch
    {
        SetMappingSyntax set => Terminal(set.Source, location),
        CountMappingSyntax or IncrementMappingSyntax or DecrementMappingSyntax or ClearMappingSyntax or AddMappingSyntax or SubtractMappingSyntax =>
            McpLineageStep.Terminal("operation", location, Mapping(mapping)),
        _ => null
    };

    // A production or projection value with no member source. Null means the trace cannot classify it.
    internal static McpLineageStep? Terminal(ExpressionSyntax? expression, SourceLocation location)
    {
        var role = expression switch
        {
            LiteralExpressionSyntax or TemplateExpressionSyntax or StringsExpressionSyntax or ListExpressionSyntax or ObjectExpressionSyntax => "literal",
            ContextExpressionSyntax or IdentityExpressionSyntax or EventSourceIdExpressionSyntax or EventContextExpressionSyntax or CausedByExpressionSyntax or EnvironmentExpressionSyntax => "context",
            SourceItemExpressionSyntax => "capture-source",
            _ => null
        };

        return role is null ? null : McpLineageStep.Terminal(role, location, ScreenplaySyntaxText.Expression(expression!));
    }
}
