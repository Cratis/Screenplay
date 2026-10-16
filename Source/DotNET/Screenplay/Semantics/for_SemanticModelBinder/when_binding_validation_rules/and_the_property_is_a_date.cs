// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

public class and_the_property_is_a_date : given.a_validated_command
{
    void Because() => _result = BindRules("dueDate == \"2024-01-01\"");

    [Fact] void should_bind() => _result.Success.ShouldBeTrue();
    [Fact] void should_select_date_comparison_semantics() => _result.Value!.Model.SemanticVersion.ShouldEqual(SemanticVersion.V26);
    [Fact] void should_keep_the_calendar_literal_as_text() => Rule.Operand.ShouldEqual(SemanticValue.Text("2024-01-01"));
}
