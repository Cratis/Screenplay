// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;

namespace Cratis.Screenplay.Semantics.for_SemanticModelBinder.when_binding_validation_rules;

// ESM v1 has no runtime date value - a date is text in a fixed format - so no comparison on it is admitted.
public class and_the_property_is_a_date : given.a_validated_command
{
    void Because() => _result = BindRules("dueDate == \"2024-01-01\"");

    [Fact] void should_not_bind() => _result.Success.ShouldBeFalse();
    [Fact] void should_report_unsupported_syntax() => Diagnostic.Code.ShouldEqual(DiagnosticCodes.UnsupportedSemanticSyntax);
    [Fact] void should_say_there_is_no_date_value() => Diagnostic.Message.ShouldEqual("Validation rule '==' on 'dueDate' is not admitted: ESM v1 has no runtime date value - dates are text in a fixed format, so they cannot be compared.");
}
