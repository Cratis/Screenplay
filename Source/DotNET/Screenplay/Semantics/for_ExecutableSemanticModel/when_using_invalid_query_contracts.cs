// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Semantics.given;
using Cratis.Specifications;
using Xunit;

namespace Cratis.Screenplay.Semantics.for_ExecutableSemanticModel;

public class when_using_invalid_query_contracts : a_valid_semantic_model
{
    Exception _cardinality;
    Exception _type;

    void Because()
    {
        var slice = _application.Modules.Single().Features.Single().Slices.Single(_ => _.Queries.Length > 0);
        var query = slice.Queries.Single();
        _cardinality = Validate(query with { Cardinality = SemanticQueryCardinality.Many });
        _type = Validate(query with
        {
            Argument = query.Argument with { Type = SemanticTypeReference.ForConcept(_projectNameConceptId) }
        });

        Exception Validate(SemanticKeyedQuery replacement) => Catch.Exception(() => ExecutableSemanticModel.Create(
            LanguageVersion.V1,
            SemanticVersion.V1,
            ReplaceSlice(slice with { Queries = [replacement] })));
    }

    [Fact] void should_reject_incompatible_cardinality() => _cardinality.ShouldBeOfExactType<InvalidSemanticContract>();
    [Fact] void should_reject_an_incompatible_argument_type() => _type.ShouldBeOfExactType<InvalidSemanticContract>();
}
#endif
