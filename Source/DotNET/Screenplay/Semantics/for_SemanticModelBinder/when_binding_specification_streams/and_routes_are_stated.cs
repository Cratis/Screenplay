// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_specification_streams;

public class and_routes_are_stated : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("""
        module M
          feature F
            slice StateView S
              event E
              specification X
                given E
                  for "other"
                  stream Account.Profile
                when append E
                  for "other"
                  stream Account.Profile
                then E
                  no stream
        """);

    [Fact] void should_refuse_all_three_route_nodes() => _result.Diagnostics.Count(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("#457", StringComparison.Ordinal)).ShouldEqual(3);
    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
}
