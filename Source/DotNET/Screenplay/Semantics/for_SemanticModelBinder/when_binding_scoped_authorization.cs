// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_scoped_authorization : given.a_semantic_binder
{
    const string Source =
        """
        policy Access
          require authenticated
        policy Staff
          require role "Staff"
        policy Extra
          require claim "department" matches "Returns"
        module Portal
          authorize Access
          feature Orders
            authorize Staff
            feature Returns
              authorize Extra
              slice StateChange ReturnOrder
                command RequestReturn
        """;

    CompilationResult<SemanticCompilation> _result;
    CompilationResult<SemanticCompilation> _unguarded;

    void Because()
    {
        _result = Bind(Source);
        _unguarded = Bind("""
            module Portal
              feature Orders
                slice StateChange ReturnOrder
                  command RequestReturn
            """);
    }

    [Fact] void should_bind_scope_level_authorization() => _result.Success.ShouldBeTrue();
    [Fact] void should_require_every_enclosing_scope() => _result.Value!.Model.Application.Modules.Single().Features.Single().Features.Single().Slices.Single().Commands.Single().Authorization.ShouldBeOfExactType<SemanticLogicalAuthorization>();
    [Fact] void should_allow_the_unguarded_structure() => _unguarded.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeFalse();
}
