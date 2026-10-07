// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// One original declaration occurrence or import match on the first successful route to a presentation rank.
/// The import node and its location identify the document-local occurrence; no trace enters semantic bytes.
/// </summary>
internal sealed record AuthoredOrderStep(string Path, SyntaxNode Node, SourceLocation Location, string? TargetPath, int? MatchIndex);
