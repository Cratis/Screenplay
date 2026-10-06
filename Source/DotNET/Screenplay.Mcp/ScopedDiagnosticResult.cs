// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

sealed record ScopedDiagnosticResult(
    string Scope,
    int DeclarationCount,
    int DependentDeclarationCount,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<string> AffectedScopes,
    ScopedUnresolvedEventConsumers UnresolvedEventConsumers,
    int PossiblyAffectedReferenceCount,
    string DependencyCoverage);
