// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_normalising_a_performed_query : given.a_semantic_binder
{
    const string Preamble =
        """
        policy NeedsReader
          require role "Reader"
        module Orders
          feature Ordering
            slice StateView OrderSummary
              readmodel OrderSummary
                orderId Uuid
              query OrderById => OrderSummary?
                by orderId Uuid
                authorize NeedsReader
        """;

    static string Specification(string body) => Preamble + "\n      specification Probe\n" + body;

    CompilationResult<SemanticCompilation> _denied;
    CompilationResult<SemanticCompilation> _rejected;
    CompilationResult<SemanticCompilation> _mixedExactness;
    CompilationResult<SemanticCompilation> _noAction;

    void Because()
    {
        _denied = Bind(Specification("        given caller\n          authenticated\n          role \"Guest\"\n        when query OrderById\n          orderId = \"00000000-0000-0000-0000-000000000001\"\n        then denied\n"));
        _rejected = Bind(Specification("        given caller\n          authenticated\n          role \"Reader\"\n        when query OrderById\n          orderId = \"00000000-0000-0000-0000-000000000001\"\n        then error\n"));
        _mixedExactness = Bind(Specification("        given caller\n          authenticated\n          role \"Reader\"\n        when query OrderById\n          orderId = \"00000000-0000-0000-0000-000000000001\"\n        then result exactly\n          orderId = \"00000000-0000-0000-0000-000000000001\"\n        then result\n          orderId = \"00000000-0000-0000-0000-000000000002\"\n"));
        _noAction = Bind(Specification("        then denied\n"));
    }

    static string Codes(CompilationResult<SemanticCompilation> result) => string.Join(' ', result.Diagnostics.Select(diagnostic => diagnostic.Code));

    [Fact] void should_treat_a_performed_query_as_the_action_of_a_denial() => Codes(_denied).ShouldEqual(string.Empty);
    [Fact] void should_treat_a_performed_query_with_one_error_as_a_rejected_query() => Codes(_rejected).ShouldEqual(string.Empty);
    [Fact] void should_refuse_results_that_mix_exact_and_loose_comparison() => Codes(_mixedExactness).ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_still_require_an_action_for_a_bare_denial() => Codes(_noAction).ShouldEqual(DiagnosticCodes.InvalidWhenlessSpecification);
}
