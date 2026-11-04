// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Syntax;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_qualified_capture_sources.given;

public class a_capture_source : Specification
{
    protected static string Source(string name) => $$"""
        concept ProductId : Uuid
        module Products
          feature Catalog
            slice Translate Facts
              direction inbound
              event ProductPublished from "catalog"
                productId ProductId
              event ProductReceived
                productId ProductId
              capture ProductsFeed
                source events
                  from {{name}}
                key productId
                append ProductReceived
                  productId = $.productId
        """;

    protected static CompilationResult<SemanticCompilation> Bind(ApplicationSyntax syntax)
    {
        var identity = ApplicationIdentity.Create("Products");
        var catalog = SemanticIdentityCatalog.Empty(identity);
        const string StableKey = "application-document";
        var document = SemanticSourceDocument.Create(catalog.ResolveDocument(StableKey), StableKey, "capture.play", Source("ProductPublished"));
        return new SemanticModelBinder().Bind("Products", syntax, SemanticDocumentSet.Create([document], catalog));
    }
}
