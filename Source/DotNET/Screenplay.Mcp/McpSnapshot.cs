// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpSnapshot : IPlayFiles
{
    readonly ImmutableArray<WorkspaceDocument> _documents;
    readonly Dictionary<string, WorkspaceDocument> _documentsByPath;
    readonly McpAnalysisCompiler _compiler;
    readonly Lazy<CompilationResult<ApplicationSyntax>> _compilation;
    readonly Lazy<McpSyntaxIndex> _index;

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents)
        : this(documents, ScreenplayLanguageRegistry.Default)
    {
    }

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents, IScreenplayLanguageRegistry languages)
    {
        _compiler = new(languages);
        _documents = documents;
        _documentsByPath = documents.ToDictionary(document => document.Path.Value, StringComparer.Ordinal);
        SourceRevision = McpSourceRevision.For(documents);
        _compilation = new(() => new PlayFileCompiler(this, _compiler).CompileFolder(".").Result);
        _index = new(CreateIndex);
    }

    internal CompilationResult<ApplicationSyntax> Compilation => _compilation.Value;

    internal McpSyntaxIndex Index => _index.Value;

    internal bool IsCompilationCreated => _compilation.IsValueCreated;

    internal bool IsIndexCreated => _index.IsValueCreated;

    internal int ParsedDocumentCount => _compiler.ParsedDocumentCount;

    internal string SourceRevision { get; }

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
        return _documents.Where(document => document.Path.Value.StartsWith(prefix, StringComparison.Ordinal))
            .OrderBy(document => document.Path.Value, StringComparer.Ordinal)
            .Select(document => new PlayFile(document.Path.Value, document.Path.Value[prefix.Length..]));
    }

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => _documentsByPath[file.RelativePath].Text;

    McpSyntaxIndex CreateIndex()
    {
        var compilation = Compilation;
        var index = new McpSyntaxIndex();

        // Compilation retains provisional trees for diagnostics. Physical authoring candidates
        // require an authoritative placement, including descendants of conflicting barrels.
        var (placements, _) = PlayImports.Resolve(
            _documentsByPath.Keys,
            new InMemoryPlayDocumentSource(_documentsByPath.ToDictionary(entry => entry.Key, entry => entry.Value.Text, StringComparer.Ordinal)),
            _compiler.Languages);
        var resolvedPaths = placements.Where(document => document.IsPlacementResolved).Select(document => document.Path).ToHashSet(StringComparer.Ordinal);
        var physical = _compiler.Documents.Select(document =>
        {
            var resolved = document.Path is not null && resolvedPaths.Contains(document.Path);

            // Never parse a conflicting placement as a guessed owner. Read its literal root
            // to retain physical source candidates, independently of navigation authority.
            var result = resolved ? document.Result : new ScreenplayCompiler(_compiler.Languages).Parse(_documentsByPath[document.Path!].Text, document.Path);
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
