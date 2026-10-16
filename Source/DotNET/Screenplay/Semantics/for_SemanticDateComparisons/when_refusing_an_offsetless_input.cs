// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Semantics.Execution;

namespace Cratis.Screenplay.Semantics.for_SemanticDateComparisons;

public class when_refusing_an_offsetless_input : given.a_date_command
{
    SemanticValidationFailure? _result;

    void Because() => _result = SemanticDateComparisons.CheckInputs(Application, Command, [new(First.Id, SemanticValue.Text("2026-01-01T00:00:00"))]);

    [Fact] void should_report_the_exact_input_refusal() => _result!.Message.ShouldEqual("DateTime command property 'from' requires an explicit offset or Z.");
    [Fact] void should_report_error_severity() => _result!.Severity.ShouldEqual(SemanticValidationSeverity.Error);
}
