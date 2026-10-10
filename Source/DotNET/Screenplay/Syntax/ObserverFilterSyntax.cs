// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an observer's event source and optional stream filter.
/// </summary>
/// <param name="EventSource">The event source name.</param>
/// <param name="Stream">The optional source-owned stream name.</param>
/// <param name="Location">The location of the from directive.</param>
public record ObserverFilterSyntax(string EventSource, string? Stream, SourceLocation Location) : SyntaxNode(Location);
