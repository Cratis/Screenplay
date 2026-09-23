// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_rule_targets_a_nested_path : given.a_validated_command
{
    void Because() => _result = BindRules("name.first not empty");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => _result.Diagnostics.Any(_ => _.Code == DiagnosticCodes.UnsupportedSemanticSyntax && _.Message.StartsWith("Validation rule on 'name.first' is not admitted", StringComparison.Ordinal)).ShouldBeTrue();
}
