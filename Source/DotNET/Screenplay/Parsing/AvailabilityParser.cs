// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses <c>audience</c> and <c>availability</c> declarations and the <c>available for | when | with</c> gate.
/// </summary>
internal static partial class AvailabilityParser
{
    const string GateExpected = "available for <Audience> [and|or <Audience> ...] | available when <condition> | available with <Availability> [and|or <Availability> ...]";

    static readonly HashSet<string> _gateKinds = new(StringComparer.Ordinal) { "for", "when", "with" };

    static readonly LogicalConditionDiagnostics _diagnostics = new(
        DiagnosticCodes.UnexpectedTokenInAvailable,
        DiagnosticCodes.UnclosedAvailableGroup,
        "available");

    /// <summary>
    /// Determines whether a line is an <c>available</c> gate, so a property that happens to be named <c>available</c> is left alone.
    /// </summary>
    /// <param name="content">The comment-free content of the line.</param>
    /// <returns><c>true</c> when the line starts <c>available for</c>, <c>available when</c> or <c>available with</c>.</returns>
    public static bool IsGate(string content) => GateStartRegex().IsMatch(content);

    /// <summary>
    /// Parses an <c>audience</c> declaration from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the header.</param>
    /// <returns>The parsed <see cref="AudienceSyntax"/>.</returns>
    public static AudienceSyntax ParseAudienceDeclaration(ParserContext context, SourceLine header)
    {
        var match = AudienceHeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidAudienceDeclaration, $"Invalid audience declaration '{header.Content}' - expected 'audience <Name>'", header.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(line.Content) == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Audience '{name}'");
                continue;
            }

            context.Error(DiagnosticCodes.InvalidAudienceDeclaration, $"Unexpected '{LineText.FirstWord(line.Content)}' in audience '{name}' - expected description", line.Location);
            context.SkipBlock(line.Indent);
        }

        return new AudienceSyntax(name, header.Location) { Description = description };
    }

    /// <summary>
    /// Parses an <c>availability</c> declaration from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the header.</param>
    /// <returns>The parsed <see cref="AvailabilitySyntax"/>.</returns>
    public static AvailabilitySyntax ParseAvailability(ParserContext context, SourceLine header)
    {
        var match = AvailabilityHeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidAvailabilityDeclaration, $"Invalid availability declaration '{header.Content}' - expected 'availability <Name>'", header.Location);
        }

        var name = match.Groups[1].Value;
        string? description = null;
        var requirements = new List<AvailabilityRequirementSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var keyword = LineText.FirstWord(line.Content);
            if (keyword == "description")
            {
                description = DescriptionParser.Parse(context, line, description, $"Availability '{name}'");
            }
            else if (_gateKinds.Contains(keyword))
            {
                if (ParseRequirement(context, keyword, line.Content[keyword.Length..], line.Location) is { } requirement)
                {
                    requirements.Add(requirement);
                }
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidAvailabilityDeclaration, $"Unexpected '{keyword}' in availability '{name}' - expected description, for, when or with", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new AvailabilitySyntax(name, requirements, header.Location) { Description = description };
    }

    /// <summary>
    /// Parses an <c>available</c> gate from its already consumed line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="line">The consumed <see cref="SourceLine"/> holding the <c>available</c> keyword.</param>
    /// <returns>The parsed <see cref="AvailableSyntax"/>, or <c>null</c> when the gate is malformed.</returns>
    public static AvailableSyntax? ParseGate(ParserContext context, SourceLine line)
    {
        var match = GateRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidAvailableGate, $"Invalid available gate '{line.Content}' - expected '{GateExpected}'", line.Location);
            return null;
        }

        return ParseRequirement(context, match.Groups[1].Value, match.Groups[2].Value, line.Location) is { } requirement
            ? new AvailableSyntax(requirement, line.Location)
            : null;
    }

    /// <summary>
    /// Appends a gate to a construct's gates, which all have to hold.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report to.</param>
    /// <param name="line">The consumed gate line.</param>
    /// <param name="gates">The gates parsed so far for the construct.</param>
    public static void AddGate(ParserContext context, SourceLine line, List<AvailableSyntax> gates)
    {
        if (ParseGate(context, line) is { } gate)
        {
            gates.Add(gate);
        }
    }

    static AvailabilityRequirementSyntax? ParseRequirement(ParserContext context, string kind, string text, SourceLocation location)
    {
        text = text.Trim();
        if (text.Length == 0)
        {
            context.Error(DiagnosticCodes.InvalidAvailableGate, $"Expected something after '{kind}' - {GateExpected}", location);
            return null;
        }

        if (kind == "when")
        {
            return ConditionParser.Parse(context, text, location) is { } condition
                ? new ConditionAvailabilityRequirementSyntax(condition, location)
                : null;
        }

        var tokens = TokenRegex().Matches(text).Select(match => match.Value).ToList();
        LogicalConditionParser.ParseOperand<AvailabilityRequirementSyntax> leaf = kind == "for" ? ParseAudienceOperand : ParseReference;
        return LogicalConditionParser.Parse(
            context,
            tokens,
            location,
            leaf,
            static (left, @operator, right, location) => new LogicalAvailabilityRequirementSyntax(left, @operator, right, location),
            _diagnostics);
    }

    static AvailabilityRequirementSyntax? ParseAudienceOperand(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (!TakeName(context, tokens, ref position, location, "an audience name", out var name))
        {
            return null;
        }

        return new AudienceAvailabilityRequirementSyntax(name, location);
    }

    static AvailabilityRequirementSyntax? ParseReference(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location)
    {
        if (!TakeName(context, tokens, ref position, location, "an availability name", out var name))
        {
            return null;
        }

        return new AvailabilityReferenceSyntax(name, location);
    }

    static bool TakeName(ParserContext context, IReadOnlyList<string> tokens, ref int position, SourceLocation location, string expected, out string name)
    {
        name = string.Empty;
        if (position >= tokens.Count)
        {
            context.Error(DiagnosticCodes.InvalidAvailableGate, $"Expected {expected}", location);
            return false;
        }

        var token = tokens[position++];
        if (!NameRegex().IsMatch(token))
        {
            context.Error(DiagnosticCodes.InvalidAvailableGate, $"Invalid name '{token}' - expected {expected} written as a PascalCase identifier", location);
            return false;
        }

        name = token;
        return true;
    }

    [GeneratedRegex(@"^audience\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex AudienceHeaderRegex();

    [GeneratedRegex(@"^availability\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex AvailabilityHeaderRegex();

    [GeneratedRegex(@"^available\s+(for|when|with)(?:\s|$)", RegexOptions.NonBacktracking)]
    private static partial Regex GateStartRegex();

    [GeneratedRegex(@"^available\s+(for|when|with)(?:\s+(.*))?$", RegexOptions.NonBacktracking)]
    private static partial Regex GateRegex();

    [GeneratedRegex(@"\(|\)|[A-Za-z_]\w*|\S", RegexOptions.NonBacktracking)]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"^[A-Z]\w*$", RegexOptions.NonBacktracking)]
    private static partial Regex NameRegex();
}
