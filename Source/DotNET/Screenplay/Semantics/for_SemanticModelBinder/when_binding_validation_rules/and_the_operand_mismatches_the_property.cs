// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_operand_mismatches_the_property : given.a_validated_command
{
    CompilationResult<SemanticCompilation> _fraction;
    CompilationResult<SemanticCompilation> _undeclaredMember;

    void Because()
    {
        _fraction = BindRules("quantity min 1.5");
        _undeclaredMember = BindRules("status == \"pending\"");
    }

    [Fact] void should_report_a_fraction_as_an_invalid_binding() => _fraction.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_say_a_whole_number_is_required() => _fraction.Diagnostics.Single().Message.ShouldEqual("The 'min' operand on 'quantity' must be a whole number to match what it is compared with.");
    [Fact] void should_report_an_undeclared_member_as_an_invalid_binding() => _undeclaredMember.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_say_the_member_must_be_declared() => _undeclaredMember.Diagnostics.Single().Message.ShouldEqual("The '==' operand on 'status' must be a declared member of the enumeration.");
}
