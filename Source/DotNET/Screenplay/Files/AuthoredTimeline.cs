// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Files;

/// <summary>
/// Snapshot-local presentation evidence, separate from the executable model and its identity catalog.
/// </summary>
internal sealed record AuthoredTimeline(
    string? Root,
    ApplicationSyntax? Application,
    IReadOnlyList<PlacedPlayDocument> Documents,
    IReadOnlyDictionary<string, int> Ranks,
    IReadOnlyDictionary<string, IReadOnlyList<AuthoredOrderStep>> Origins,
    IReadOnlyList<TimelineFinding> Findings,
    bool SourceValid);
