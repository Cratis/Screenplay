// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents a document of an application together with where its top level belongs.
/// </summary>
/// <param name="Path">The portable path of the document.</param>
/// <param name="Source">The source text of the document.</param>
/// <param name="Placement">The <see cref="PlayPlacement"/> its imports settled on.</param>
public record PlacedPlayDocument(string Path, string Source, PlayPlacement Placement);
