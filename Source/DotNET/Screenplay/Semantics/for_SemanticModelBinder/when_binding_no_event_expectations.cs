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
    [Fact] void should_refuse_no_events_until_admission() => _explicit.Success.ShouldBeFalse();
    [Fact] void should_name_the_unadmitted_feature() => _explicit.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message == "Explicit no-event assertions are not admitted by any supported executable model (ESM) version yet (#433).").ShouldBeTrue();
}
