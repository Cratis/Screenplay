// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_referencing_an_authorized_query_in_a_later_slice : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        policy SignedIn
          require authenticated
        module Projects
          feature Views
            slice StateView Assertions
              specification LookingUpARow
                then query ById
                  arguments
                    id = "first"
            slice StateView Rows
              readmodel Row
                id String key
              query ById => Row?
                by id String
                authorize SignedIn
        """);

    [Fact] void should_refuse_the_specification_without_an_explicit_caller() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_the_missing_caller() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.MissingSpecificationCaller);
    [Fact] void should_not_publish_a_model() => _result.Value.ShouldBeNull();
}
