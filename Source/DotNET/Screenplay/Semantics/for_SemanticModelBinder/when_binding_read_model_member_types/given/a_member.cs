// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_read_model_member_types.given;

public class a_member : Cratis.Screenplay.for_ScreenplayCompiler.when_validating_read_model_member_types.given.a_member
{
    protected CompilationResult<SemanticCompilation> Binding;

    protected void BindParsedSource()
    {
        var syntax = Compiler.Parse(Source, "model.play").Value!;
        var catalog = SemanticIdentityCatalog.Empty(ApplicationIdentity.Create("Members"));
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument("model"), "model", "model.play", Source);
        Binding = new SemanticModelBinder().Bind("Members", syntax, SemanticDocumentSet.Create([document], catalog));
    }
}
