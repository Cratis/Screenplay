// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses top level <c>authentication</c> blocks - named identity providers with free-form settings.
/// </summary>
internal static partial class AuthenticationParser
{
    /// <summary>
    /// Parses an authentication block from its already consumed header line.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to parse in.</param>
    /// <param name="header">The consumed <see cref="SourceLine"/> holding the <c>authentication</c> header.</param>
    /// <param name="existing">The authentication block already parsed for the document, when there is one.</param>
    /// <returns>The parsed <see cref="AuthenticationSyntax"/>, or <paramref name="existing"/> when the block is invalid or a duplicate.</returns>
    public static AuthenticationSyntax? Parse(ParserContext context, SourceLine header, AuthenticationSyntax? existing)
    {
        if (header.Content != "authentication")
        {
            context.Error($"Invalid authentication declaration '{header.Content}' - expected 'authentication'", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error("The document already declares an authentication block - a document can have at most one", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        var providers = new List<AuthenticationProviderSyntax>();
        while (context.TryPeekChild(header.Indent, out var line))
        {
            context.Reader.TakeSignificant();
            var match = ProviderRegex().Match(line.Content);
            if (!match.Success)
            {
                context.Error($"Invalid provider declaration '{line.Content}' - expected 'provider <Name>'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            providers.Add(new(match.Groups[1].Value, ParseSettings(context, line), line.Location));
        }

        return new(providers, header.Location);
    }

    static List<AuthenticationSettingSyntax> ParseSettings(ParserContext context, SourceLine provider)
    {
        var settings = new List<AuthenticationSettingSyntax>();
        while (context.TryPeekChild(provider.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            var match = SettingRegex().Match(child.Content);
            if (!match.Success)
            {
                context.Error($"Invalid provider setting '{child.Content}' - expected '<name> <value>'", child.Location);
                continue;
            }

            settings.Add(new(match.Groups[1].Value, ExpressionParser.ParseMappingSource(match.Groups[2].Value, child.Location), child.Location));
        }

        return settings;
    }

    [GeneratedRegex(@"^provider\s+([A-Za-z_]\w*)$", RegexOptions.None, 1000)]
    private static partial Regex ProviderRegex();

    [GeneratedRegex(@"^([A-Za-z_]\w*)\s+(.+)$", RegexOptions.None, 1000)]
    private static partial Regex SettingRegex();
}
