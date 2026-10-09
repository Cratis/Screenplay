// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses report-only processing declarations and references.
/// </summary>
internal static partial class PurposeParser
{
    static readonly HashSet<string> _bases = ["consent", "contract", "legalObligation", "vitalInterests", "publicTask", "legitimateInterests"];
    static readonly HashSet<string> _conditions = ["explicitConsent", "employmentLaw", "vitalInterests", "notForProfit", "madePublic", "legalClaims", "substantialPublicInterest", "healthCare", "publicHealth", "research"];
    static readonly HashSet<string> _exceptions = ["expression", "legalObligation", "publicTask", "publicHealth", "archiving", "legalClaims"];

    internal static PurposeSyntax Parse(ParserContext context, SourceLine header)
    {
        var name = Name(context, header);
        var purpose = new PurposeSyntax(name, header.Location);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var locations = new Dictionary<string, SourceLocation>();
        var recipients = new List<string>();
        var transfers = new List<PurposeTransferSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var keyword = LineText.FirstWord(line.Content);
            if (keyword is not "recipient" and not "transfer" && !seen.Add(keyword))
            {
                context.Error(DiagnosticCodes.DuplicatePurposeField, $"Purpose '{name}' already declares '{keyword}' - at most one is allowed", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            locations.TryAdd(keyword, line.Location);
            var previousRecipients = recipients.Count;
            purpose = keyword switch
            {
                "description" => purpose with { Description = DescriptionParser.Parse(context, line, purpose.Description, $"Purpose '{name}'") },
                "basis" => Vocabulary(context, line, purpose, true),
                "condition" => Vocabulary(context, line, purpose, false),
                "interest" => purpose with { Interest = Text(context, line, keyword) },
                "authorization" => purpose with { Authorization = Text(context, line, keyword) },
                "retention" => purpose with { Retention = Text(context, line, keyword) },
                "subjects" => purpose with { Subjects = Subjects(context, line) },
                "erasure" => purpose with { ErasureException = Erasure(context, line) },
                "recipient" => Recipient(context, line, purpose, recipients),
                "transfer" => Transfer(context, line, purpose, transfers),
                _ => Unexpected(context, line, purpose)
            };
            if (recipients.Count > previousRecipients) locations[DirectiveLocationKeys.ForValue("recipient", recipients, recipients.Count - 1)] = line.Location;
        }

        return purpose with { Recipients = recipients, Transfers = transfers, DirectiveLocations = locations };
    }

    internal static void ParseReference(ParserContext context, SourceLine line, List<PurposeReferenceSyntax> references)
    {
        var name = Name(context, line);
        if (name.Length > 0 && !references.Exists(reference => reference.Name == name)) references.Add(new(name, line.Location));
    }

    static string Name(ParserContext context, SourceLine line)
    {
        var match = HeaderRegex().Match(line.Content);
        if (match.Success) return match.Groups[1].Value;
        context.Error(DiagnosticCodes.InvalidPurposeDeclaration, $"Invalid purpose declaration '{line.Content}' - expected 'purpose <Name>'", line.Location);
        return string.Empty;
    }

    static PurposeSyntax Vocabulary(ParserContext context, SourceLine line, PurposeSyntax purpose, bool basis)
    {
        var match = VocabularyRegex().Match(line.Content);
        var values = basis ? _bases : _conditions;
        if (!match.Success || !values.Contains(match.Groups[2].Value))
        {
            context.Error(DiagnosticCodes.InvalidPurposeVocabulary, $"Invalid {LineText.FirstWord(line.Content)} in purpose '{purpose.Name}' - expected {string.Join(", ", values)} with an optional quoted reference", line.Location);
            return purpose;
        }

        var reference = match.Groups[3].Success ? StringLiteral.Unescape(match.Groups[3].Value) : null;
        return basis ? purpose with { Basis = match.Groups[2].Value, BasisReference = reference } : purpose with { Condition = match.Groups[2].Value, ConditionReference = reference };
    }

    static string? Text(ParserContext context, SourceLine line, string keyword)
    {
        var match = TextRegex().Match(line.Content);
        if (match.Success) return StringLiteral.Unescape(match.Groups[2].Value);
        context.Error(DiagnosticCodes.InvalidPurposeDeclaration, $"Invalid purpose field '{line.Content}' - expected '{keyword} \"<text>\"'", line.Location);
        return null;
    }

    static string[] Subjects(ParserContext context, SourceLine line)
    {
        var values = line.Content["subjects".Length..].Split(',').Select(value => value.Trim()).ToArray();
        if (values.All(value => IdentifierRegex().IsMatch(value))) return [.. values.Distinct(StringComparer.Ordinal)];
        context.Error(DiagnosticCodes.InvalidPurposeDeclaration, $"Invalid purpose subjects '{line.Content}' - expected comma-separated identifiers", line.Location);
        return [];
    }

    static string? Erasure(ParserContext context, SourceLine line)
    {
        var match = ErasureRegex().Match(line.Content);
        if (match.Success && _exceptions.Contains(match.Groups[1].Value)) return match.Groups[1].Value;
        context.Error(DiagnosticCodes.InvalidPurposeVocabulary, $"Invalid erasure exception '{line.Content}' - expected 'erasure exception {string.Join('|', _exceptions)}'", line.Location);
        return null;
    }

    static PurposeSyntax Recipient(ParserContext context, SourceLine line, PurposeSyntax purpose, List<string> recipients)
    {
        if (Text(context, line, "recipient") is { } value) recipients.Add(value);
        return purpose;
    }

    static PurposeSyntax Transfer(ParserContext context, SourceLine line, PurposeSyntax purpose, List<PurposeTransferSyntax> transfers)
    {
        var match = TransferRegex().Match(line.Content);
        if (match.Success) transfers.Add(new(StringLiteral.Unescape(match.Groups[1].Value), StringLiteral.Unescape(match.Groups[2].Value), line.Location));
        else context.Error(DiagnosticCodes.InvalidPurposeDeclaration, $"Invalid transfer '{line.Content}' - expected 'transfer \"<destination>\" safeguard \"<text>\"'", line.Location);
        return purpose;
    }

    static PurposeSyntax Unexpected(ParserContext context, SourceLine line, PurposeSyntax purpose)
    {
        context.Error(DiagnosticCodes.InvalidPurposeDeclaration, $"Unexpected '{line.Content}' in purpose '{purpose.Name}'", line.Location);
        context.SkipBlock(line.Indent);
        return purpose;
    }

    [GeneratedRegex(@"^purpose\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^(basis|condition)\s+(\w+)(?:\s+""(" + StringLiteral.BodyPattern + @")"")?$", RegexOptions.None, 1000)]
    private static partial Regex VocabularyRegex();

    [GeneratedRegex(@"^(interest|authorization|retention|recipient)\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex TextRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*$", RegexOptions.None, 1000)]
    private static partial Regex IdentifierRegex();

    [GeneratedRegex(@"^erasure\s+exception\s+(\w+)$", RegexOptions.None, 1000)]
    private static partial Regex ErasureRegex();

    [GeneratedRegex(@"^transfer\s+""(" + StringLiteral.BodyPattern + @")""\s+safeguard\s+""(" + StringLiteral.BodyPattern + @")""$", RegexOptions.None, 1000)]
    private static partial Regex TransferRegex();
}
