// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_screen_with_a_guarded_action : given.a_semantic_binder
{
    const string Source =
        """
        module Work
          feature Items
            slice StateChange Retry
              command Retry
                itemId Uuid identifier
                produces Retried
              event Retried
        """;

    const string Screen =
        """

              screen Details
                action "Again"
                  when item.status == "failed" execute Retry
                  otherwise hidden
        """;

    CompilationResult<SemanticCompilation> _baseline;
    CompilationResult<SemanticCompilation> _guarded;

    void Because()
    {
        _baseline = Bind(Source);
        _guarded = Bind(Source + Screen);
    }

    [Fact] void should_bind_both_models() => (_baseline.Success && _guarded.Success).ShouldBeTrue();
    [Fact] void should_preserve_canonical_bytes() => SemanticModelSerializer.Serialize(_baseline.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_guarded.Value!.Model)).ShouldBeTrue();
    [Fact] void should_preserve_revision() => _guarded.Value!.Model.Revision.ShouldEqual(_baseline.Value!.Model.Revision);
    [Fact] void should_report_only_the_deferred_screen() => _guarded.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.DeferredSemanticSyntax);
    [Fact] void should_keep_the_screen_diagnostic_informational() => _guarded.Diagnostics.Single().Severity.ShouldEqual(DiagnosticSeverity.Information);
}
