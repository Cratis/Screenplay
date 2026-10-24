// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a specification <c>given time zone "&lt;id&gt;"</c> declaration.
/// </summary>
/// <param name="Id">The IANA time zone id as written.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record SpecificationTimeZoneSyntax(string Id, SourceLocation Location) : SyntaxNode(Location);
