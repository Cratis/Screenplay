// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_named_rule_has_a_body : given.a_validated_command
{
    void Because() => _result = BindRules("name rule BeUnique", "  file Validations/BeUnique.cs");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => Diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_requires_an_implementation_attachment() => Diagnostic.Message.ShouldEqual("Validation rule 'BeUnique' on 'name' has an implementation body; code validation requires a constrained implementation attachment (#139).");
}
