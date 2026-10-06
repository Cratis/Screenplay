// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_RegisterProjectCorpus.given;

public class a_v7_corpus : Specification
{
    protected readonly CanonicalCorpusVector Corpus = RegisterProjectCorpus.V7;

    protected static SemanticCompilation Compile(CanonicalCorpusVector corpus, CanonicalCorpusSourceForm form)
    {
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        catalog.Application.ShouldEqual(corpus.ApplicationIdentity);
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        var result = new SemanticModelCompiler().Compile(corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
        result.Success.ShouldBeTrue();
        result.Diagnostics.ShouldBeEmpty();

        return result.Value!;
    }
}
