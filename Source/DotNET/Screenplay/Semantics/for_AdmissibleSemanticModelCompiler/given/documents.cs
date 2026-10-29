// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_AdmissibleSemanticModelCompiler.given;

public class documents : Specification
{
    protected const string Valid = "module M\n  feature F\n    slice StateChange Kept\n";
    protected const string Unsupported = "module M\n  feature F\n    slice StateView Removed\n      readmodel View\n        id String\n      query Scoped => View[]\n        scoped to identity\n    slice StateChange Kept\n";
    protected SemanticDocumentSet _documents;
    protected CompilationResult<SemanticCompilation> _normal;
    protected AdmissibleCompilationResult _result;
    protected IAdmissibleSemanticModelCompiler _compiler;

    protected void Source(string source)
    {
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Example"));
        _documents = SemanticDocumentSet.Create([SemanticSourceDocument.Create(catalog.ResolveDocument("main"), "main", "main.play", source)], catalog);
        _normal = new SemanticModelCompiler().Compile("Example", _documents);
        _compiler = new SemanticModelCompiler();
    }

    protected IEnumerable<string> Slices => _result.Compilation.Value!.Model.Application.Modules.SelectMany(module => module.Features).SelectMany(feature => feature.Slices).Select(slice => slice.Name);
}
