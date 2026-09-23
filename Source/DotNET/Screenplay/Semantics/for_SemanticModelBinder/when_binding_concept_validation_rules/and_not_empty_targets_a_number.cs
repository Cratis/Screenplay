// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_concept_validation_rules;

public class and_not_empty_targets_a_number : given.a_semantic_binder
{
    CompilationResult<SemanticCompilation> _result;

    void Because() => _result = Bind("concept Quantity : Int\n  validate\n    not empty");

    [Fact] void should_reject_the_rule() => _result.Diagnostics.Single().Code.ShouldEqual(DiagnosticCodes.InvalidSemanticBinding);
    [Fact] void should_locate_the_rule_not_the_application() => _result.Diagnostics.Single().Location.Line.ShouldEqual(3);
}
