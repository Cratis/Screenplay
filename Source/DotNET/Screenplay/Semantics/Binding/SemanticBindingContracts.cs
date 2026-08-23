// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.Binding;

/// <summary>
/// Defines the additive syntax-to-ESM binding boundary.
/// </summary>
public interface ISemanticModelBinder
{
    /// <summary>
    /// Binds one merged application syntax tree and its source document set.
    /// </summary>
    /// <param name="application">The existing public application syntax tree.</param>
    /// <param name="documents">The source documents and identity assignments.</param>
    /// <param name="options">The explicit language and semantic binding options.</param>
    /// <returns>The semantic binding compilation.</returns>
    SemanticBindingCompilation Bind(
        ApplicationSyntax application,
        SemanticDocumentSet documents,
        SemanticBindingOptions options);
}

/// <summary>
/// Defines the future document-set compilation boundary without changing existing compiler or file interfaces.
/// </summary>
public interface ISemanticDocumentCompiler
{
    /// <summary>
    /// Compiles a logical source document set into an executable semantic model.
    /// </summary>
    /// <param name="documents">The logical application documents and identity assignments.</param>
    /// <param name="options">The explicit language and semantic binding options.</param>
    /// <returns>The semantic binding compilation.</returns>
    SemanticBindingCompilation Compile(SemanticDocumentSet documents, SemanticBindingOptions options);
}

/// <summary>
/// Represents options for binding syntax into an executable semantic model.
/// </summary>
/// <param name="LanguageVersion">The source language version to bind.</param>
/// <param name="SemanticVersion">The portable execution semantic version to bind.</param>
/// <param name="AllowLegacyIdentityBootstrap">Whether unresolved legacy declarations may receive deterministic provisional identities.</param>
public sealed record SemanticBindingOptions(
    LanguageVersion LanguageVersion,
    SemanticVersion SemanticVersion,
    bool AllowLegacyIdentityBootstrap)
{
    /// <summary>
    /// Gets the initial binding options with deterministic legacy identity bootstrap enabled.
    /// </summary>
    public static SemanticBindingOptions V1 { get; } = new(LanguageVersion.V1, SemanticVersion.V1, true);
}

/// <summary>
/// Represents the complete result of semantic binding.
/// </summary>
/// <param name="Model">The executable semantic model, or <c>null</c> when binding failed.</param>
/// <param name="IdentityCatalog">The resulting authoritative and provisional identity assignments.</param>
/// <param name="SourceMap">The source-to-semantic map.</param>
/// <param name="Diagnostics">The binding diagnostics.</param>
public sealed record SemanticBindingCompilation(
    ExecutableSemanticModel? Model,
    SemanticIdentityCatalog IdentityCatalog,
    SemanticSourceMap SourceMap,
    ImmutableArray<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets a value indicating whether binding produced a model without error diagnostics.
    /// </summary>
    public bool Success => Model is not null && !Diagnostics.IsDefault && Diagnostics.All(_ => _.Severity != DiagnosticSeverity.Error);
}
