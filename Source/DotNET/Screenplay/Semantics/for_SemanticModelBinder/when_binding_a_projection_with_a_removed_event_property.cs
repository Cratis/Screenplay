// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder;

public class when_binding_a_projection_with_a_removed_event_property : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("module Projects\n  feature Registration\n    slice StateChange RegisterProject\n      event Registered\n        old String\n      event Registered generation 2\n        current String\n    slice StateView Lookup\n      readmodel Summary\n        id String\n        label String\n      projection SummaryProjection => Summary\n        from Registered key old\n          label = old\n");

    [Fact] void should_name_the_historical_event_revision() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.InvalidSemanticBinding && diagnostic.Message.Contains("Registered", StringComparison.Ordinal) && diagnostic.Message.Contains("revision 1", StringComparison.Ordinal)).ShouldBeTrue();
}
