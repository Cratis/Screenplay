// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_a_constraint;

public class with_invalid_extensions : given.a_semantic_binder
{
    const string Source =
        """
        module Projects
          feature Registration
            slice StateChange RegisterProject
              event ProjectRegistered
                code String
              constraint InvalidCasing
                unique event ProjectRegistered
                ignore casing
              constraint UnknownRelease
                unique code on ProjectRegistered
                released by ProjectMissing
              constraint UnknownCompositeProperty
                unique code, missing on ProjectRegistered
        """;

    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind(Source);

    [Fact] void should_reject_event_casing() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.InvalidConstraintCasing).ShouldBeTrue();
    [Fact] void should_reject_unknown_release() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnknownConstraintEvent && _.Message.Contains("ProjectMissing", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_reject_unknown_composite_property() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnknownConstraintProperty && _.Message.Contains("missing", StringComparison.Ordinal)).ShouldBeTrue();
}
