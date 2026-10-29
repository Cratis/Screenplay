// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Identifies an excluded admissible unit.
/// </summary>
/// <param name="Kind">The kind of unit.</param>
/// <param name="Address">The address used in the exclusion list.</param>
public sealed record SemanticExclusionReference(SemanticExclusionKind Kind, string Address);
