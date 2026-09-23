// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_is_a_bare_named_rule : given.a_validated_command
{
    void Because() => _result = BindRules("name rule BeUnique");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => Diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_it_has_no_portable_meaning() => Diagnostic.Message.ShouldEqual("Named validation rule 'BeUnique' on 'name' has no portable meaning - its logic lives outside the document.");
}
