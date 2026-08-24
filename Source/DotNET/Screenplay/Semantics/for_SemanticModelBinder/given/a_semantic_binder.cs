// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.given;

public class a_semantic_binder : Specification
{
    protected readonly ApplicationIdentity _applicationIdentity = ApplicationIdentity.Create("Projects");
    protected readonly SemanticModelBinder _binder = new();

    protected CompilationResult<SemanticCompilation> Bind(string source, string? syntaxPath = null, string displayPath = "application.play")
    {
        var syntax = new ScreenplayCompiler().Parse(source, syntaxPath).Value!;
        var catalog = SemanticIdentityCatalog.Empty(_applicationIdentity);
        const string StableKey = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, displayPath, source);
        var documents = SemanticDocumentSet.Create([document], catalog);
        return _binder.Bind("Projects", syntax, documents);
    }
}
