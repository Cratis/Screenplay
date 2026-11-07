// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Migrations;

/// <summary>
/// Represents one document a migration step rewrites: its text, where it sits in the application and the tree the
/// parser read from it, retired forms included.
/// </summary>
/// <param name="Path">The path of the document, as locations in it report.</param>
/// <param name="Source">The source text the tree was read from.</param>
/// <param name="Placement">Where the document's content sits - the application, or the module or feature an import puts it in.</param>
/// <param name="Syntax">The <see cref="ApplicationSyntax"/> read from <paramref name="Source"/>.</param>
public sealed record DocumentSyntax(string Path, string Source, PlayPlacement Placement, ApplicationSyntax Syntax);
