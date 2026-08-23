// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
namespace Cratis.Screenplay.Semantics.for_SemanticCompilation;

public class when_creating_a_valid_compilation : given.a_valid_semantic_compilation
{
    SemanticCompilation _compilation;
    SemanticRevision _revision;

    void Because()
    {
        _revision = _model.Revision;
        _compilation = SemanticCompilation.Create(_model, _documents, _sourceMap);
    }

    [Fact] void should_preserve_the_model() => _compilation.Model.ShouldEqual(_model);
    [Fact] void should_preserve_the_document_set() => _compilation.Documents.ShouldEqual(_documents);
    [Fact] void should_preserve_the_validated_source_map() => _compilation.SourceMap.Entries.ShouldContainOnly(_sourceMap.Entries);
    [Fact] void should_keep_source_integrity_metadata_out_of_the_semantic_revision() => _compilation.Model.Revision.ShouldEqual(_revision);
}
#endif
