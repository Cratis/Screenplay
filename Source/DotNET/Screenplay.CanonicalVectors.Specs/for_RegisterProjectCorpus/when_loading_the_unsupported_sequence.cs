// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.CanonicalVectors.for_RegisterProjectCorpus;

public class when_loading_the_unsupported_sequence : Specification
{
    CanonicalCorpusRejectionVector _corpus = null!;
    CompilationResult<SemanticCompilation> _result = null!;

    void Because()
    {
        _corpus = RegisterProjectCorpus.UnsupportedSequence;
        var catalog = SemanticIdentityCatalogSerializer.Deserialize(_corpus.SourceForm.IdentityCatalogBytes.AsSpan());
        var documents = _corpus.SourceForm.Documents.Select(document => SemanticSourceDocument.Create(
            catalog.ResolveDocument(document.StableKey), document.StableKey, document.DisplayPath, document.Text));
        _result = new SemanticModelCompiler().Compile(
            _corpus.ApplicationName, SemanticDocumentSet.Create([.. documents], catalog));
    }

    [Fact] void should_keep_the_negative_vector_name() => _corpus.Name.ShouldEqual("register-project/unsupported-sequence");
    [Fact] void should_keep_the_application_identity() => _corpus.ApplicationIdentity.ToString().ShouldEqual("app1:20ccb167f2400bc55fae1597b1a0f4d19b40841f513bd013a7fa815e9e7f2994");
    [Fact] void should_fail_compilation() => _result.Success.ShouldBeFalse();
    [Fact] void should_produce_no_executable_model() => _result.Value.ShouldBeNull();
    [Fact] void should_match_the_expected_diagnostics() => _result.Diagnostics.Select(diagnostic => (diagnostic.Code, diagnostic.Message)).ShouldEqual(_corpus.Diagnostics.Select(expected => (expected.Code, expected.Message)));
    [Fact] void should_publish_zero_artifacts() => _corpus.ArtifactPaths.ShouldBeEmpty();
}
