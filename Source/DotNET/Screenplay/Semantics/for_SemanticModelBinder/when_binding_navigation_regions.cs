// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics.Serialization;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_navigation_regions : given.a_semantic_binder
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

    const string Layout =
        """
        layout Shell
          content
          navigation sidebar
            item work title "Work" target Work

        """;

    const string ModuleNavigation =
        """
          navigation sidebar extends
            item items title "Items" target Work.Items
        """;

    CompilationResult<SemanticCompilation> _baseline;
    CompilationResult<SemanticCompilation> _navigated;

    void Because()
    {
        _baseline = Bind(Source);
        _navigated = Bind(Layout + Source.Replace("module Work\n", "module Work\n" + ModuleNavigation + "\n", StringComparison.Ordinal));
    }

    [Fact] void should_bind_both_models() => (_baseline.Success && _navigated.Success).ShouldBeTrue();
    [Fact] void should_preserve_canonical_bytes() => SemanticModelSerializer.Serialize(_baseline.Value!.Model).SequenceEqual(SemanticModelSerializer.Serialize(_navigated.Value!.Model)).ShouldBeTrue();
    [Fact] void should_defer_the_layout_and_the_module_region() => _navigated.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldContainOnly(DiagnosticCodes.DeferredSemanticSyntax, DiagnosticCodes.DeferredSemanticSyntax);
    [Fact] void should_keep_the_diagnostics_informational() => _navigated.Diagnostics.All(diagnostic => diagnostic.Severity == DiagnosticSeverity.Information).ShouldBeTrue();
    [Fact] void should_name_the_module_region() => _navigated.Diagnostics.Any(diagnostic => diagnostic.Message.Contains("Navigation sidebar of module 'Work'", StringComparison.Ordinal)).ShouldBeTrue();
}
