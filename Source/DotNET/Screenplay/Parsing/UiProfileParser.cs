// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Text;

namespace Cratis.Screenplay.Parsing;

internal static partial class UiProfileParser
{
    public static UiProfileSyntax Parse(ParserContext context, SourceLine header)
    {
        var match = HeaderRegex().Match(header.Content);
        if (!match.Success)
        {
            context.Error(DiagnosticCodes.InvalidUiProfileDeclaration, $"Invalid ui profile declaration '{header.Content}' - expected 'ui profile <Name>'", header.Location);
            context.SkipBlock(header.Indent);
            return new(LineText.FirstWord(header.Content), [], null, [], header.Location);
        }

        var name = match.Groups[1].Value;
        var platforms = new List<string>();
        string? defaultSizeClass = null;
        var packages = new List<string>();
        string? theme = null;
        string? layout = null;
        var hasTargetPlatform = false;
        var hasTargetSize = false;
        var hasPackagesBlock = false;
        var hasTheme = false;
        var hasLayout = false;

        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            switch (LineText.FirstWord(line.Content))
            {
                case "target":
                    ParseTarget(context, line, platforms, ref hasTargetPlatform, ref hasTargetSize, ref defaultSizeClass);
                    break;
                case "packages":
                    if (hasPackagesBlock)
                    {
                        context.Error(DiagnosticCodes.UnknownUiProfileDirective, "This ui profile already declares a packages block - a profile can have at most one", line.Location);
                        context.SkipBlock(line.Indent);
                        break;
                    }

                    hasPackagesBlock = true;
                    ParsePackages(context, line, packages);
                    break;
                case "theme":
                    if (hasTheme)
                    {
                        context.Error(DiagnosticCodes.DuplicateProfileTheme, "This ui profile already declares a theme - a profile can have at most one", line.Location);
                        break;
                    }

                    var themeMatch = ThemeReferenceRegex().Match(line.Content);
                    if (!themeMatch.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidProfileTheme, $"Invalid theme reference '{line.Content}' - expected 'theme <Name>'", line.Location);
                        break;
                    }

                    hasTheme = true;
                    theme = themeMatch.Groups[1].Value;
                    break;
                case "layout":
                    if (hasLayout)
                    {
                        context.Error(DiagnosticCodes.DuplicateProfileLayout, "This ui profile already declares a layout - a profile can have at most one", line.Location);
                        break;
                    }

                    var layoutMatch = LayoutReferenceRegex().Match(line.Content);
                    if (!layoutMatch.Success)
                    {
                        context.Error(DiagnosticCodes.InvalidProfileLayout, $"Invalid layout reference '{line.Content}' - expected 'layout <Name>'", line.Location);
                        break;
                    }

                    hasLayout = true;
                    layout = layoutMatch.Groups[1].Value;
                    break;
                default:
                    context.Error(DiagnosticCodes.UnknownUiProfileDirective, $"Unexpected '{LineText.FirstWord(line.Content)}' in ui profile body - expected target, packages, theme or layout", line.Location);
                    context.SkipBlock(line.Indent);
                    break;
            }
        }

        return new(name, platforms, defaultSizeClass, packages, header.Location, theme, layout);
    }

    static void ParseTarget(ParserContext context, SourceLine line, List<string> platforms, ref bool hasTargetPlatform, ref bool hasTargetSize, ref string? defaultSizeClass)
    {
        var platformMatch = TargetPlatformRegex().Match(line.Content);
        if (platformMatch.Success)
        {
            if (hasTargetPlatform)
            {
                context.Error(DiagnosticCodes.DuplicateTargetDeclaration, "This ui profile already declares 'target platform' - at most one is allowed", line.Location);
                return;
            }

            hasTargetPlatform = true;
            platforms.AddRange(platformMatch.Groups[1].Value.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries));
            return;
        }

        var sizeMatch = TargetSizeRegex().Match(line.Content);
        if (sizeMatch.Success)
        {
            if (hasTargetSize)
            {
                context.Error(DiagnosticCodes.DuplicateTargetDeclaration, "This ui profile already declares 'target size' - at most one is allowed", line.Location);
                return;
            }

            hasTargetSize = true;
            defaultSizeClass = sizeMatch.Groups[1].Value;
            return;
        }

        context.Error(
            DiagnosticCodes.InvalidTargetDeclaration,
            $"Invalid target declaration '{line.Content}' - expected 'target platform <Platform>[, <Platform>...]' or 'target size <SizeClass>'",
            line.Location);
    }

    static void ParsePackages(ParserContext context, SourceLine header, List<string> packages)
    {
        var seen = new HashSet<string>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            if (!PackageNameRegex().IsMatch(line.Content))
            {
                context.Error(DiagnosticCodes.InvalidPackageName, $"Invalid package name '{line.Content}' - expected an identifier, optionally dotted", line.Location);
                continue;
            }

            if (!seen.Add(line.Content))
            {
                context.Error(DiagnosticCodes.DuplicatePackageDeclaration, $"Package '{line.Content}' is already declared in this profile's packages block", line.Location);
                continue;
            }

            packages.Add(line.Content);
        }
    }

    [GeneratedRegex(@"^ui\s+profile\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex HeaderRegex();

    [GeneratedRegex(@"^target\s+platform\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex TargetPlatformRegex();

    [GeneratedRegex(@"^target\s+size\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex TargetSizeRegex();

    [GeneratedRegex(@"^[A-Za-z_]\w*(?:\.[A-Za-z_]\w*)*$", RegexOptions.None, 1000)]
    private static partial Regex PackageNameRegex();

    [GeneratedRegex(@"^theme\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ThemeReferenceRegex();

    [GeneratedRegex(@"^layout\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex LayoutReferenceRegex();
}
