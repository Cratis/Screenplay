// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Defines the ESM allocation for first-class ordinal text rules.
/// </summary>
public static class TextRuleVersion
{
    /// <summary>
    /// The first language and semantic major version admitting ordinal text rules.
    /// </summary>
    public const uint Major = 24;

    internal static bool IsTextRule(SemanticValidationRuleKind kind) => kind is
        SemanticValidationRuleKind.Contains or SemanticValidationRuleKind.NotContains or
        SemanticValidationRuleKind.StartsWith or SemanticValidationRuleKind.EndsWith;
}
