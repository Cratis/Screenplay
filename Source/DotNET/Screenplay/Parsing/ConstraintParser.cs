// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>constraint</c> declarations.
/// </summary>
internal static partial class ConstraintParser
{
    /// <summary>
    /// Parses a constraint from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>constraint</c> header.</param>
    /// <returns>The parsed <see cref="ConstraintSyntax"/>.</returns>
    public static ConstraintSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = HeaderRegex().Match(header.Content) is { Success: true } match
            ? match.Groups[1].Value
            : ReportInvalidHeader(context, header);

        var rules = new List<ConstraintSyntax>();
        var releases = new List<string>();
        string? message = null;
        var ignoreCasing = false;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (ReleaseRegex().Match(line.Content) is { Success: true } release)
            {
                var releaseName = release.Groups[1].Value;
                if (releases.Contains(releaseName))
                {
                    context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' already releases with event '{releaseName}'", line.Location);
                }
                else
                {
                    releases.Add(releaseName);
                }

                continue;
            }

            if (MessageRegex().Match(line.Content) is { Success: true } text)
            {
                if (message is not null)
                {
                    context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' already has a message", line.Location);
                }
                else
                {
                    message = StringLiteral.Unescape(text.Groups[1].Value);
                }

                continue;
            }

            if (line.Content == "ignore casing")
            {
                if (ignoreCasing)
                {
                    context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' already ignores casing", line.Location);
                }

                ignoreCasing = true;
                continue;
            }

            var parsed = ParseRule(context, name, line);
            if (parsed is null)
            {
                continue;
            }

            if (rules.Count > 0 && (parsed is FileConstraintSyntax || rules[0] is FileConstraintSyntax ||
                (parsed is UniqueEventConstraintSyntax) != (rules[0] is UniqueEventConstraintSyntax)))
            {
                context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' cannot mix constraint kinds", line.Location);
                continue;
            }

            var eventName = parsed switch
            {
                UniquePropertyConstraintSyntax property => property.Event,
                UniqueEventConstraintSyntax occurrence => occurrence.Event,
                _ => null
            };
            if (eventName is not null && rules.Exists(rule => rule switch
            {
                UniquePropertyConstraintSyntax property => property.Event == eventName,
                UniqueEventConstraintSyntax occurrence => occurrence.Event == eventName,
                _ => false
            }))
            {
                context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' already targets event '{eventName}'", line.Location);
                continue;
            }

            rules.Add(parsed);
        }

        foreach (var release in releases.Where(release => rules.Exists(rule => rule switch
        {
            UniquePropertyConstraintSyntax property => property.Event == release,
            UniqueEventConstraintSyntax occurrence => occurrence.Event == release,
            _ => false
        })))
        {
            context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' cannot target and release event '{release}'", header.Location);
        }

        if (rules.Count == 0)
        {
            context.Error(DiagnosticCodes.ConstraintWithoutRule, $"Constraint '{name}' must declare 'unique ... on ...', 'unique event ...' or 'file ...'", header.Location);
            rules.Add(new UniqueEventConstraintSyntax(name, string.Empty, header.Location));
        }

        if (rules[0] is FileConstraintSyntax && (releases.Count > 0 || message is not null || ignoreCasing))
        {
            context.Error(DiagnosticCodes.InvalidConstraintBody, $"File constraint '{name}' cannot have declarative options", header.Location);
        }

        return rules[0] with
        {
            AdditionalRules = [.. rules.Skip(1)],
            ReleasedBy = releases,
            Message = message,
            IgnoreCasing = ignoreCasing
        };
    }

    static ConstraintSyntax? ParseRule(ParserContext context, string name, SourceLine line)
    {
        if (UniqueEventRegex().Match(line.Content) is { Success: true } uniqueEvent)
        {
            return new UniqueEventConstraintSyntax(name, uniqueEvent.Groups[1].Value, line.Location);
        }

        if (UniquePropertyRegex().Match(line.Content) is { Success: true } uniqueProperty)
        {
            var properties = uniqueProperty.Groups[1].Value.Split(',').Select(_ => _.Trim()).ToArray();
            if (properties.Distinct(StringComparer.Ordinal).Count() != properties.Length)
            {
                context.Error(DiagnosticCodes.DuplicateConstraintBody, $"Constraint '{name}' repeats a property in its composite key", line.Location);
                return null;
            }

            return new UniquePropertyConstraintSyntax(name, properties[0], uniqueProperty.Groups[2].Value, line.Location)
            {
                AdditionalProperties = [.. properties.Skip(1)]
            };
        }

        if (FileReferenceParser.IsDirective(line))
        {
            context.Warning(
                DiagnosticCodes.FileConstraintOnlySupportsUniqueness,
                "A file constraint can only declare unique constraints in Chronicle - declare them with 'unique ...' so they are portable; a rule that is not uniqueness belongs in command validation or a 'require' condition",
                line.Location);
            return new FileConstraintSyntax(name, FileReferenceParser.Parse(context, line), line.Location)
            {
                DirectiveLocations = new Dictionary<string, SourceLocation> { ["file"] = line.Location }
            };
        }

        context.Error(DiagnosticCodes.InvalidConstraintBody, $"Invalid constraint body '{line.Content}'", line.Location);
        return null;
    }

    static string ReportInvalidHeader(ParserContext context, SourceLine header)
    {
        context.Error(DiagnosticCodes.InvalidConstraintDeclaration, $"Invalid constraint declaration '{header.Content}' - expected 'constraint <Name>'", header.Location);
        return LineText.FirstWord(header.Content);
    }

    [GeneratedRegex(@"^constraint\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^unique\s+event\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UniqueEventRegex();

    [GeneratedRegex(@"^unique\s+([a-z_][\w.]*(?:\s*,\s*[a-z_][\w.]*)*)\s+on\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex UniquePropertyRegex();

    [GeneratedRegex(@"^released\s+by\s+([A-Z]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ReleaseRegex();

    [GeneratedRegex("^message\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.None, 1000)]
    private static partial Regex MessageRegex();
}
