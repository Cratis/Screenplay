// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the application-level <c>sink &lt;Name&gt; for readmodels|graphs</c> declaration and the <c>sink &lt;Name&gt;</c>
/// body directive of a read model or graph.
/// </summary>
/// <remarks>
/// <c>sink</c> stays contextual inside a read model: a line is the directive only when it is exactly <c>sink</c> followed
/// by one identifier that is not a primitive type. <c>sink String</c>, <c>sink Money[]</c> or <c>sink Money optional</c>
/// remain properties named <c>sink</c>, and <c>@sink Money</c> declares one of any type.
/// </remarks>
internal static partial class SinkParser
{
    /// <summary>
    /// The keyword both forms start with.
    /// </summary>
    internal const string Keyword = "sink";

    /// <summary>
    /// Parses an application-level sink declaration.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the declaration.</param>
    /// <returns>The parsed <see cref="SinkSyntax"/>, or <c>null</c> when the line is malformed.</returns>
    public static SinkSyntax? ParseDeclaration(ParserContext context, SourceLine line)
    {
        var match = DeclarationRegex().Match(line.Content);
        if (context.TryPeekChild(line.Indent, out var child))
        {
            context.Error(DiagnosticCodes.InvalidSinkDeclaration, $"A sink is declared on one line; '{child.Content}' has no place under it", child.Location);
            context.SkipBlock(line.Indent);
        }

        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSinkDeclaration, $"Invalid sink declaration '{line.Content}' - expected 'sink <Name> for readmodels' or 'sink <Name> for graphs'", line.Location);
            return null;
        }

        var kind = match.Groups[2].Value == "graphs" ? SinkKind.Graphs : SinkKind.ReadModels;
        return new(match.Groups[1].Value, kind, line.Location);
    }

    /// <summary>
    /// Determines whether a read-model body line is the <c>sink</c> directive rather than a property named <c>sink</c>.
    /// </summary>
    /// <param name="content">The line content.</param>
    /// <returns><see langword="true"/> when the line is the directive or a malformed attempt at it.</returns>
    public static bool IsReadModelDirective(string content)
    {
        if (LineText.FirstWord(content) != Keyword) return false;
        if (content == Keyword) return true;
        var match = DirectiveRegex().Match(content);
        return match.Success && !ConceptSyntax.PrimitiveTypes.Contains(match.Groups[1].Value, StringComparer.Ordinal);
    }

    /// <summary>
    /// Parses a <c>sink &lt;Name&gt;</c> directive into an owner's current choice, reporting repeats.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The directive line.</param>
    /// <param name="current">The sink the owner already names, or <c>null</c>.</param>
    /// <param name="owner">Describes the owner in diagnostics, such as <c>Read model 'Revenue'</c>.</param>
    /// <param name="directiveLocations">The owner's directive locations, which record the first directive.</param>
    /// <returns>The sink the owner names after the line.</returns>
    public static string? ParseDirective(ParserContext context, SourceLine line, string? current, string owner, Dictionary<string, SourceLocation> directiveLocations)
    {
        var match = DirectiveRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSinkDeclaration, $"Invalid sink directive '{line.Content}' - expected 'sink <Name>'", line.Location);
            return current;
        }

        var name = match.Groups[1].Value;
        if (current is null)
        {
            directiveLocations[Keyword] = line.Location;
            return name;
        }

        if (string.Equals(current, name, StringComparison.Ordinal))
        {
            context.Warning(DiagnosticCodes.RepeatedSinkDirective, $"{owner} repeats 'sink {name}'; keep one directive.", line.Location);
        }
        else
        {
            context.Error(DiagnosticCodes.ConflictingSinkDirective, $"{owner} names sink '{current}' and sink '{name}'; it is stored in one sink.", line.Location);
        }

        return current;
    }

    [GeneratedRegex(@"^sink\s+([A-Za-z_]\w*)\s+for\s+(readmodels|graphs)$", RegexOptions.NonBacktracking)]
    private static partial Regex DeclarationRegex();

    [GeneratedRegex(@"^sink\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex DirectiveRegex();
}
