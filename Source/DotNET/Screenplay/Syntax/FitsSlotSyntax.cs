// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a <c>fits slot &lt;name&gt;</c> directive inside a screen template.
/// </summary>
/// <param name="Name">The slot on the parent structure.</param>
/// <param name="Location">The directive's location in the source text.</param>
public record FitsSlotSyntax(string Name, SourceLocation Location) : SyntaxNode(Location);
