// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Parsing;
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
        bool allowUnresolvedPersonaPolicies = false) =>
        Compile(compiler, roots, source, (compiler as ILanguageRegistryOwner)?.Languages ?? ScreenplayLanguageRegistry.Default, allowUnresolvedPersonaPolicies);

    internal static (IReadOnlyList<PlacedPlayDocument> Documents, CompilationResult<ApplicationSyntax> Result) Compile(
        IScreenplayCompiler compiler,
        IEnumerable<string> roots,
        IPlayDocumentSource source,
        IScreenplayLanguageRegistry languages,
        bool allowUnresolvedPersonaPolicies = false)
    {
        var rootPaths = roots.ToArray();
        var (documents, diagnostics) = PlayImports.Resolve(rootPaths, source, languages);
        var candidates = (compiler as ICommandStreamCandidateParser)?.CaptureCandidates(documents.Where(document => document.IsPlacementResolved)
            .Select(document => (SourceLineSplitter.Split(document.Source, path: document.Path), document.Placement)));
        var parsed = documents.Select(document =>
        {
            CompilationResult<ApplicationSyntax> result;
            if (compiler is ICommandStreamCandidateParser native) result = native.ParseWithCandidates(document.Source, document.Path, document.Placement, candidates!);
            else if (document.Placement.IsDocument) result = compiler.Parse(document.Source, document.Path);
            else result = compiler.Parse(document.Source, document.Path, document.Placement);
            return !document.IsPlacementResolved && result.Value is { } application
                ? result with { Value = application with { EventSources = [] } }
                : result;
        });
        var merged = PlayFolderMerge.Merge([.. parsed], allowUnresolvedPersonaPolicies);
        var orderingRoot = OrderingRoot.Select(rootPaths, documents, languages);
        var timeline = orderingRoot is not null && merged.Value is { } application
            ? TimelineOrder.In(application, AuthoredOrder.Record([orderingRoot], documents, languages))
            : [];

        return (documents, merged with { Diagnostics = [.. diagnostics, .. merged.Diagnostics, .. timeline] });
    }
}
