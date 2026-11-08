// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Parsing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Workspaces;

/// <summary>
/// The sinks a workspace declares across all of its documents, before and after an edit.
/// </summary>
/// <remarks>
/// A read model's <c>sink &lt;Name&gt;</c> line is the sink directive only when <c>Name</c> is a declared sink, and the
/// declaration may sit in another document. A document parsed on its own therefore resolves its lines against the sinks
/// of the whole workspace, so authoring compares and reparses documents the way the merged compilation reads them.
/// </remarks>
/// <param name="Original">The names of the sinks declared before the edit.</param>
/// <param name="Intended">The names of the sinks declared after the edit.</param>
sealed record WorkspaceSinks(IReadOnlySet<string> Original, IReadOnlySet<string> Intended)
{
    /// <summary>
    /// Collects the sinks of a workspace before and after its documents are replaced.
    /// </summary>
    /// <param name="index">The <see cref="WorkspaceSyntaxIndex"/> of the original workspace.</param>
    /// <param name="intended">The intended syntax of every edited or created document; a created document has no id.</param>
    /// <returns>The <see cref="WorkspaceSinks"/>.</returns>
    internal static WorkspaceSinks For(WorkspaceSyntaxIndex index, IEnumerable<(DocumentId? Document, ApplicationSyntax Syntax)> intended)
    {
        var edited = intended.ToArray();
        var replaced = edited.Where(entry => entry.Document is not null).Select(entry => entry.Document!.Value).ToHashSet();
        var original = Names(index.Entries.Select(entry => entry.Node).OfType<SinkSyntax>());
        var kept = index.Entries.Where(entry => !replaced.Contains(entry.Handle.Document)).Select(entry => entry.Node).OfType<SinkSyntax>();
        return new(original, Names(kept.Concat(edited.SelectMany(entry => entry.Syntax.Sinks))));
    }

    /// <summary>
    /// Resolves a parsed document's read-model sink lines against sinks declared anywhere in the workspace.
    /// </summary>
    /// <param name="parsed">The parse result of one document.</param>
    /// <param name="sinks">The names of the workspace's sinks, or <c>null</c> to keep the document's own resolution.</param>
    /// <returns>The resolved parse result.</returns>
    internal static CompilationResult<ApplicationSyntax> Resolve(CompilationResult<ApplicationSyntax> parsed, IReadOnlySet<string>? sinks) =>
        sinks is null || parsed.Value is null ? parsed : parsed with { Value = SinkResolution.Resolve(parsed.Value, sinks, new ParserContext(new([]))) };

    /// <summary>
    /// Resolves an intended document's read-model sink lines against sinks declared anywhere in the workspace, so a
    /// document an edit built from a standalone fragment compares with its reparse the way the compilation reads both.
    /// </summary>
    /// <param name="intended">The intended syntax of one document.</param>
    /// <param name="sinks">The names of the workspace's sinks, or <c>null</c> to leave it as it is.</param>
    /// <returns>The resolved syntax.</returns>
    internal static ApplicationSyntax Resolve(ApplicationSyntax intended, IReadOnlySet<string>? sinks) =>
        sinks is null ? intended : SinkResolution.Resolve(intended, sinks, new ParserContext(new([])));

    static HashSet<string> Names(IEnumerable<SinkSyntax> sinks) => sinks.Select(sink => sink.Name).ToHashSet(StringComparer.Ordinal);
}
