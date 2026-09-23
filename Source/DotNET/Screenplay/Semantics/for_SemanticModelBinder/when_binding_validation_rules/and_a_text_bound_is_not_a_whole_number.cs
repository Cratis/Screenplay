// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_a_text_bound_is_not_a_whole_number : given.a_validated_command
{
    void Because() => _result = BindRules("name max 2.5");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_an_invalid_binding() => Diagnostic.Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_say_the_bound_is_a_length() => Diagnostic.Message.ShouldEqual("The 'max' operand on 'name' must be a non-negative whole number because it is a text length.");
}
