// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>Contains an options-based binding outcome, including any typed metadata refusal.</summary>
/// <param name="Compilation">The binding result. Its value is absent when the requested metadata is refused.</param>
/// <param name="Refusal">The explicit metadata refusal, or null.</param>
public sealed record SemanticBindingOptionsResult(CompilationResult<SemanticCompilation> Compilation, PropertyOrderMetadataRefusal? Refusal = null)
{
    /// <summary>Gets whether binding supplied all requested metadata without errors.</summary>
    public bool Success => Refusal is null && Compilation.Success;
}
