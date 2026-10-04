// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpSnapshot : IPlayFiles
{
    readonly ImmutableArray<WorkspaceDocument> _documents;
    readonly Dictionary<string, WorkspaceDocument> _documentsByPath;
    readonly McpAnalysisCompiler _compiler = new();
    readonly Lazy<CompilationResult<ApplicationSyntax>> _compilation;
    readonly Lazy<McpSyntaxIndex> _index;

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents)
    {
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
            .Select(document => new PlayFile(document.Path.Value, document.Path.Value[prefix.Length..]));
    }

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => _documentsByPath[file.RelativePath].Text;

    McpSyntaxIndex CreateIndex()
    {
        var compilation = Compilation;
        var index = new McpSyntaxIndex();
        if (compilation.Value is { } assembled) index.Initialize(assembled);

        // Compilation retains provisional trees for diagnostics. Physical authoring candidates
        // require an authoritative placement, including descendants of conflicting barrels.
        var (placements, _) = PlayImports.Resolve(
            _documentsByPath.Keys,
            new InMemoryPlayDocumentSource(_documentsByPath.ToDictionary(entry => entry.Key, entry => entry.Value.Text, StringComparer.Ordinal)));
        var resolvedPaths = placements.Where(document => document.IsPlacementResolved).Select(document => document.Path).ToHashSet(StringComparer.Ordinal);
        foreach (var application in _compiler.Documents.Where(document => document.Path is not null && resolvedPaths.Contains(document.Path))
            .Select(document => document.Result.Value).OfType<ApplicationSyntax>())
        {
            index.VisitApplication(application);
        }

        index.Complete(compilation.Value);
        return index;
    }
}
