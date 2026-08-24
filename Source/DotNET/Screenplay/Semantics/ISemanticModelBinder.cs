// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Binds the compatible source syntax tree to the versioned executable semantic model.
/// </summary>
public interface ISemanticModelBinder
{
    /// <summary>
    /// Resolves and binds one compiled application without silently dropping unsupported syntax.
    /// </summary>
    /// <param name="applicationName">The stable display name of the application.</param>
    /// <param name="syntax">The compiled source syntax tree.</param>
    /// <param name="documents">The source documents and authoritative identity catalog.</param>
    /// <returns>The semantic compilation, or typed diagnostics when binding is blocked.</returns>
    CompilationResult<SemanticCompilation> Bind(
        string applicationName,
        ApplicationSyntax syntax,
        SemanticDocumentSet documents);
}
