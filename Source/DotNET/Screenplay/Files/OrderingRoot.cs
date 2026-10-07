// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Languages;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Chooses a presentation root without changing resolution or merge order.
/// </summary>
internal static class OrderingRoot
{
    internal static string? Select(IReadOnlyList<string> roots, IReadOnlyList<PlacedPlayDocument> documents, IScreenplayLanguageRegistry languages, IReadOnlyDictionary<string, IReadOnlyList<DiscoveredFileImport>>? imports = null)
    {
        var normalized = roots.Select(PlayGlob.Normalize).Distinct(StringComparer.Ordinal).ToArray();
        if (normalized.Length == 1)
        {
            return normalized[0];
        }

        imports ??= documents.ToDictionary(document => document.Path, document => ScreenplayCompiler.DiscoverImports(document.Source, document.Path, languages), StringComparer.Ordinal);
        if (imports.TryGetValue("application.play", out var applicationImports) && applicationImports.Count > 0)
        {
            return "application.play";
        }

        var imported = new HashSet<string>(StringComparer.Ordinal);
        foreach (var (path, entries) in imports)
        {
            foreach (var entry in entries)
            {
                var pattern = PlayGlob.Resolve(path, entry.Import.Pattern);
                imported.UnionWith(documents.Where(document => document.Path != path && PlayGlob.IsMatch(pattern, document.Path)).Select(document => document.Path));
            }
        }

        var candidates = imports.Where(entry => entry.Value.Count > 0 && !imported.Contains(entry.Key)).Select(entry => entry.Key).ToArray();

        return candidates.Length == 1 ? candidates[0] : null;
    }
}
