// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Syntax;

/// <summary>
/// Represents an <c>import "&lt;pattern&gt;"</c> of other <c>.play</c> files.
/// </summary>
/// <param name="Pattern">The path or glob pattern, relative to the directory of the importing file.</param>
/// <param name="Location">The <see cref="SourceLocation"/> where the node starts in the source text.</param>
/// <remarks>
/// Where the import is written decides where what it imports belongs. At the top level of a document it brings
/// in whole documents; inside a <c>module</c> or <c>feature</c> it places each imported file there, so the
/// file's top level is that module's or feature's body.
/// </remarks>
public record FileImportSyntax(string Pattern, SourceLocation Location) : SyntaxNode(Location);
