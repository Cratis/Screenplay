// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Reports portable pattern admission as target-neutral data.
/// </summary>
/// <param name="Status">The admission classification.</param>
/// <param name="Code">The diagnostic code, or null for an admitted pattern.</param>
/// <param name="Construct">The classified construct, or null for an admitted pattern.</param>
/// <param name="Offset">The UTF-16 offset of the construct in the decoded pattern.</param>
/// <param name="Suggestion">An available authoring alternative.</param>
/// <param name="Description">A human-readable explanation.</param>
/// <param name="Replacement">The equivalent rule for a deprecated Studio pattern.</param>
public sealed record PortableMatchCheck(
    PortableMatchStatus Status,
    string? Code,
    string? Construct,
    int Offset,
    string? Suggestion,
    string Description,
    PortableMatchReplacement? Replacement = null);
