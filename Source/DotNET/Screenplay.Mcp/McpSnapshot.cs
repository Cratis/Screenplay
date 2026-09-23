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
    public IEnumerable<PlayFile> FindIn(string root) => _documents.Select(document => new PlayFile(document.Path.Value, document.Path.Value));

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => _documentsByPath[file.RelativePath].Text;

    McpSyntaxIndex CreateIndex()
    {
        var compilation = Compilation;
        var index = new McpSyntaxIndex();
        foreach (var application in _compiler.Documents.Select(document => document.Value).OfType<ApplicationSyntax>())
        {
            index.VisitApplication(application);
        }

        index.Complete(compilation.Value);
        return index;
    }
}
