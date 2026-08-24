// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_non_portable_syntax : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _deferred;
    CompilationResult<SemanticCompilation> _unknownDocument;
    CompilationResult<SemanticCompilation> _unsupported;

    void Because()
    {
        _unsupported = Bind(
            """
            module Projects
              feature Registration
                slice StateChange RegisterProject
                  command RegisterProject
            """);
        _deferred = Bind(
            """
            layout ApplicationShell
              content
            module Projects
              feature Registration
                slice StateChange RegisterProject
            """);
        _unknownDocument = Bind(
            """
            module Projects
              feature Registration
                slice StateChange RegisterProject
            """,
            "other.play");
    }

    [Fact] void should_block_unsupported_behavior() => _unsupported.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax && _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
    [Fact] void should_bind_with_explicit_deferred_metadata() => _deferred.Success.ShouldBeTrue();
    [Fact] void should_report_deferred_metadata() => _deferred.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.DeferredSemanticSyntax);
    [Fact] void should_block_a_location_outside_the_document_set() => _unknownDocument.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnknownSemanticSourceDocument).ShouldBeTrue();
}
