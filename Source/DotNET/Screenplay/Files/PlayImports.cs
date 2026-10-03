// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Settles which documents make up an application and where each one's top level belongs, by following the
/// file imports from a set of root documents.
/// </summary>
/// <remarks>
/// <para>
/// A root document is a whole document. Every file an import matches joins the application once, however many
/// imports match it - so a composite file at the root importing <c>**/*.play</c> and one per module importing its
/// own folder can both exist. An import placed inside a module or feature places what it imports there.
/// </para>
/// <para>
/// When several imports place the same file, the deepest placement wins: a file imported at the top level by the
/// root and into <c>feature Ordering.Orders</c> by its feature belongs to the feature. Two placements where
/// neither lies inside the other are a conflict, and placements that keep deepening are an import cycle.
/// </para>
/// <para>
/// A file's placement depends on the placements of the files importing it, which depend on theirs in turn, so it is
/// recomputed from every importer's current placement until nothing changes. The order files are found in therefore
/// never decides where one belongs.
/// </para>
/// </remarks>
public static class PlayImports
{
    /// <summary>
    /// How deep a placement may get before the imports behind it are taken to be a cycle.
    /// </summary>
    public const int MaximumDepth = 32;

    /// <summary>
    /// Resolves the documents of an application.
    /// </summary>
    /// <param name="roots">The portable paths of the root documents, each a whole document.</param>
    /// <param name="source">The <see cref="IPlayDocumentSource"/> to find and read documents through.</param>
    /// <returns>Every document - the roots in the order given, then what they import - with the diagnostics resolving them produced.</returns>
    public static (IReadOnlyList<PlacedPlayDocument> Documents, IReadOnlyList<Diagnostic> Diagnostics) Resolve(
        IEnumerable<string> roots,
        IPlayDocumentSource source)
    {
        var resolution = new Resolution(source);
        foreach (var root in roots.Select(PlayGlob.Normalize).Distinct(StringComparer.Ordinal))
        {
            resolution.AddRoot(root);
        }

        resolution.Settle();
        return (resolution.Documents(), resolution.Diagnostics);
    }

    sealed class Resolution(IPlayDocumentSource source)
    {
        // Found order is the order documents are returned in: roots as given, then what they import.
        readonly List<string> _found = [];
        readonly HashSet<string> _roots = new(StringComparer.Ordinal);
        readonly Dictionary<string, string> _sources = new(StringComparer.Ordinal);
        readonly Dictionary<string, IReadOnlyList<(DiscoveredFileImport Import, IReadOnlyList<string> Targets)>> _imports = new(StringComparer.Ordinal);

        // What each import currently contributes to a file's placement, keyed by the importing file and the import.
        readonly Dictionary<string, Dictionary<(string File, int Import), PlayPlacement>> _contributions = new(StringComparer.Ordinal);
        readonly Dictionary<string, PlayPlacement> _placements = new(StringComparer.Ordinal);
        readonly Queue<string> _pending = new();
        readonly Dictionary<string, int> _changes = new(StringComparer.Ordinal);
        readonly List<Diagnostic> _diagnostics = [];
        readonly HashSet<string> _unresolved = new(StringComparer.Ordinal);

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        public void AddRoot(string root)
        {
            _roots.Add(root);
            Find(root);
        }

        public void Settle()
        {
            // Placements only deepen, and none is allowed past the maximum depth, so this ends.
            while (_pending.Count > 0)
            {
                var file = _pending.Dequeue();
                Propagate(file);
            }

            ReportConflicts();

            // A child cannot acquire an authoritative scope through an unresolved importer.
            var unresolved = new Queue<string>(_unresolved);
            while (unresolved.TryDequeue(out var importer))
            {
                foreach (var target in _imports[importer].SelectMany(import => import.Targets))
                {
                    if (_unresolved.Add(target)) unresolved.Enqueue(target);
                }
            }
        }

        public IReadOnlyList<PlacedPlayDocument> Documents() =>
            [.. _found.Select(path => new PlacedPlayDocument(path, _sources[path], Placement(path) ?? PlayPlacement.Document)
            {
                IsPlacementResolved = !_unresolved.Contains(path)
            })];

        void Find(string file)
        {
            if (_sources.ContainsKey(file))
            {
                return;
            }

            _found.Add(file);
            _sources[file] = source.Read(file);
            _imports[file] = [.. ScreenplayCompiler.DiscoverImports(_sources[file], file).Select(import => (import, Targets(file, import)))];
            _pending.Enqueue(file);
        }

        List<string> Targets(string file, DiscoveredFileImport import)
        {
            var resolved = PlayGlob.Resolve(file, import.Import.Pattern);
            var matches = source.FilesBeneath(PlayGlob.StaticFolder(resolved))
                .Select(PlayGlob.Normalize)
                .Where(path => !string.Equals(path, file, StringComparison.Ordinal) && PlayGlob.IsMatch(resolved, path))
                .Distinct(StringComparer.Ordinal)
                .Order(StringComparer.Ordinal)
                .ToList();

            if (matches.Count == 0)
            {
                Report(PlayGlob.HasWildcard(import.Import.Pattern)
                    ? Diagnostic.Warning(DiagnosticCodes.FileImportMatchesNothing, $"Import '{import.Import.Pattern}' matches no .play file", import.Import.Location)
                    : Diagnostic.Error(DiagnosticCodes.ImportedFileNotFound, $"Imported file '{import.Import.Pattern}' does not exist", import.Import.Location));
            }

            return matches;
        }

        void Propagate(string file)
        {
            var placement = Placement(file);
            var imports = _imports[file];
            for (var index = 0; index < imports.Count; index++)
            {
                var (import, targets) = imports[index];
                var target = placement is null ? null : import.PlacementFrom(placement);
                if (target?.Scope.Count > MaximumDepth)
                {
                    Report(Diagnostic.Error(DiagnosticCodes.ImportCycle, $"Import '{import.Import.Pattern}' places files deeper than {MaximumDepth} levels - the imports form a cycle", import.Import.Location));
                    _unresolved.Add(file);
                    target = null;
                }

                foreach (var path in targets)
                {
                    Find(path);
                    Contribute(path, (file, index), target);
                }
            }
        }

        void Contribute(string file, (string File, int Import) by, PlayPlacement? placement)
        {
            if (!_contributions.TryGetValue(file, out var contributions))
            {
                contributions = [];
                _contributions[file] = contributions;
            }

            var changed = placement is null
                ? contributions.Remove(by)
                : !contributions.TryGetValue(by, out var current) || !current.Equals(placement);
            if (placement is not null)
            {
                contributions[by] = placement;
            }

            if (changed)
            {
                Recompute(file);
            }
        }

        void Recompute(string file)
        {
            var placement = Deepest(file);
            if (_placements.TryGetValue(file, out var current) ? !current.Equals(placement) : placement is not null)
            {
                // A placement that keeps changing is held up by imports that feed back into themselves.
                _changes[file] = _changes.GetValueOrDefault(file) + 1;
                if (_changes[file] > MaximumDepth)
                {
                    _unresolved.Add(file);
                    foreach (var import in _imports[file].Select(entry => entry.Import.Import))
                    {
                        Report(Diagnostic.Error(DiagnosticCodes.ImportCycle, $"Import '{import.Pattern}' is part of imports that keep placing each other - the imports form a cycle", import.Location));
                    }

                    return;
                }

                if (placement is null)
                {
                    _placements.Remove(file);
                }
                else
                {
                    _placements[file] = placement;
                }

                _pending.Enqueue(file);
            }
        }

        PlayPlacement? Placement(string file) => _placements.TryGetValue(file, out var placement) ? placement : Deepest(file);

        PlayPlacement? Deepest(string file)
        {
            var candidates = Candidates(file).ToList();
            return candidates.Count == 0
                ? null
                : candidates.Aggregate((deepest, candidate) => candidate.IsWithinOrSame(deepest) ? candidate : deepest);
        }

        IEnumerable<PlayPlacement> Candidates(string file)
        {
            if (_roots.Contains(file))
            {
                yield return PlayPlacement.Document;
            }

            foreach (var contribution in _contributions.TryGetValue(file, out var contributions) ? contributions.OrderBy(entry => entry.Key.File, StringComparer.Ordinal).ThenBy(entry => entry.Key.Import).Select(entry => entry.Value) : [])
            {
                yield return contribution;
            }
        }

        void ReportConflicts()
        {
            foreach (var file in _found)
            {
                if (!_contributions.TryGetValue(file, out var contributions) || Placement(file) is not { } placement)
                {
                    continue;
                }

                foreach (var ((importer, index), contribution) in contributions.OrderBy(entry => entry.Key.File, StringComparer.Ordinal).ThenBy(entry => entry.Key.Import))
                {
                    if (!placement.IsWithinOrSame(contribution))
                    {
                        _unresolved.Add(file);
                        Report(Diagnostic.Error(
                            DiagnosticCodes.ConflictingImportPlacement,
                            $"'{file}' is imported into both {placement.Description} and {contribution.Description} - a file belongs in one place",
                            _imports[importer][index].Import.Import.Location));
                    }
                }
            }
        }

        void Report(Diagnostic diagnostic)
        {
            if (!_diagnostics.Contains(diagnostic))
            {
                _diagnostics.Add(diagnostic);
            }
        }
    }
}
