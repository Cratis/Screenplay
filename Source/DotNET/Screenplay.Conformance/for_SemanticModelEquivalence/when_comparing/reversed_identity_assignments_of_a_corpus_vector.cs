// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.CanonicalCorpus;
using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class reversed_identity_assignments_of_a_corpus_vector : given.compiled_models
{
    ExecutableSemanticModel _original;
    ExecutableSemanticModel _reversed;

    void Establish()
    {
        var vector = RegisterProjectCorpus.V7;
        _original = Compile(vector, vector.SourceForms[0]);
        _reversed = Compile(vector, vector.SourceForms[0], reversed: true);
    }

    void Because() => _comparison = SemanticModelEquivalence.Compare(_original, _reversed);

    [Fact] void should_assign_different_identities() => _reversed.Revision.ShouldNotEqual(_original.Revision);
    [Fact] void should_be_equivalent() => _comparison.IsEquivalent.ShouldBeTrue();
}
