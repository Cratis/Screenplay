// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class the_same_source_split_across_documents : given.compiled_models
{
    CanonicalCorpusSourceForm _single;
    CanonicalCorpusSourceForm _folder;

    void Establish()
    {
        _single = RegisterProjectCorpus.V7.SourceForms.Single(form => form.Name == "single");
        _folder = RegisterProjectCorpus.V7.SourceForms.Single(form => form.Name == "folder");
    }

    void Because() => _comparison = SemanticModelEquivalence.Compare(Compile(RegisterProjectCorpus.V7, _single), Compile(RegisterProjectCorpus.V7, _folder));

    [Fact] void should_split_the_source() => _folder.Documents.Length.ShouldBeGreaterThan(_single.Documents.Length);
    [Fact] void should_be_equivalent() => _comparison.IsEquivalent.ShouldBeTrue();
}
