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
/// Represents a named <c>provider</c> within an <c>authentication</c> block.
/// </summary>
/// <param name="Name">The name of the provider.</param>
/// <param name="Settings">The <see cref="AuthenticationSettingSyntax">settings</see> of the provider.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AuthenticationProviderSyntax(string Name, IEnumerable<AuthenticationSettingSyntax> Settings, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a free-form setting of an authentication provider, such as <c>clientId $secrets.azureAdClientId</c>.
/// </summary>
/// <param name="Name">The name of the setting.</param>
/// <param name="Value">The <see cref="ExpressionSyntax"/> providing the value.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record AuthenticationSettingSyntax(string Name, ExpressionSyntax Value, SourceLocation Location) : SyntaxNode(Location);
