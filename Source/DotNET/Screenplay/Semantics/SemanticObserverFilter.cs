// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Filters observed facts by their stored event source and optional stream names, never stream ids.
/// </summary>
/// <param name="Source">The event source catalog identity.</param>
/// <param name="Stream">The optional source-owned stream identity.</param>
public sealed record SemanticObserverFilter(SemanticId Source, SemanticId? Stream = null)
{
    internal bool Matches(SemanticApplication application, SemanticEventRoute? route)
    {
        if (route is null) return false;
        var source = application.EventSources.Single(value => value.Id == Source);

        return source.SourceKind == route.SourceKind &&
            (Stream is null || source.Streams.Single(value => value.Id == Stream).StreamKind == route.StreamKind);
    }
}
