// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_specification_event_source_syntax : given.a_semantic_binder
{
    const string Source =
        """
        concept ProjectId : Uuid
        module Projects
          feature Registration
            slice StateChange RegisterProject
              command RegisterProject
                projectId ProjectId identifier
                produces ProjectRegistered
                  for projectId
              event ProjectRegistered
              specification ExplicitEventSources
                given ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                when RegisterProject
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                  projectId = "3fa85f64-5717-4562-b3fc-2c963f66afa6"
                then ProjectRegistered
                  for "3fa85f64-5717-4562-b3fc-2c963f66afa6"
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_fail_closed() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_every_event_source_assertion() => UnsupportedDiagnostics.Length.ShouldEqual(3);
    [Fact] void should_report_the_exact_assertion_lines() => UnsupportedDiagnostics.Select(diagnostic => diagnostic.Location.Line).Order().ShouldEqual(12, 14, 17);
    [Fact] void should_report_real_source_columns() => UnsupportedDiagnostics.All(diagnostic => diagnostic.Location.Column > 0).ShouldBeTrue();
    [Fact] void should_name_the_unsupported_v1_boundary() => UnsupportedDiagnostics.All(diagnostic => diagnostic.Message.Contains("not admitted by ESM v1", StringComparison.Ordinal)).ShouldBeTrue();

    Diagnostic[] UnsupportedDiagnostics => [.. _result.Diagnostics.Where(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax)];
}
