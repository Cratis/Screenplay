// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_public_events;

public class an_import_with_an_origin : given.a_public_events_model
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("import Shipping.ShipmentDispatched from \"shipping\"\nmodule M\n  feature F\n    slice Translate T\n      direction inbound\n      event Local\n        id String\n");

    [Fact] void should_refuse_it_because_it_has_no_local_shape() => _result.Diagnostics.Any(diagnostic => diagnostic.Code == DiagnosticCodes.UnsupportedSemanticSyntax && diagnostic.Message.Contains("no local shape")).ShouldBeTrue();
    [Fact] void should_not_produce_a_model() => _result.Value.ShouldBeNull();
}
