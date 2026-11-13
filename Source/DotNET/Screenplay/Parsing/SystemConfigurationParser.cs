// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the realization lines of a system - <c>extension</c>, <c>setting</c> and <c>activation</c> - and top-level
/// <c>secret</c> declarations.
/// </summary>
internal static partial class SystemConfigurationParser
{
    internal static readonly IReadOnlySet<string> Keywords = new HashSet<string>(StringComparer.Ordinal) { "extension", "setting", "activation" };

    internal static SecretSyntax? ParseSecret(ParserContext context, SourceLine header)
    {
        var match = SecretHeaderRegex().Match(header.Content);
        if (!match.Success) context.Error(DiagnosticCodes.InvalidSecretDeclaration, "Expected 'secret <Name>'.", header.Location);
        var name = match.Success ? match.Groups[1].Value : string.Empty;
        string? description = null;
        while (context.TryPeekChild(header.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) == "description")
            {
                description = DescriptionParser.Parse(context, child, description, $"Secret '{name}'");
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidSecretDeclaration, "A secret accepts only a description.", child.Location);
                context.SkipBlock(child.Indent);
            }
        }

        return match.Success ? new(name, description, header.Location) : null;
    }

    internal static SystemExtensionSyntax? ParseExtension(ParserContext context, SourceLine line)
    {
        SkipChildren(context, line);
        var match = ExtensionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'extension \"<reference>\" [version <range>]'.", line.Location);
            return null;
        }

        var reference = StringLiteral.Unescape(match.Groups[1].Value);
        if (string.IsNullOrWhiteSpace(reference))
        {
            context.Error(DiagnosticCodes.InvalidSystemDeclaration, "An extension reference cannot be empty.", line.Location);
            return null;
        }

        return new(reference, match.Groups[2].Success ? match.Groups[2].Value : null, line.Location);
    }

    internal static SystemSettingSyntax? ParseSetting(ParserContext context, SourceLine line)
    {
        SkipChildren(context, line);
        var fromSecret = SettingFromSecretRegex().Match(line.Content);
        if (fromSecret.Success) return new(fromSecret.Groups[1].Value, fromSecret.Groups[2].Value, null, null, line.Location);

        var literal = SettingLiteralRegex().Match(line.Content);
        if (literal.Success)
        {
            var text = literal.Groups[2].Value;
            if (text.Length > 1 && text[0] == '"' && text[^1] == '"' && StringBodyRegex().IsMatch(text)) return new(literal.Groups[1].Value, null, StringLiteral.Unescape(text[1..^1]), "string", line.Location);
            if (string.Equals(text, "true", StringComparison.Ordinal) || string.Equals(text, "false", StringComparison.Ordinal)) return new(literal.Groups[1].Value, null, text, "boolean", line.Location);
            if (NumberRegex().IsMatch(text)) return new(literal.Groups[1].Value, null, text, "number", line.Location);
        }

        context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'setting <name> from secret <Name>' or 'setting <name> = <string, number or true or false>'.", line.Location);
        return null;
    }

    internal static SystemActivationSyntax? ParseActivation(ParserContext context, SourceLine line)
    {
        SkipChildren(context, line);
        var match = ActivationRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidSystemDeclaration, "Expected 'activation always' or 'activation on demand [idle after <n> <seconds|minutes|hours|days>]'.", line.Location);
            return null;
        }

        if (match.Groups[1].Success) return new(true, null, null, line.Location);
        if (!match.Groups[2].Success) return new(false, null, null, line.Location);
        if (!int.TryParse(match.Groups[2].Value, NumberStyles.None, CultureInfo.InvariantCulture, out var amount) || amount < 1)
        {
            context.Error(DiagnosticCodes.InvalidSystemDeclaration, "An idle timeout is a whole number greater than zero.", line.Location);
            return null;
        }

        return new(false, amount, match.Groups[3].Value.TrimEnd('s'), line.Location);
    }

    static void SkipChildren(ParserContext context, SourceLine line)
    {
        if (!context.TryPeekChild(line.Indent, out var child)) return;
        context.Error(DiagnosticCodes.InvalidSystemDeclaration, "A system's extension, setting and activation lines are written on one line.", child.Location);
        context.SkipBlock(line.Indent);
    }

    [GeneratedRegex(@"^secret\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex SecretHeaderRegex();

    [GeneratedRegex(@"^extension\s+""((?:[^""\\]|\\.)*)""(?:\s+version\s+(\S+))?$", RegexOptions.NonBacktracking)]
    private static partial Regex ExtensionRegex();

    [GeneratedRegex(@"^setting\s+([A-Za-z_]\w*)\s+from\s+secret\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex SettingFromSecretRegex();

    [GeneratedRegex(@"^setting\s+([A-Za-z_]\w*)\s*=\s*(\S.*)$", RegexOptions.NonBacktracking)]
    private static partial Regex SettingLiteralRegex();

    [GeneratedRegex(@"^""(?:[^""\\]|\\.)*""$", RegexOptions.NonBacktracking)]
    private static partial Regex StringBodyRegex();

    [GeneratedRegex(@"^-?\d+(?:\.\d+)?$", RegexOptions.NonBacktracking)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"^activation\s+(?:(always)|on\s+demand(?:\s+idle\s+after\s+(\d+)\s+(seconds?|minutes?|hours?|days?))?)$", RegexOptions.NonBacktracking)]
    private static partial Regex ActivationRegex();
}
