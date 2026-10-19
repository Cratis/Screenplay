// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses the shell declarations of a <c>ui profile</c>: <c>root template</c> with its <c>option</c> lines, the <c>appearance</c>
/// block and <c>new screens use template</c>. Only the grammar is judged here; whether a template, option or value exists is the validator's.
/// </summary>
internal static partial class ProfileShellParser
{
    const string RootForm = "root template [blueprint <Blueprint>] <Name> [version \"<range>\"]";
    const string NewScreensForm = "new screens use template [blueprint <Blueprint>] <Name> [version \"<range>\"]";

    /// <summary>
    /// Parses a <c>root template</c> line and the <c>option</c> lines under it.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The <c>root template</c> line.</param>
    /// <returns>The parsed <see cref="RootTemplateSyntax"/>, or <see langword="null"/> when the line is malformed.</returns>
    public static RootTemplateSyntax? ParseRoot(ParserContext context, SourceLine line)
    {
        var reference = ReadReference(context, line, "root template".Length, RootForm);
        var options = new List<ShellOptionSyntax>();
        while (context.TryPeekChild(line.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            if (LineText.FirstWord(child.Content) != "option")
            {
                context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Unexpected '{LineText.FirstWord(child.Content)}' under root template - expected 'option <name> <value>'", child.Location);
                context.SkipBlock(child.Indent);
                continue;
            }

            if (context.TryPeekChild(child.Indent, out _))
            {
                context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Unexpected block under '{child.Content}' - an option is one line", child.Location);
                context.SkipBlock(child.Indent);
            }

            var option = ParseOption(context, child);
            if (option is null)
            {
                continue;
            }

            if (options.Exists(existing => existing.Name == option.Name))
            {
                context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"The root template already sets option '{option.Name}' - an option can be set once", child.Location);
                continue;
            }

            options.Add(option);
        }

        return reference is null ? null : new(reference.Value.Name, options, line.Location) { Blueprint = reference.Value.Blueprint, Version = reference.Value.Version };
    }

    /// <summary>
    /// Parses a <c>new screens use template</c> line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="line">The <c>new screens use template</c> line.</param>
    /// <returns>The parsed <see cref="NewScreenTemplateSyntax"/>, or <see langword="null"/> when the line is malformed.</returns>
    public static NewScreenTemplateSyntax? ParseNewScreenTemplate(ParserContext context, SourceLine line)
    {
        if (!line.Content.StartsWith("new screens use template", StringComparison.Ordinal))
        {
            context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Invalid ui profile line '{line.Content}' - expected '{NewScreensForm}'", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        var reference = ReadReference(context, line, "new screens use template".Length, NewScreensForm);
        if (context.TryPeekChild(line.Indent, out _))
        {
            context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Unexpected block under '{line.Content}' - declare one template per line", line.Location);
            context.SkipBlock(line.Indent);
        }

        return reference is null ? null : new(reference.Value.Name, line.Location) { Blueprint = reference.Value.Blueprint, Version = reference.Value.Version };
    }

    /// <summary>
    /// Parses an <c>appearance</c> block.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="header">The <c>appearance</c> line.</param>
    /// <returns>The parsed <see cref="ProfileAppearanceSyntax"/>.</returns>
    public static ProfileAppearanceSyntax ParseAppearance(ParserContext context, SourceLine header)
    {
        if (header.Content != "appearance")
        {
            context.Error(DiagnosticCodes.InvalidProfileAppearance, $"Invalid appearance line '{header.Content}' - expected 'appearance' followed by an indented block", header.Location);
        }

        string? colorScheme = null;
        string? accent = null;
        string? corners = null;
        string? font = null;
        SourceLocation? colorSchemeLocation = null;
        SourceLocation? accentLocation = null;
        SourceLocation? cornersLocation = null;
        SourceLocation? fontLocation = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var schemeMatch = ColorSchemeRegex().Match(line.Content);
            var accentMatch = AccentRegex().Match(line.Content);
            var cornersMatch = CornersRegex().Match(line.Content);
            var fontMatch = FontRegex().Match(line.Content);
            if (schemeMatch.Success)
            {
                if (colorScheme is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This appearance block already declares a color scheme - at most one is allowed", line.Location);
                    continue;
                }

                colorScheme = schemeMatch.Groups[1].Value;
                colorSchemeLocation = line.Location;
            }
            else if (accentMatch.Success)
            {
                if (accent is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This appearance block already declares an accent - at most one is allowed", line.Location);
                    continue;
                }

                accent = StringLiteral.Unescape(accentMatch.Groups[1].Value);
                accentLocation = line.Location;
            }
            else if (cornersMatch.Success)
            {
                if (corners is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This appearance block already declares corners - at most one is allowed", line.Location);
                    continue;
                }

                corners = cornersMatch.Groups[1].Value;
                cornersLocation = line.Location;
            }
            else if (fontMatch.Success)
            {
                if (font is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This appearance block already declares a font - at most one is allowed", line.Location);
                    continue;
                }

                font = fontMatch.Groups[1].Value;
                fontLocation = line.Location;
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidProfileAppearance, $"Invalid appearance line '{line.Content}' - expected 'color scheme <light|dark|system>', 'accent \"<#hex>\"', 'corners <sharp|default|round>' or 'font <font id>'", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(colorScheme, accent, header.Location)
        {
            ColorSchemeLocation = colorSchemeLocation,
            AccentLocation = accentLocation,
            Corners = corners,
            CornersLocation = cornersLocation,
            Font = font,
            FontLocation = fontLocation
        };
    }

    /// <summary>
    /// Parses an <c>identity</c> block.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="header">The <c>identity</c> line.</param>
    /// <returns>The parsed <see cref="ProfileIdentitySyntax"/>.</returns>
    public static ProfileIdentitySyntax ParseIdentity(ParserContext context, SourceLine header)
    {
        if (header.Content != "identity")
        {
            context.Error(DiagnosticCodes.InvalidProfileAppearance, $"Invalid identity line '{header.Content}' - expected 'identity' followed by an indented block", header.Location);
        }

        string? name = null;
        string? logoKind = null;
        string? logoValue = null;
        bool? showName = null;
        SourceLocation? nameLocation = null;
        SourceLocation? logoLocation = null;
        SourceLocation? showNameLocation = null;
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var nameMatch = IdentityNameRegex().Match(line.Content);
            var logoMatch = LogoRegex().Match(line.Content);
            var showNameMatch = ShowNameRegex().Match(line.Content);
            if (nameMatch.Success)
            {
                if (name is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This identity block already declares a name - at most one is allowed", line.Location);
                    continue;
                }

                name = StringLiteral.Unescape(nameMatch.Groups[1].Value);
                nameLocation = line.Location;
            }
            else if (logoMatch.Success)
            {
                if (logoKind is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This identity block already declares a logo - at most one is allowed", line.Location);
                    continue;
                }

                logoKind = logoMatch.Groups[1].Value;
                logoValue = logoMatch.Groups[2].Success ? StringLiteral.Unescape(logoMatch.Groups[2].Value) : logoMatch.Groups[3].Value;
                logoLocation = line.Location;
            }
            else if (showNameMatch.Success)
            {
                if (showName is not null)
                {
                    context.Error(DiagnosticCodes.InvalidProfileAppearance, "This identity block already declares 'show name' - at most one is allowed", line.Location);
                    continue;
                }

                showName = showNameMatch.Groups[1].Value == "true";
                showNameLocation = line.Location;
            }
            else
            {
                context.Error(DiagnosticCodes.InvalidProfileAppearance, $"Invalid identity line '{line.Content}' - expected 'name \"<text>\"', 'logo initials \"<AB>\"', 'logo icon <icon>', 'logo image \"<data URI>\"' or 'show name <true|false>'", line.Location);
                context.SkipBlock(line.Indent);
            }
        }

        return new(name, header.Location)
        {
            NameLocation = nameLocation,
            LogoKind = logoKind,
            LogoValue = logoValue,
            LogoLocation = logoLocation,
            ShowName = showName,
            ShowNameLocation = showNameLocation
        };
    }

    static (string? Blueprint, string Name, string? Version)? ReadReference(ParserContext context, SourceLine line, int keywordLength, string form)
    {
        var text = line.Content[keywordLength..].Trim();
        if (TemplateReferenceText.StartsBlueprint(text))
        {
            if (!TemplateReferenceText.TryParseBlueprint(text, out var reference))
            {
                context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Invalid template reference '{line.Content}' - expected '{form}'", line.Location);
                context.SkipBlock(line.Indent);
                return null;
            }

            if (reference.Version is not null && !TemplateVersionRange.IsValid(reference.Version))
            {
                context.Error(
                    DiagnosticCodes.InvalidTemplateVersionRange,
                    $"The template version range '{reference.Version}' is not one Scene understands - use '*', an exact version or a ^, ~, >=, >, <= or < range such as \"^1.2\"",
                    line.Location);
            }

            return (reference.Blueprint, reference.Name, reference.Version);
        }

        var own = OwnReferenceRegex().Match(text);
        if (!own.Success)
        {
            var hint = text.Contains(" version ", StringComparison.Ordinal) ? "; a version range only applies to a blueprint template" : string.Empty;
            context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Invalid template reference '{line.Content}' - expected '{form}'{hint}", line.Location);
            context.SkipBlock(line.Indent);
            return null;
        }

        return (null, own.Groups[1].Value, null);
    }

    static ShellOptionSyntax? ParseOption(ParserContext context, SourceLine line)
    {
        var match = OptionRegex().Match(line.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidProfileRootTemplate, $"Invalid option '{line.Content}' - expected 'option <name> <value>' where the value is a word, a quoted string, a number, true or false", line.Location);
            return null;
        }

        var name = match.Groups[1].Value;
        if (match.Groups[2].Success)
        {
            return new(name, StringLiteral.Unescape(match.Groups[2].Value), ShellOptionValueKind.Text, line.Location);
        }

        var value = match.Groups[3].Value;
        var kind = ShellOptionValueKind.Word;
        if (string.Equals(value, "true", StringComparison.Ordinal) || string.Equals(value, "false", StringComparison.Ordinal))
        {
            kind = ShellOptionValueKind.Flag;
        }
        else if (NumberRegex().IsMatch(value))
        {
            kind = ShellOptionValueKind.Number;
        }

        return new(name, value, kind, line.Location);
    }

    [GeneratedRegex(@"^([A-Za-z_]\w*(?:\.\w+)*)$", RegexOptions.NonBacktracking)]
    private static partial Regex OwnReferenceRegex();

    [GeneratedRegex("^option\\s+([A-Za-z_]\\w*)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|(-?\\d+(?:\\.\\d+)?|[A-Za-z_][\\w.-]*))$", RegexOptions.NonBacktracking)]
    private static partial Regex OptionRegex();

    [GeneratedRegex(@"^-?\d+(?:\.\d+)?$", RegexOptions.NonBacktracking)]
    private static partial Regex NumberRegex();

    [GeneratedRegex(@"^color\s+scheme\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex ColorSchemeRegex();

    [GeneratedRegex("^accent\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.NonBacktracking)]
    private static partial Regex AccentRegex();

    [GeneratedRegex(@"^corners\s+([A-Za-z_]\w*)$", RegexOptions.NonBacktracking)]
    private static partial Regex CornersRegex();

    [GeneratedRegex(@"^font\s+([A-Za-z0-9][A-Za-z0-9-]*)$", RegexOptions.NonBacktracking)]
    private static partial Regex FontRegex();

    [GeneratedRegex("^name\\s+\"(" + StringLiteral.BodyPattern + ")\"$", RegexOptions.NonBacktracking)]
    private static partial Regex IdentityNameRegex();

    [GeneratedRegex("^logo\\s+(initials|icon|image)\\s+(?:\"(" + StringLiteral.BodyPattern + ")\"|([A-Za-z_][\\w-]*))$", RegexOptions.NonBacktracking)]
    private static partial Regex LogoRegex();

    [GeneratedRegex(@"^show\s+name\s+(true|false)$", RegexOptions.NonBacktracking)]
    private static partial Regex ShowNameRegex();
}
