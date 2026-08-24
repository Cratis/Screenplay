// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Compiles one logical Screenplay document set to its executable semantic model.
/// </summary>
public interface ISemanticModelCompiler
{
    /// <summary>
    /// Parses, merges, resolves, and binds a logical application document set.
    /// </summary>
    /// <param name="applicationName">The stable display name of the application.</param>
    /// <param name="documents">The source documents and authoritative identity catalog.</param>
    /// <returns>The semantic compilation, or typed parser/binder diagnostics when compilation is blocked.</returns>
    CompilationResult<SemanticCompilation> Compile(string applicationName, SemanticDocumentSet documents);
}
