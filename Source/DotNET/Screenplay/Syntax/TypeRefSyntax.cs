// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a reference to a type, such as <c>Money</c>, <c>InvoiceLine[]</c> or <c>String?</c>.
/// </summary>
/// <param name="Name">The name of the type being referenced.</param>
/// <param name="IsCollection">Whether the reference is a collection (<c>[]</c> suffix).</param>
/// <param name="IsOptional">Whether the reference is optional (<c>?</c> suffix).</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record TypeRefSyntax(string Name, bool IsCollection, bool IsOptional, SourceLocation Location) : SyntaxNode(Location);
