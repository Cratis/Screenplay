// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Describes an ordinal text rule equivalent to a deprecated pattern.
/// </summary>
/// <param name="Kind">The replacement rule kind.</param>
/// <param name="Value">The literal text operand.</param>
public sealed record PortableMatchReplacement(SemanticValidationRuleKind Kind, string Value);
