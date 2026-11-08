// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Checks that the sketches, HTML mockups and images screens carry exist beside the model.
/// </summary>
/// <remarks>
/// A visual is never read into the workspace the way a code attachment is: it may be binary and it never
/// changes behavior. Only its presence is checked, so a missing file is reported where the model is opened.
/// </remarks>
public static class ScreenVisualFiles
{
    /// <summary>
    /// Gets every visual the documents' screens carry.
    /// </summary>
    /// <param name="documents">The source documents.</param>
    /// <returns>The visuals, in document order.</returns>
    public static ImmutableArray<ScreenVisualSyntax> In(ImmutableArray<SemanticSourceDocument> documents) =>
        In(documents, ScreenplayLanguageRegistry.Default);

    /// <summary>
    /// Gets every visual the screens of an application carry.
    /// </summary>
    /// <param name="application">The <see cref="ApplicationSyntax"/>.</param>
    /// <returns>The visuals, in source order.</returns>
    public static ImmutableArray<ScreenVisualSyntax> In(ApplicationSyntax application)
    {
        var visuals = new Visuals();
        visuals.VisitApplication(application);
        return [.. visuals.Found];
    }

    /// <summary>
    /// Checks that every visual the documents' screens carry is a file beneath the model root.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <param name="documents">The source documents.</param>
    /// <returns>A <see cref="DiagnosticCodes.ScreenVisualMissing"/> warning per visual that is not there.</returns>
    public static ImmutableArray<Diagnostic> Check(string root, ImmutableArray<SemanticSourceDocument> documents) =>
        Check(root, In(documents));

    /// <summary>
    /// Checks that every visual is a file beneath the model root.
    /// </summary>
    /// <param name="root">The model root directory.</param>
    /// <param name="visuals">The visuals to check.</param>
    /// <returns>A <see cref="DiagnosticCodes.ScreenVisualMissing"/> warning per visual that is not there.</returns>
    public static ImmutableArray<Diagnostic> Check(string root, IEnumerable<ScreenVisualSyntax> visuals) =>
        [.. visuals.Select(visual => Missing(root, visual)).OfType<Diagnostic>()];

    internal static ImmutableArray<ScreenVisualSyntax> In(ImmutableArray<SemanticSourceDocument> documents, IScreenplayLanguageRegistry languages)
    {
        var visuals = new Visuals();
        var texts = new Dictionary<string, string>(StringComparer.Ordinal);
        foreach (var document in documents)
        {
            texts[document.DisplayPath] = document.Text;
            var parsed = new ScreenplayCompiler(languages).Parse(document.Text, document.DisplayPath);
            if (parsed.Value is not null) visuals.VisitApplication(parsed.Value);
        }

        // A placed document is not whole on its own, so the set is also assembled the way a folder is.
        var (_, assembled) = PlayApplicationAssembly.Compile(new ScreenplayCompiler(languages), texts.Keys, new InMemoryPlayDocumentSource(texts));
        if (assembled.Value is not null)
        {
            var placed = new Visuals();
            placed.VisitApplication(assembled.Value);
            var known = visuals.Found.Select(Identify).ToHashSet(StringComparer.Ordinal);
            visuals.Found.AddRange(placed.Found.Where(visual => !known.Contains(Identify(visual))));
        }

        return [.. visuals.Found];
    }

    static Diagnostic? Missing(string root, ScreenVisualSyntax visual)
    {
        var keyword = Parsing.ScreenVisualParser.Keyword(visual.Kind);
        if (!AttachmentFiles.TryNormalize(visual.File.Path, out var normalized, out var reason))
        {
            return Diagnostic.Warning(DiagnosticCodes.ScreenVisualMissing, $"The {keyword} '{visual.File.Path}' is {reason}", visual.Location);
        }

        var path = Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar));
        return File.Exists(path)
            ? null
            : Diagnostic.Warning(DiagnosticCodes.ScreenVisualMissing, $"The {keyword} '{visual.File.Path}' does not exist beside the model", visual.Location);
    }

    static string Identify(ScreenVisualSyntax visual) => $"{visual.Location.Path}({visual.Location.Line},{visual.Location.Column}):{visual.File.Path}";

    sealed class Visuals : ScreenplaySyntaxWalker
    {
        internal List<ScreenVisualSyntax> Found { get; } = [];

        public override void VisitNode(SyntaxNode node)
        {
            if (node is ScreenVisualSyntax visual) Found.Add(visual);
        }
    }
}
