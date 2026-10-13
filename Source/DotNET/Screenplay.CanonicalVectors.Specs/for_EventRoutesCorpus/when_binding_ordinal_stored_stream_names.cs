// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.Specs.for_EventRoutesCorpus;

public class when_binding_ordinal_stored_stream_names : Specification
{
    CompilationResult<SemanticCompilation> _result = null!;

    void Because()
    {
        var form = EventRoutesCorpus.StoredStreamNamesSource;
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(form.IdentityCatalogBytes.AsSpan());
        var documents = form.Documents.Select(document => SemanticSourceDocument.Create(catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        _result = new SemanticModelCompiler().Compile("EventRoutes", SemanticDocumentSet.Create([.. documents], catalog));
    }

    [Fact] void should_bind_without_diagnostics() => _result.Diagnostics.ShouldBeEmpty();
    [Fact] void should_publish_an_executable_model() => _result.Success.ShouldBeTrue();
    [Fact] void should_preserve_the_stored_names() => _result.Value!.Model.Application.EventSources.Single().Streams.Select(stream => stream.StreamKind).Order(StringComparer.Ordinal).ShouldEqual(["ALL", "Archive", "Default", "all"]);
}
