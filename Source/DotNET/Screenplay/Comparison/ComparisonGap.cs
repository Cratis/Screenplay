// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Describes a known limit that prevents a complete comparison verdict.
/// </summary>
/// <param name="Kind">The typed reason.</param>
/// <param name="Statement">The stable statement of the comparison gap.</param>
public sealed record ComparisonGap(
    ComparisonGapKind Kind,
    string Statement);
