// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Encodings.Web;
using System.Text.Json;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Records presentation ranks separately from syntax, semantic bytes and identities.
/// </summary>
internal static class AuthoredOrder
{
    static readonly JsonSerializerOptions _json = new() { Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping };

    internal static string Key(IEnumerable<string> scope) => JsonSerializer.Serialize(scope.ToArray(), _json);

    internal static IReadOnlyDictionary<string, int> Record(
        IReadOnlyList<string> roots,
        IReadOnlyList<PlacedPlayDocument> documents,
        IScreenplayLanguageRegistry languages,
        IReadOnlyDictionary<string, ApplicationSyntax>? parsed = null,
        IReadOnlyDictionary<string, IReadOnlyList<DiscoveredFileImport>>? imports = null) =>
        Record(roots, documents, languages, out _, parsed, imports);

    internal static IReadOnlyDictionary<string, int> Record(
        IReadOnlyList<string> roots,
        IReadOnlyList<PlacedPlayDocument> documents,
        IScreenplayLanguageRegistry languages,
        out IReadOnlyDictionary<string, IReadOnlyList<AuthoredOrderStep>> origins,
        IReadOnlyDictionary<string, ApplicationSyntax>? parsed = null,
        IReadOnlyDictionary<string, IReadOnlyList<DiscoveredFileImport>>? imports = null)
    {
        var files = documents.ToDictionary(document => document.Path, StringComparer.Ordinal);
        var own = documents.Where(document => document.IsPlacementResolved).ToDictionary(
            document => document.Path,
            document => Declarations(parsed?.GetValueOrDefault(document.Path) ?? ScreenplayCompiler.ParsePlaced(document.Source, document.Path, document.Placement, languages).Value!).ToArray(),
            StringComparer.Ordinal);
        var owners = ContainerOwners(own, roots.Select(PlayGlob.Normalize).ToHashSet(StringComparer.Ordinal));
        var explicitDeclarations = own.Values.SelectMany(declarations => declarations).Where(declaration => !declaration.Implicit).Select(declaration => Key(declaration.Scope)).ToHashSet(StringComparer.Ordinal);
        var order = new Dictionary<string, int>(StringComparer.Ordinal);
        var trace = new Dictionary<string, IReadOnlyList<AuthoredOrderStep>>(StringComparer.Ordinal);
        var visited = new HashSet<string>(StringComparer.Ordinal);
        void Visit(string path, IReadOnlyList<AuthoredOrderStep> outer)
        {
            if (!files.TryGetValue(path, out var document) || !document.IsPlacementResolved || !visited.Add(path))
            {
                return;
            }

            var entries = new List<(SourceLocation Location, Action Run)>();
            foreach (var declaration in own[path])
            {
                var key = Key(declaration.Scope);
                if ((declaration.Implicit && explicitDeclarations.Contains(key)) || (declaration.IsContainer && owners.TryGetValue(key, out var owner) && owner != path))
                {
                    continue;
                }

                entries.Add((declaration.Location, () =>
                {
                    if (order.TryAdd(key, order.Count))
                    {
                        trace[key] = [.. outer, .. declaration.Chain.Select(node => new AuthoredOrderStep(path, node, node.Location, null, null))];
                    }
                }));
            }

            foreach (var import in imports is not null ? imports[path] : ScreenplayCompiler.DiscoverImports(document.Source, path, languages))
            {
                entries.Add((import.Import.Location, () =>
                {
                    var placement = import.PlacementFrom(document.Placement);
                    if (placement is null)
                    {
                        return;
                    }

                    var pattern = PlayGlob.Resolve(path, import.Import.Pattern);
                    var ancestors = own[path].Where(declaration => declaration.IsContainer && !declaration.Implicit &&
                        declaration.Scope.Length <= placement.Scope.Count && declaration.Scope.SequenceEqual(placement.Scope.Take(declaration.Scope.Length)))
                        .OrderBy(declaration => declaration.Scope.Length).Select(declaration => declaration.Chain[^1]).Distinct().ToArray();
                    var matches = files.Keys.Where(target => target != path && PlayGlob.IsMatch(pattern, target)).Order(StringComparer.Ordinal).ToArray();
                    for (var match = 0; match < matches.Length; match++)
                    {
                        var target = matches[match];
                        if (files[target].Placement.Equals(placement))
                        {
                            Visit(target, [.. outer, .. ancestors.Select(node => new AuthoredOrderStep(path, node, node.Location, null, null)),
                                new(path, import.Import, import.Import.Location, target, match)]);
                        }
                    }
                }));
            }

            foreach (var entry in entries.OrderBy(entry => entry.Location.Line).ThenBy(entry => entry.Location.Column))
            {
                entry.Run();
            }
        }

        foreach (var root in roots.Select(PlayGlob.Normalize))
        {
            if (files.TryGetValue(root, out var document) && document.Placement.IsDocument)
            {
                Visit(root, []);
            }
        }

        origins = trace;
        return order;
    }

    static Dictionary<string, string> ContainerOwners(IReadOnlyDictionary<string, Declaration[]> own, HashSet<string> roots)
    {
        var owners = new Dictionary<string, (string Path, int Priority, int Depth)>(StringComparer.Ordinal);
        foreach (var (path, declarations) in own)
        {
            var segments = path.Split('/');
            var name = segments[^1][..^5];
            foreach (var declaration in declarations.Where(declaration => declaration.IsContainer && !declaration.Implicit))
            {
                var ancestor = Array.IndexOf(declaration.Scope, name);
                if (!roots.Contains(path) && ancestor == -1)
                {
                    continue;
                }

                var priority = roots.Contains(path) ? 0 : ancestor + 1;
                var key = Key(declaration.Scope);
                if (!owners.TryGetValue(key, out var current) || priority < current.Priority || (priority == current.Priority && segments.Length < current.Depth))
                {
                    owners[key] = (path, priority, segments.Length);
                }
            }
        }

        return owners.ToDictionary(entry => entry.Key, entry => entry.Value.Path, StringComparer.Ordinal);
    }

    static IEnumerable<Declaration> Declarations(ApplicationSyntax application)
    {
        foreach (var module in application.Modules)
        {
            SyntaxNode[] chain = module.IsPlacement ? [] : [module];
            yield return new([module.Name], module.Location, module.IsPlacement, true, chain);
            foreach (var declaration in module.Features.SelectMany(feature => Declarations(feature, [module.Name], chain)))
            {
                yield return declaration;
            }
        }
    }

    static IEnumerable<Declaration> Declarations(FeatureSyntax feature, string[] outer, SyntaxNode[] ancestors)
    {
        string[] scope = [.. outer, feature.Name];
        var chain = feature.IsPlacement ? ancestors : [.. ancestors, feature];
        yield return new(scope, feature.Location, feature.IsPlacement, true, chain);
        foreach (var declaration in feature.Features.SelectMany(child => Declarations(child, scope, chain)))
        {
            yield return declaration;
        }

        foreach (var slice in feature.Slices)
        {
            yield return new([.. scope, slice.Name], slice.Location, false, false, [.. chain, slice]);
        }
    }

    sealed record Declaration(string[] Scope, SourceLocation Location, bool Implicit, bool IsContainer, SyntaxNode[] Chain);
}
