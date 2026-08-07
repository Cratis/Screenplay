// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a top level <c>authentication</c> block declaring the identity providers of the application.
/// </summary>
/// <param name="Providers">The <see cref="AuthenticationProviderSyntax">providers</see> declared in the block.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AuthenticationSyntax(IEnumerable<AuthenticationProviderSyntax> Providers, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>provider &lt;Name&gt; [name &lt;Alias&gt;]</c> within an <c>authentication</c> block.
/// </summary>
/// <param name="Name">The kind of provider - <c>EntraId</c>, <c>GitHub</c>, <c>OpenId</c>.</param>
/// <param name="Alias">The name this provider goes by, when it needs one to be distinguishable.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// A provider says which identity provider the application signs users in with, and nothing about how it is
/// configured. How to reach it - authority, client id, the secret that goes with it - is what running the
/// application needs to know rather than what the application <em>is</em>, so it belongs to whatever runs
/// the document, which looks the provider up by name.
/// <para>
/// <see cref="Alias"/> exists because a generic provider can appear more than once: two <c>OpenId</c>
/// providers are two different identity providers, and only a name told apart tells them apart.
/// </para>
/// </remarks>
public record AuthenticationProviderSyntax(string Name, string? Alias, SourceLocation Location) : SyntaxNode(Location)
{
    /// <summary>
    /// Gets what the provider is known by - its <see cref="Alias"/> when it has one, otherwise its <see cref="Name"/>.
    /// </summary>
    public string Identity => Alias ?? Name;
}
