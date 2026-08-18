// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an inline fenced code block in a specific language, such as <c>csharp</c> or <c>react</c>.
/// </summary>
/// <param name="Language">The language tag of the block.</param>
/// <param name="Code">The verbatim code inside the fence.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record CodeBlockSyntax(string Language, string Code, SourceLocation Location) : SyntaxNode(Location);

/// <summary>
/// Represents a <c>file</c> directive referencing an external file by relative path.
/// </summary>
/// <param name="Path">The relative path being referenced.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
public record FileReferenceSyntax(string Path, SourceLocation Location) : SyntaxNode(Location);
