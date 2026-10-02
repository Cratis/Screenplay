// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Represents a file import found in a document, with where in the document it is written.
/// </summary>
/// <param name="Scope">The module and feature names around the import inside the document, outermost first. Empty at the top level.</param>
/// <param name="StartsAtModule">Whether the outermost name is a <c>module</c> written at the document's top level, rather than a <c>feature</c>.</param>
/// <param name="Import">The <see cref="FileImportSyntax"/>.</param>
public record DiscoveredFileImport(IReadOnlyList<string> Scope, bool StartsAtModule, FileImportSyntax Import)
{
    /// <summary>
    /// Gets where the import places what it imports, given where the document itself is placed.
    /// </summary>
    /// <param name="document">The <see cref="PlayPlacement"/> of the document holding the import.</param>
    /// <returns>The placement of the imported files, or <c>null</c> when the import cannot place anything from there.</returns>
    /// <remarks>
    /// A top level <c>feature</c> only means something once the document is placed in a module, so until then
    /// its imports place nothing - the document reports the misplaced feature itself if it never is. A top level
    /// <c>module</c> is the module's own declaration in a whole document, a restatement that joins the placement
    /// in a document placed in that module, and nothing anywhere else.
    /// </remarks>
    public PlayPlacement? PlacementFrom(PlayPlacement document)
    {
        if (Scope.Count == 0)
        {
            return document;
        }

        if (!StartsAtModule)
        {
            return document.IsDocument ? null : document.Within(Scope);
        }

        if (document.IsDocument)
        {
            return new(Scope);
        }

        return document.Scope.Count == 1 && string.Equals(document.Scope[0], Scope[0], StringComparison.Ordinal)
            ? document.Within(Scope.Skip(1))
            : null;
    }
}
