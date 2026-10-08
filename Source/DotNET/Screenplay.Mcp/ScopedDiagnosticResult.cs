// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Mcp;

/// <summary>
/// Represents source diagnostics for a uniquely selected scope and its direct dependent declarations.
/// </summary>
/// <param name="Scope">The selected case-sensitive dotted address.</param>
/// <param name="DeclarationCount">The number of declarations in the scope, including descendants.</param>
/// <param name="DependentDeclarationCount">The additional direct-dependent declaration count.</param>
/// <param name="Diagnostics">The reported diagnostics, including selected completeness findings.</param>
/// <param name="AffectedScopes">The sorted, distinct scopes containing direct dependents; an empty address means the application.</param>
/// <param name="UnresolvedEventConsumers">Unattributable event references outside the reported declarations.</param>
/// <param name="PossiblyAffectedReferenceCount">Other unresolved references outside the reported declarations, excluding event consumers.</param>
/// <param name="DependencyCoverage">A description of the source-reference index's coverage limits.</param>
/// <param name="WholeApplicationErrorCount">The error count across the whole application.</param>
/// <param name="WholeApplicationWarningCount">The warning count across the whole application, including completeness findings.</param>
public sealed record ScopedDiagnosticResult(
    string Scope,
    int DeclarationCount,
    int DependentDeclarationCount,
    ImmutableArray<Diagnostic> Diagnostics,
    ImmutableArray<string> AffectedScopes,
    ScopedUnresolvedEventConsumers UnresolvedEventConsumers,
    int PossiblyAffectedReferenceCount,
    string DependencyCoverage,
    int WholeApplicationErrorCount,
    int WholeApplicationWarningCount);
