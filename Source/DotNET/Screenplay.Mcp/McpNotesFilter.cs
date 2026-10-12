// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Workspaces;

namespace Cratis.Screenplay.Mcp;

sealed record McpNotesFilter(string State, string? Kind, ModelNotesAnchor? Anchor, string? Ref, string? Scope)
{
    internal bool Matches(ModelNotesItem item, SemanticIdentityCatalog catalog) =>
        MatchesState(item) && MatchesKind(item.Note) && MatchesAnchor(item.Note) && MatchesReference(item.Note) &&
        ModelNotesAnchors.InScope(ModelNotesAnchors.Resolve(item.Note.Anchor, catalog), Scope);

    bool MatchesState(ModelNotesItem item) => State == "all" || item.State == State;
    bool MatchesKind(ModelNote note) => Kind is null || note.Kind == Kind;
    bool MatchesAnchor(ModelNote note) => Anchor is null || note.Anchor == Anchor;
    bool MatchesReference(ModelNote note) => Ref is null || note.Refs.Any(value => value.Id == Ref) || note.Resolution?.Ref?.Id == Ref;
}
