// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_scoped_authorization : given.a_semantic_binder
{
    const string Source =
        """
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

    [Fact] void should_reject_scope_level_authorization() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_every_scope_without_silently_dropping_it() => _result.Diagnostics.Where(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax).Select(_ => _.Message).ShouldContainOnly(
        "Module 'Portal' authorization requires portable policy semantics and is not admitted by ESM v1.",
        "Feature 'Orders' authorization requires portable policy semantics and is not admitted by ESM v1.",
        "Feature 'Returns' authorization requires portable policy semantics and is not admitted by ESM v1.");
    [Fact] void should_allow_the_unguarded_structure() => _unguarded.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax).ShouldBeFalse();
}
