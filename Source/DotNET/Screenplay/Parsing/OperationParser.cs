// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

internal static partial class OperationParser
{
    static readonly HashSet<string> _eventMetadata = new(StringComparer.Ordinal)
    {
        "for", "tag", "generation", "origin", "documentation", "namespace", "sequence", "correlation", "causation", "causedBy", "occurred"
    };

    internal static SystemSyntax ParseSystem(ParserContext context, SourceLine header)
    {
        var match = SystemHeaderRegex().Match(header.Content);
        if (!match.Success) context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'system <Name>'.", header.Location);
        string? description = null;
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "description")
            {
                description = DescriptionParser.Parse(context, child, description, $"System '{match.Groups[1].Value}'", out _);
                locations.TryAdd("description", child.Location);
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidSystemDeclaration, "A system accepts only a description; abilities are not supported.", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return new(match.Groups[1].Value, description, header.Location) { DirectiveLocations = locations };
    }

    internal static (OperationSyntax Operation, List<PropertyMappingSyntax> Mappings) Parse(ParserContext context, SourceLine header, bool inline = false)
    {
        var match = (inline ? InlineHeaderRegex() : HeaderRegex()).Match(header.Content);
        var name = match.Groups[1].Value;
        if (!match.Success) context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Expected an operation declaration with one name.", header.Location);
        string? description = null;
        string? uses = null;
        OperationPhaseSyntax? execute = null;
        OperationPhaseSyntax? compensate = null;
        var inputs = new List<PropertySyntax>();
        var mappings = new List<PropertyMappingSyntax>();
        var names = new HashSet<string>(StringComparer.Ordinal);
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var typed = TypedMappingRegex().Match(line.Content);
            var propertyLine = inline && typed.Success ? line with { Content = typed.Groups[1].Value.TrimEnd() } : line;
            if (!typed.Success && _eventMetadata.Contains(LineText.FirstWord(line.Content)))
            {
                context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Operations cannot declare event routing or metadata; escape a keyword-named input with @.", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            // 'uses System' is intentionally a directive; @uses escapes an input of that name.
            if (!UsesRegex().IsMatch(line.Content) && PropertyLineParser.TryParse(propertyLine) is not null)
            {
                var property = PropertyLineParser.Parse(context, propertyLine)!;
                if (inline && !typed.Success)
                {
                    context.Error(DiagnosticCodes.InvalidOperationMapping, "An inline operation input requires '<property> <Type> = <source>'.", line.Location);
                }
                if (property.IsIdentifier || property.IsGenerated)
                {
                    context.Error(DiagnosticCodes.InvalidOperationDeclaration, "Operation inputs cannot be identifier or generated properties.", line.Location);
                }
                if (!names.Add(property.Name)) context.Error(DiagnosticCodes.DuplicateDeclaration, $"Operation '{name}' already declares input '{property.Name}'.", line.Location);
                inputs.Add(property);
                if (inline && typed.Success) mappings.Add(ExpressionParser.ParseMapping(context, property.Name, typed.Groups[2], line));
                RejectChildren(context, line, DiagnosticCodes.InvalidOperationDeclaration, "An operation input cannot have children.");
                continue;
            }
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Operation '{name}'", out _);
                locations.TryAdd("description", line.Location);
            }
            else if (UsesRegex().Match(line.Content) is { Success: true } reference)
            {
                if (uses is not null) context.Error(DiagnosticCodes.InvalidSystemReference, "An operation declares exactly one 'uses <System>'.", line.Location);
                else uses = reference.Groups[1].Value;
                locations.TryAdd("uses", line.LocationAt(reference.Groups[1].Index));
                RejectChildren(context, line, DiagnosticCodes.InvalidSystemReference, "A system reference cannot have children.");
            }
            else if (line.Content == "execute" || line.Content == "compensate")
            {
                var phase = ParsePhase(context, line);
                if (line.Content == "execute")
                {
                    if (execute is not null) context.Error(DiagnosticCodes.InvalidOperationDeclaration, "An operation declares execute at most once.", line.Location);
                    else execute = phase;
                }
                else if (compensate is not null)
                {
                    context.Error(DiagnosticCodes.InvalidOperationDeclaration, "An operation declares compensate at most once.", line.Location);
                }
                else
                {
                    compensate = phase;
                }
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidOperationDeclaration, $"Unexpected '{line.Content}' in operation - expected uses, an input, description, execute or compensate.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }
        if (uses is null) context.Error(DiagnosticCodes.InvalidSystemReference, "An operation requires exactly one 'uses <System>'.", header.Location);

        return (new(name, uses ?? string.Empty, inputs, header.Location) { Description = description, Execute = execute, Compensate = compensate, DirectiveLocations = locations }, mappings);
    }

    internal static OperationPhaseSyntax ParsePhase(ParserContext context, SourceLine header)
    {
        string? description = null;
        FileReferenceSyntax? file = null;
        CodeBlockSyntax? code = null;
        ImplementationSyntax? implementation = null;
        var sourceSelected = false;
        var locations = new Dictionary<string, SourceLocation>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Operation phase '{header.Content}'", out _);
                locations.TryAdd("description", line.Location);
                continue;
            }
            if (LineText.FirstWord(line.Content) == "implementation")
            {
                if (sourceSelected) context.Error(DiagnosticCodes.ConflictingImplementationSources, "A phase cannot mix wrapped and direct sources or repeat its wrapper.", line.Location);
                var source = ImplementationParser.ParseWrapper(context, line);
                if (!sourceSelected)
                {
                    file = source.File;
                    code = source.Code;
                    implementation = source.Implementation;
                }
                sourceSelected = true;
            }
            else if (FileReferenceParser.IsDirective(line) || line.Content.StartsWith("```", StringComparison.Ordinal))
            {
                if (sourceSelected) context.Error(DiagnosticCodes.ConflictingImplementationSources, "A phase has at most one file or inline payload.", line.Location);
                var parsedFile = FileReferenceParser.IsDirective(line) ? FileReferenceParser.Parse(context, line) : null;
                var parsedCode = parsedFile is null ? CodeBlockParser.Parse(context, line) : null;
                if (!sourceSelected)
                {
                    file = parsedFile;
                    code = parsedCode;
                }
                sourceSelected = true;
                if (parsedFile is not null) RejectChildren(context, line, DiagnosticCodes.InvalidImplementationBlock, "A file directive cannot have children.");
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidOperationDeclaration, "A phase accepts description, file, a tagged fence or implementation.", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(description, file, code, implementation, header.Location) { DirectiveLocations = locations };
    }

    internal static void RejectChildren(ParserContext context, SourceLine line, string code, string message)
    {
        if (!context.TryPeekChild(line.Indent, out var child)) return;
        context.Error(code, message, child.Location);
        context.SkipBlock(line.Indent);
    }

    [GeneratedRegex(@"^system\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex SystemHeaderRegex();
    [GeneratedRegex(@"^operation\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();
    [GeneratedRegex(@"^produces\s+operation\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex InlineHeaderRegex();
    [GeneratedRegex(@"^uses\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UsesRegex();
    [GeneratedRegex(@"^(.+?)\s*=(?!=|>)\s*(.+)$", RegexOptions.None, 1000)]
    private static partial Regex TypedMappingRegex();
}
