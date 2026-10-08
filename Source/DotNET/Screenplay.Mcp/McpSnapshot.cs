// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpSnapshot : IPlayFiles
{
    readonly McpAnalysisCompiler _compiler;
    readonly Dictionary<string, ImmutableArray<Diagnostic>> _completeness = [];
    readonly Lazy<CompilationResult<ApplicationSyntax>> _compilation;
    readonly Lazy<McpSyntaxIndex> _index;
    readonly Lazy<IReadOnlyList<PlacedPlayDocument>> _placements;
    readonly Lazy<DependencyGraph> _dependencyGraph;
    AuthoredTimeline _timeline = null!;

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents)
        : this(documents, ScreenplayLanguageRegistry.Default)
    {
    }

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents, IScreenplayLanguageRegistry languages)
        : this(documents.ToDictionary(document => document.Path.Value, document => document.Text, StringComparer.Ordinal), McpSourceRevision.For(documents), languages)
    {
    }

    // CLI source paths and encodings follow PlayFileCompiler, not workspace admission rules.
    internal McpSnapshot(IReadOnlyDictionary<string, string> sources)
        : this(sources, string.Empty, ScreenplayLanguageRegistry.Default)
    {
    }

    internal McpSnapshot(
        IReadOnlyDictionary<string, string> sources,
        string revision,
        IScreenplayLanguageRegistry languages,
        McpAnalysisCompiler? compiler = null,
        (CompilationResult<ApplicationSyntax> Result, AuthoredTimeline Timeline)? compilation = null,
        IEnumerable<string>? roots = null)
    {
        _compiler = compiler ?? new(languages);
        Sources = sources;
        SourceRevision = revision;
        _compilation = new(() =>
        {
            if (compilation is { } compiled)
            {
                _timeline = compiled.Timeline;
                return compiled.Result;
            }

            var source = new DiskPlayDocumentSource(this, ".");
            var (_, result) = PlayApplicationAssembly.Compile(_compiler, roots ?? source.FilesBeneath(string.Empty), source, _compiler.Languages, out _timeline);

            return result;
        });
        _placements = new(() => PlayImports.Resolve(roots ?? Sources.Keys, new InMemoryPlayDocumentSource(Sources), languages).Documents);
        _index = new(CreateIndex);
        _dependencyGraph = new(() =>
        {
            _ = Compilation;

            return DependencyGraph.For(_timeline);
        });
    }

    internal IReadOnlyList<PlacedPlayDocument> Placements => _placements.Value;

    internal CompilationResult<ApplicationSyntax> Compilation => _compilation.Value;

    internal McpSyntaxIndex Index => _index.Value;

    internal DependencyGraph DependencyGraph => _dependencyGraph.Value;

    internal bool IsCompilationCreated => _compilation.IsValueCreated;

    internal bool IsIndexCreated => _index.IsValueCreated;

    internal int ParsedDocumentCount => _compiler.ParsedDocumentCount;

    internal IScreenplayLanguageRegistry Languages => _compiler.Languages;

    internal string SourceRevision { get; }

    internal IReadOnlyDictionary<string, string> Sources { get; }

    /// <inheritdoc/>
    public IEnumerable<PlayFile> FindIn(string root)
    {
        var folder = root == "." ? string.Empty : root.Replace('\\', '/');
        if (folder.StartsWith("./", StringComparison.Ordinal))
        {
            folder = folder[2..];
        }
        var prefix = folder.Length == 0 ? string.Empty : $"{folder.TrimEnd('/')}/";

        // IPlayFiles returns paths relative to the requested folder, not the application root.
        // DiskPlayDocumentSource adds that folder back when it follows native file imports.
        return Sources.Keys.Where(path => path.StartsWith(prefix, StringComparison.Ordinal))
            .Order(StringComparer.Ordinal)
            .Select(path => new PlayFile(path, path[prefix.Length..]));
    }

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => Sources[file.RelativePath];

    internal static McpSnapshot Compile(string target, bool isFile)
    {
        var compiler = new McpAnalysisCompiler();
        var files = new PlayFileCompiler(new PlayFiles(), compiler);
        var compilation = isFile ? files.CompileApplication(target) : files.CompileFolder(target);
        var sources = compilation.Sources.ToDictionary(source => source.File.RelativePath, source => source.Source, StringComparer.Ordinal);

        return new(sources, string.Empty, compiler.Languages, compiler, (compilation.Result, files.Timeline), isFile ? [Path.GetFileName(target)] : sources.Keys);
    }

    internal ImmutableArray<Diagnostic> Completeness(CompletenessChecks checks)
    {
        var key = string.Join(',', checks.Selected.Order());
        if (!_completeness.TryGetValue(key, out var diagnostics))
        {
            diagnostics = ModelCompleteness.Check(Compilation, checks);
            _completeness.Add(key, diagnostics);
        }

        return diagnostics;
    }

    McpSyntaxIndex CreateIndex()
    {
        var compilation = Compilation;
        var index = new McpSyntaxIndex();

        // Compilation retains provisional trees for diagnostics. Physical authoring candidates
        // require an authoritative placement, including descendants of conflicting barrels.
        var placements = Placements;
        var resolvedPaths = placements.Where(document => document.IsPlacementResolved).Select(document => document.Path).ToHashSet(StringComparer.Ordinal);
        var physical = _compiler.Documents.Select(document =>
        {
            var resolved = document.Path is not null && resolvedPaths.Contains(document.Path);

            // Never parse a conflicting placement as a guessed owner. Read its literal root
            // to retain physical source candidates, independently of navigation authority.
            var result = resolved ? document.Result : new ScreenplayCompiler(_compiler.Languages).Parse(Sources[document.Path!], document.Path);
            return (Result: result, Resolved: resolved);
        }).ToArray();
        var complete = placements.All(document => document.IsPlacementResolved) && physical.All(document => !EventSourceReadConfidence.HasUnknownExtent(document.Result.Diagnostics));
        index.SourceConfidence = new(physical.SelectMany(document => (document.Result.Value?.EventSources ?? []).Select(source => (source, document.Resolved))), complete);
        var applications = _compiler.Documents.Where(document => document.Path is not null && resolvedPaths.Contains(document.Path))
            .Select(document => document.Result.Value).OfType<ApplicationSyntax>().ToArray();

        // Preserve physical slice ownership and all candidates, including declarations that a failed
        // merge cannot select. Do not initialize readiness from just the first file or merged owners.
        index.Initialize(new(
            applications.SelectMany(application => application.Imports),
            applications.SelectMany(application => application.Concepts),
            applications.SelectMany(application => application.Policies),
            applications.SelectMany(application => application.Modules),
            Diagnostics.SourceLocation.Start)
        {
            Types = applications.SelectMany(application => application.Types ?? []),
            Examples = applications.SelectMany(application => application.Examples),
            Systems = applications.SelectMany(application => application.Systems),
            EventSources = physical.SelectMany(document => document.Result.Value?.EventSources ?? []),
            SourceOptions = applications.Any(application => application.SourceOptions.NumericMode == NumericMode.Exact) ? SourceOptions.Exact : SourceOptions.Legacy
        });
        foreach (var application in applications) index.VisitApplication(application with { EventSources = [] });
        foreach (var source in physical.SelectMany(document => document.Result.Value?.EventSources ?? [])) index.VisitEventSource(source);

        index.Complete(compilation.Value);
        return index;
    }
}
