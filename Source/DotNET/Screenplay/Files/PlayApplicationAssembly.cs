// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Assembles one application from documents - following their imports, parsing each where it is placed, and
/// merging the lot.
/// </summary>
internal static class PlayApplicationAssembly
{
    /// <summary>
    /// Compiles the application the root documents and everything they import make up.
    /// </summary>
    /// <param name="compiler">The <see cref="IScreenplayCompiler"/> to parse each document with.</param>
    /// <param name="roots">The portable paths of the root documents.</param>
    /// <param name="source">The <see cref="IPlayDocumentSource"/> the documents come from.</param>
    /// <param name="allowUnresolvedPersonaPolicies">Whether draft authoring may retain unresolved persona references as warnings.</param>
    /// <returns>The documents that make up the application, and the <see cref="CompilationResult{TResult}"/> of it as a whole.</returns>
    public static (IReadOnlyList<PlacedPlayDocument> Documents, CompilationResult<ApplicationSyntax> Result) Compile(
        IScreenplayCompiler compiler,
        IEnumerable<string> roots,
        IPlayDocumentSource source,
        bool allowUnresolvedPersonaPolicies = false)
    {
        var (documents, diagnostics) = PlayImports.Resolve(roots, source);
        var parsed = documents.Select(document => document.Placement.IsDocument
            ? compiler.Parse(document.Source, document.Path)
            : compiler.Parse(document.Source, document.Path, document.Placement));
        var merged = PlayFolderMerge.Merge([.. parsed], allowUnresolvedPersonaPolicies);
        return (documents, merged with { Diagnostics = [.. diagnostics, .. merged.Diagnostics] });
    }
}
