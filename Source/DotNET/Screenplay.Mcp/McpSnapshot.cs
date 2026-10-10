// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Completeness;
using Cratis.Screenplay.Dependencies;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Indexing;
using Cratis.Screenplay.Languages;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

// Retain the MCP/Tool internal boundary without exposing the library's authoring indexes.
sealed class McpSnapshot : IPlayFiles
{
    internal McpSnapshot(AuthoringSnapshot authoring) => Authoring = authoring;

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents)
        : this(new AuthoringSnapshot(documents))
    {
    }

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents, IScreenplayLanguageRegistry languages)
        : this(new AuthoringSnapshot(documents, languages))
    {
    }

    internal McpSnapshot(IReadOnlyDictionary<string, string> sources)
        : this(new AuthoringSnapshot(sources))
    {
    }

    internal McpSnapshot(
        IReadOnlyDictionary<string, string> sources,
        string revision,
        IScreenplayLanguageRegistry languages,
        McpAnalysisCompiler? compiler = null,
        (CompilationResult<ApplicationSyntax> Result, AuthoredTimeline Timeline)? compilation = null,
        IEnumerable<string>? roots = null)
        : this(new AuthoringSnapshot(sources, revision, languages, compiler, compilation, roots))
    {
    }

    internal AuthoringSnapshot Authoring { get; }
    internal IReadOnlyList<PlacedPlayDocument> Placements => Authoring.Placements;
    internal CompilationResult<ApplicationSyntax> Compilation => Authoring.Compilation;
    internal McpSyntaxIndex Index => Authoring.Index;
    internal DependencyGraph DependencyGraph => Authoring.DependencyGraph;
    internal bool IsCompilationCreated => Authoring.IsCompilationCreated;
    internal bool IsIndexCreated => Authoring.IsIndexCreated;
    internal int ParsedDocumentCount => Authoring.ParsedDocumentCount;
    internal IScreenplayLanguageRegistry Languages => Authoring.Languages;
    internal string SourceRevision => Authoring.SourceRevision;
    internal IReadOnlyDictionary<string, string> Sources => Authoring.Sources;

    /// <inheritdoc/>
    public IEnumerable<PlayFile> FindIn(string root) => Authoring.FindIn(root);

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => Authoring.ReadContent(file);

    internal static McpSnapshot Compile(string target, bool isFile) => new(AuthoringSnapshot.Compile(target, isFile));

    internal ImmutableArray<Diagnostic> Completeness(CompletenessChecks checks) => Authoring.Completeness(checks);
}
