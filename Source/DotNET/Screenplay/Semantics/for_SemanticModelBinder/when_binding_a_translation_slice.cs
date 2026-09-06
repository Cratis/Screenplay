// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

#if DEBUG
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_translation_slice : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;
    Exception? _error;

    void Because() => _error = Catch.Exception(() =>
    {
        _result = Bind(
            """
            module Projects
              feature Registration
                slice Translate TranslateRegistration
            """);
    });

    [Fact] void should_not_throw_while_computing_the_semantic_revision() => _error.ShouldBeNull();
    [Fact] void should_not_admit_unimplemented_occurrence_semantics() => _result.Success.ShouldBeFalse();
    [Fact] void should_not_return_a_usable_semantic_compilation() => _result.Value.ShouldBeNull();
    [Fact] void should_report_unsupported_semantics() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax && _.Severity == DiagnosticSeverity.Error).ShouldBeTrue();
}
#endif
