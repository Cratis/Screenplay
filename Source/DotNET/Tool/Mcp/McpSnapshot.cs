// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Files;
using Cratis.Screenplay.Syntax;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Tool.Mcp;

sealed class McpSnapshot : IPlayFiles
{
    readonly ImmutableArray<WorkspaceDocument> _documents;

    internal McpSnapshot(ImmutableArray<WorkspaceDocument> documents)
    {
        _documents = documents;
        Compilation = new PlayFileCompiler(this, new ScreenplayCompiler()).CompileFolder(".").Result;
        Index = new McpSyntaxIndex();
        foreach (var document in documents)
        {
            var parsed = new ScreenplayCompiler().Parse(document.Text, document.Path.Value);
            if (parsed.Value is not null)
            {
                Index.VisitApplication(parsed.Value);
            }
        }
    }

    internal CompilationResult<ApplicationSyntax> Compilation { get; }

    internal McpSyntaxIndex Index { get; }

    /// <inheritdoc/>
    public IEnumerable<PlayFile> FindIn(string root) => _documents.Select(document => new PlayFile(document.Path.Value, document.Path.Value));

    /// <inheritdoc/>
    public string ReadContent(PlayFile file) => _documents.Single(document => document.Path.Value == file.RelativePath).Text;
}
