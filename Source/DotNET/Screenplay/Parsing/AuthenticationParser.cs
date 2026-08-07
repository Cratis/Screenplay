// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.RegularExpressions;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Parsing;

/// <summary>
/// Parses top level <c>authentication</c> blocks - the identity providers the application signs users in with.
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
            context.Error(DiagnosticCodes.InvalidAuthenticationDeclaration, $"Invalid authentication declaration '{header.Content}' - expected 'authentication'", header.Location);
            context.SkipBlock(header.Indent);
            return existing;
        }

        if (existing is not null)
        {
            context.Error(DiagnosticCodes.DuplicateAuthentication, "The document already declares an authentication block - a document can have at most one", header.Location);
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
                context.Error(DiagnosticCodes.InvalidProviderDeclaration, $"Invalid provider declaration '{line.Content}' - expected 'provider <Name>' or 'provider <Name> name <Alias>'", line.Location);
                context.SkipBlock(line.Indent);
                continue;
            }

            var alias = match.Groups[2];
            providers.Add(new(match.Groups[1].Value, alias.Success ? alias.Value : null, line.Location));
            RejectBody(context, line);
        }

        return new(providers, header.Location);
    }

    /// <summary>
    /// Reports anything indented under a provider, which used to be where its configuration went.
    /// </summary>
    /// <param name="context">The <see cref="ParserContext"/> to report diagnostics to.</param>
    /// <param name="provider">The <see cref="SourceLine"/> holding the provider declaration.</param>
    /// <remarks>
    /// A provider carries no configuration - what it takes to reach one is what running the application
    /// needs to know, not what the application is - so a body under it is a document saying something the
    /// language deliberately does not express, and silently dropping it would hide that.
    /// </remarks>
    static void RejectBody(ParserContext context, SourceLine provider)
    {
        while (context.TryPeekChild(provider.Indent, out var child))
        {
            context.Reader.TakeSignificant();
            context.Error(
                DiagnosticCodes.ProviderWithConfiguration,
                $"Unexpected '{child.Content}' under a provider - how to reach a provider is configured where the application runs, not in the document",
                child.Location);
            context.SkipBlock(child.Indent);
        }
    }

    [GeneratedRegex(@"^provider\s+([A-Za-z_]\w*)(?:\s+name\s+([A-Za-z_]\w*))?$", RegexOptions.None, 1000)]
    private static partial Regex ProviderRegex();
}
