// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents a handler's implementation wrapper, independent of its authoritative payload.
/// </summary>
/// <param name="Hints">The ordered implementation hints.</param>
/// <param name="Location">The source location of the wrapper.</param>
public record ImplementationSyntax(IEnumerable<ImplementationHintSyntax> Hints, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents one decoded, nonblank implementation hint with its own comment anchor.
/// </summary>
/// <param name="Text">The authored hint text, without trimming.</param>
/// <param name="Location">The source location of the hint.</param>
public record ImplementationHintSyntax(string Text, SourceLocation Location) : SyntaxNode(Location);
