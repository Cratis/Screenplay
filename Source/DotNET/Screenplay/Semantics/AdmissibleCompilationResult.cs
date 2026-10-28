// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Represents an executable subset compilation and the declarations excluded from it.
/// </summary>
/// <param name="Compilation">The usual semantic compilation result, including final warnings.</param>
/// <param name="Exclusions">The excluded declarations, ordered by source location.</param>
public sealed record AdmissibleCompilationResult(CompilationResult<SemanticCompilation> Compilation, ImmutableArray<SemanticExclusion> Exclusions);
