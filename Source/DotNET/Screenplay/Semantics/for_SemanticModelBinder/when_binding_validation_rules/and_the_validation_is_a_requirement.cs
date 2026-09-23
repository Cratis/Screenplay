// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// A requirement is decision-consistency semantics, which waits on #129.
public class and_the_validation_is_a_requirement : given.a_validated_command
{
    void Because() => _result = BindRules("require quantity > 0");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => Diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_awaits_decision_consistency() => Diagnostic.Message.ShouldEqual("Command 'PlaceOrder' requirement conditions await decision consistency (#129).");
}
