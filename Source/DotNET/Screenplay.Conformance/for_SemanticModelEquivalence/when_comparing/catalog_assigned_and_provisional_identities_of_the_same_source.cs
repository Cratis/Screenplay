// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics;

namespace Cratis.Screenplay.Conformance.for_SemanticModelEquivalence.when_comparing;

public class catalog_assigned_and_provisional_identities_of_the_same_source : given.compiled_models
{
    ExecutableSemanticModel _provisional;
    ExecutableSemanticModel _assigned;

    void Establish()
    {
        _provisional = Compile(Source);
        _assigned = Compile(Source, Reversed(Workspace(Source).IdentityCatalog));
    }

    void Because() => _comparison = SemanticModelEquivalence.Compare(_provisional, _assigned);

    [Fact] void should_assign_different_identities() => _assigned.Revision.ShouldNotEqual(_provisional.Revision);
    [Fact] void should_be_equivalent() => _comparison.IsEquivalent.ShouldBeTrue();
}
