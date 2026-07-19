// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an <c>event</c> declaration - an immutable, past tense fact.
/// </summary>
/// <param name="Name">The name of the event.</param>
/// <param name="Properties">The <see cref="PropertySyntax">properties</see> the event carries.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record EventSyntax(string Name, IEnumerable<PropertySyntax> Properties, SourceLocation Location) : SyntaxNode(Location);
