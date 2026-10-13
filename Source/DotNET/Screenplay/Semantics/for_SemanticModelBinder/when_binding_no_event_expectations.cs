// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_no_event_expectations : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Work
            slice StateChange Nothing
              command DoNothing
                projectId String identifier
              specification NothingHappens
                when DoNothing
                  projectId = "one"
        """;

    CompilationResult<SemanticCompilation> _explicit;
    CompilationResult<SemanticCompilation> _omitted;

    void Because()
    {
        _omitted = Bind(Source);
        _explicit = Bind(Source + "\n        then no events");
    }

    [Fact] void should_keep_outcome_less_actions_invalid() => _omitted.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("at least one success outcome", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_admit_explicit_no_events() => _explicit.Success.ShouldBeTrue();
    [Fact] void should_select_the_new_version() => _explicit.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V16);
}
