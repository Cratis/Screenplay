// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Files;

namespace Cratis.Screenplay.Semantics;

/// <summary>
/// Compiles Screenplay source documents through the compatible syntax tree to ESM.
/// </summary>
/// <param name="compiler">The source syntax compiler.</param>
/// <param name="binder">The semantic model binder.</param>
public sealed class SemanticModelCompiler(IScreenplayCompiler compiler, ISemanticModelBinder binder) : ISemanticModelCompiler
{
    /// <summary>
    /// Initializes a new instance of the <see cref="SemanticModelCompiler"/> class with default collaborators.
    /// </summary>
    public SemanticModelCompiler()
        : this(new ScreenplayCompiler(), new SemanticModelBinder())
    {
    }

    /// <inheritdoc/>
    public CompilationResult<SemanticCompilation> Compile(string applicationName, SemanticDocumentSet documents)
    {
        var parsed = documents.Documents
            .Select(document => compiler.Parse(document.Text, document.DisplayPath))
            .ToArray();
        var syntax = PlayFolderMerge.Merge(parsed);
        if (!syntax.Success)
        {
            return CompilationResult<SemanticCompilation>.Failed(syntax.Diagnostics);
        }

        var bound = binder.Bind(applicationName, syntax.Value!, documents);
        var diagnostics = syntax.Diagnostics.Concat(bound.Diagnostics).ToArray();
        return bound.Success
            ? new(bound.Value, diagnostics)
            : CompilationResult<SemanticCompilation>.Failed(diagnostics);
    }
}
