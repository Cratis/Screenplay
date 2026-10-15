// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_sample_data : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() =>
        _result = Bind(
            """
            module Projects
              feature Registration
                slice StateView ProjectList
                  readmodel Project
                    id String
                    name String
                  query ProjectById => Project optional
                    by id String

            sample data Demo
              shape Project
                row
                  name = "Apollo"

            ui profile Desktop
              sample data
                active Demo
            """);

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_report_the_pack_as_deferred_and_not_executable() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.DeferredSemanticSyntax && diagnostic.Message.Contains("Sample data pack 'Demo'", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_not_carry_the_rows_into_the_executable_model() => _result.Diagnostics.Any(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ShouldBeFalse();
}
