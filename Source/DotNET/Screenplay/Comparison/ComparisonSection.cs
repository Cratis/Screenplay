// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Comparison;

/// <summary>
/// Reports comparison coverage for one structural section.
/// </summary>
/// <param name="Section">The section.</param>
/// <param name="Complete">Whether the section has no known gaps.</param>
/// <param name="Gaps">The typed gaps and their contractual statements.</param>
public sealed record ComparisonSection(
    ComparisonSectionKind Section,
    bool Complete,
    IReadOnlyList<ComparisonGap> Gaps);
