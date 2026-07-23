// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>domain</c> declaration - the domain a document belongs to.
/// </summary>
/// <param name="Name">The qualified name of the domain.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record DomainSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);
