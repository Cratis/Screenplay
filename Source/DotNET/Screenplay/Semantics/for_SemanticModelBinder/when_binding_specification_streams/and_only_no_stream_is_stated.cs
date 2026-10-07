// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_specification_streams;

public class and_only_no_stream_is_stated : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module M
          feature F
            slice StateView S
              event E
              specification X
                when append E
                then E
                  no stream
        """);

    [Fact] void should_refuse_the_unrouted_assertion_without_any_source_declaration() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_name_the_unadmitted_feature() => _result.Diagnostics.Single().Message.ShouldContain("#457");
}
