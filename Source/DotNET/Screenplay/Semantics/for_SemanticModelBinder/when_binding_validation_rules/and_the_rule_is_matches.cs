// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// Whether 'matches' names a pattern or carries a regular expression, and in which dialect, is undecided (#209).
public class and_the_rule_is_matches : given.a_validated_command
{
    void Because() => _result = BindRules("name matches email");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => Diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_awaits_a_portable_pattern() => Diagnostic.Message.ShouldEqual("Validation rule 'matches' on 'name' awaits a portable pattern definition (#209).");
}
