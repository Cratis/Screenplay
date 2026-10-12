// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed class McpNotesFilter(IEnumerable<Func<ModelNotesItem, bool>> rules)
{
    internal static McpNotesFilter Read(JsonElement arguments, ScreenplayWorkspace workspace) => new(
    [
        State(McpNotesArguments.State(arguments)),
        Kind(McpJson.OptionalString(arguments, "kind")),
        Anchor(arguments, workspace),
        Reference(McpJson.OptionalString(arguments, "ref")),
        Scope(McpJson.OptionalString(arguments, "scope"), workspace)
    ]);

    internal bool Matches(ModelNotesItem item) => rules.All(rule => rule(item));

    static Func<ModelNotesItem, bool> State(string state) => item => state == "all" || item.State == state;

    static Func<ModelNotesItem, bool> Kind(string? kind) => item => kind is null || item.Note.Kind == kind;

    static Func<ModelNotesItem, bool> Anchor(JsonElement arguments, ScreenplayWorkspace workspace)
    {
        var anchor = arguments.TryGetProperty("anchor", out var target) ? McpNotesTargets.Read(target, workspace, allowOrphan: true) : null;
        return item => anchor is null || item.Note.Anchor == anchor;
    }

    static Func<ModelNotesItem, bool> Reference(string? reference)
    {
        if (reference is null) return _ => true;
        return item => item.Note.Refs.Select(value => value.Id).Append(item.Note.Resolution?.Ref?.Id).Contains(reference, StringComparer.Ordinal);
    }

    static Func<ModelNotesItem, bool> Scope(string? scope, ScreenplayWorkspace workspace) =>
        item => ModelNotesAnchors.InScope(ModelNotesAnchors.Resolve(item.Note.Anchor, workspace.IdentityCatalog), scope);
}
