// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_not_empty_targets_a_non_text_scalar : given.a_validated_command
{
    CompilationResult<SemanticCompilation>[] _results = null!;

    void Because() => _results = [BindRules("orderId not empty"), BindRules("amount not empty"), BindRules("dueDate not empty"), BindRules("express not empty")];

    [Fact] void should_reject_every_invalid_target_at_the_rule() => _results.All(result => !result.Success && result.Diagnostics.Single().Code == DiagnosticCodes.InvalidSemanticBinding && result.Diagnostics.Single().Location.Line == 20).ShouldBeTrue();
    [Fact] void should_explain_the_applicable_types() => _results.All(result => result.Diagnostics.Single().Message.Contains("not empty requires text or a collection", StringComparison.Ordinal)).ShouldBeTrue();
}
