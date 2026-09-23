// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.Execution.for_SemanticEvaluator.when_checking_whether_a_value_is_empty;

public class with_every_known_value_variant : Specification
{
    [Fact] void should_treat_null_as_empty() => SemanticEvaluator.IsEmpty(SemanticValue.Null).ShouldBeTrue();
    [Fact] void should_treat_empty_text_as_empty() => SemanticEvaluator.IsEmpty(SemanticValue.Text(string.Empty)).ShouldBeTrue();
    [Fact] void should_treat_text_as_present() => SemanticEvaluator.IsEmpty(SemanticValue.Text("name")).ShouldBeFalse();
    [Fact] void should_treat_an_empty_array_as_empty() => SemanticEvaluator.IsEmpty(SemanticValue.Array([])).ShouldBeTrue();
    [Fact] void should_treat_an_array_with_values_as_present() => SemanticEvaluator.IsEmpty(SemanticValue.Array([SemanticValue.Null])).ShouldBeFalse();
    [Fact] void should_treat_a_number_as_present() => SemanticEvaluator.IsEmpty(SemanticValue.Number(0m)).ShouldBeFalse();
    [Fact] void should_treat_a_boolean_as_present() => SemanticEvaluator.IsEmpty(SemanticValue.Boolean(false)).ShouldBeFalse();
    [Fact] void should_treat_a_composite_as_present() => SemanticEvaluator.IsEmpty(SemanticValue.Composite([])).ShouldBeFalse();
}
