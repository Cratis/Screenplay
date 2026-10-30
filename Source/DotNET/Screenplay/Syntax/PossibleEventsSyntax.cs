// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>Declares the closed set of event types a handler may return.</summary>
/// <param name="Events">The possible events, in authoring order.</param>
/// <param name="Location">The location of the declaration.</param>
public record PossibleEventsSyntax(IEnumerable<PossibleEventSyntax> Events, SourceLocation Location) : SyntaxNode(Location);
